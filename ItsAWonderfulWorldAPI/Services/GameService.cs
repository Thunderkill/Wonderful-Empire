using System;
using System.Collections.Generic;
using System.Linq;
using ItsAWonderfulWorldAPI.Models;
using Microsoft.Extensions.Logging;

namespace ItsAWonderfulWorldAPI.Services
{
    public class GameService
    {
        private static readonly Random _random = new Random();
        private readonly ILogger<GameService> _logger;

        public GameService(ILogger<GameService> logger)
        {
            _logger = logger;
        }

        public void InitializeGame(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.Players.Count < 2 || game.Players.Count > 5)
                throw new InvalidOperationException("The game requires 2 to 5 players.");

            _logger.LogInformation($"Initializing game {game.Id} with {game.Players.Count} players");

            CreateDevelopmentDeck(game);
            DealInitialCards(game);

            _logger.LogInformation($"Game {game.Id} initialized successfully");
        }

        public void DraftCard(Game game, Guid playerId, Guid cardId)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.CurrentPhase != GamePhase.Draft)
                throw new InvalidOperationException("It's not the drafting phase.");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId) 
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.Hand.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's hand.", nameof(cardId));

            _logger.LogInformation($"Player {player.Name} (ID: {playerId}) drafting card {card.Name} (ID: {cardId}) in game {game.Id}");

            player.Hand.Remove(card);
            player.ConstructionArea.Add(card);

            PassCards(game, player);

            _logger.LogInformation($"Card drafted successfully in game {game.Id}");
        }

        public void PlanCard(Game game, Guid playerId, Guid cardId, bool construct)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.CurrentPhase != GamePhase.Planning)
                throw new InvalidOperationException("It's not the planning phase.");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.ConstructionArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's construction area.", nameof(cardId));

            _logger.LogInformation($"Player {player.Name} (ID: {playerId}) planning card {card.Name} (ID: {cardId}) in game {game.Id}. Construct: {construct}");

            player.ConstructionArea.Remove(card);

            if (construct)
            {
                var constructionCost = new Dictionary<ResourceType, int>(card.ConstructionCost);
                int reducedCostCards = player.Empire.Count(c => c.SpecialAbility == SpecialAbility.ReducedConstructionCost);
                
                foreach (var resource in constructionCost.Keys.ToList())
                {
                    while (reducedCostCards > 0 && constructionCost[resource] > 0)
                    {
                        constructionCost[resource]--;
                        reducedCostCards--;
                    }
                }

                bool canConstruct = constructionCost.All(cost => player.Resources[cost.Key] >= cost.Value);

                if (canConstruct)
                {
                    foreach (var cost in constructionCost)
                    {
                        player.Resources[cost.Key] -= cost.Value;
                    }
                    player.Empire.Add(card);
                    _logger.LogInformation($"Card {card.Name} (ID: {cardId}) constructed successfully in game {game.Id}");
                }
                else
                {
                    player.ConstructionArea.Add(card);
                    _logger.LogWarning($"Not enough resources to construct card {card.Name} (ID: {cardId}) in game {game.Id}");
                    throw new InvalidOperationException("Not enough resources to construct this card.");
                }
            }
            else
            {
                foreach (var resource in card.RecyclingBonus)
                {
                    player.Resources[resource.Key] += resource.Value;
                }
                _logger.LogInformation($"Card {card.Name} (ID: {cardId}) recycled successfully in game {game.Id}");
            }
        }

        public void ProduceResources(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.CurrentPhase != GamePhase.Production)
                throw new InvalidOperationException("It's not the production phase.");

            _logger.LogInformation($"Starting production phase for game {game.Id}");

            foreach (var player in game.Players)
            {
                foreach (var card in player.Empire)
                {
                    foreach (var production in card.Production)
                    {
                        int productionValue = production.Value;
                        if (card.SpecialAbility == SpecialAbility.DoubleProduction)
                        {
                            productionValue *= 2;
                        }
                        player.Resources[production.Key] += productionValue;
                    }

                    ApplySpecialAbility(player, card);
                }

                foreach (var card in player.Empire.Where(c => c.SpecialAbility == SpecialAbility.VictoryPointBonus))
                {
                    card.VictoryPoints += player.Resources.Values.Sum() / 5;
                }

                _logger.LogInformation($"Production completed for player {player.Name} (ID: {player.Id}) in game {game.Id}");
            }

            game.CurrentRound++;
            CheckGameOver(game);

            _logger.LogInformation($"Production phase completed for game {game.Id}. Current round: {game.CurrentRound}");
        }

        public void CheckGameOver(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.CurrentRound > 4)
            {
                game.CurrentPhase = GamePhase.GameOver;
                _logger.LogInformation($"Game {game.Id} is over after 4 rounds");
            }
            else
            {
                game.CurrentPhase = GamePhase.Draft;
                DealCards(game);
                _logger.LogInformation($"Starting new round for game {game.Id}. Current round: {game.CurrentRound}");
            }
        }

        public Dictionary<Guid, int> CalculateFinalScores(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.CurrentPhase != GamePhase.GameOver)
                throw new InvalidOperationException("The game is not over yet.");

            _logger.LogInformation($"Calculating final scores for game {game.Id}");

            var scores = new Dictionary<Guid, int>();

            foreach (var player in game.Players)
            {
                int score = 0;
                score += player.Empire.Sum(card => card.VictoryPoints);
                score += player.Resources.Values.Sum() / 3;
                score += CalculateSpecialScoringPoints(player);
                scores[player.Id] = score;

                _logger.LogInformation($"Final score for player {player.Name} (ID: {player.Id}): {score}");
            }

            return scores;
        }

        public void DealInitialCards(Game game)
        {
            _logger.LogInformation($"Dealing initial cards for game {game.Id}");
            DealCards(game);
        }

        private int CalculateSpecialScoringPoints(Player player)
        {
            int specialPoints = 0;

            var cardTypeCounts = player.Empire.GroupBy(card => card.Type).ToDictionary(g => g.Key, g => g.Count());
            specialPoints += cardTypeCounts.Values.Sum(count => count / 2);

            specialPoints += cardTypeCounts.Count / 3;

            int setCount = player.Resources.Values.Min();
            specialPoints += setCount * 2;

            specialPoints += player.Resources[ResourceType.Krystallium] / 2;

            if (cardTypeCounts.Count == Enum.GetValues(typeof(CardType)).Length)
            {
                specialPoints += 3;
            }

            _logger.LogInformation($"Special scoring points for player {player.Name} (ID: {player.Id}): {specialPoints}");
            return specialPoints;
        }

        private void ApplySpecialAbility(Player player, Card card)
        {
            switch (card.SpecialAbility)
            {
                case SpecialAbility.ResourceConversion:
                    ConvertResources(player);
                    break;
                case SpecialAbility.ExtraCardDraw:
                    // This is handled in the DealCards method
                    break;
                case SpecialAbility.ReducedConstructionCost:
                    // This is handled in the PlanCard method
                    break;
            }
        }

        private void ConvertResources(Player player)
        {
            var conversions = new List<(ResourceType From, ResourceType To, int Rate)>
            {
                (ResourceType.Materials, ResourceType.Krystallium, 3),
                (ResourceType.Energy, ResourceType.Science, 2),
                (ResourceType.Gold, ResourceType.Exploration, 2)
            };

            foreach (var conversion in conversions)
            {
                int conversionAmount = player.Resources[conversion.From] / conversion.Rate;
                if (conversionAmount > 0)
                {
                    player.Resources[conversion.From] -= conversionAmount * conversion.Rate;
                    player.Resources[conversion.To] += conversionAmount;
                    _logger.LogInformation($"Player {player.Name} (ID: {player.Id}) converted {conversionAmount * conversion.Rate} {conversion.From} to {conversionAmount} {conversion.To}");
                }
            }
        }

        private void CreateDevelopmentDeck(Game game)
        {
            string[] cardNames = {
                "Recycling Plant", "Wind Turbines", "Research Lab", "Financial District", "Space Station",
                "Hydroelectric Dam", "Quantum Computer", "Stock Exchange", "Mars Colony", "Fusion Reactor",
                "AI Research Center", "Orbital Hotel", "Underwater City", "Time Machine", "Antimatter Factory"
            };

            for (int i = 0; i < 150; i++)
            {
                game.DevelopmentDeck.Add(GenerateCard(cardNames[i % cardNames.Length], (CardType)(i % 5)));
            }

            game.DevelopmentDeck = game.DevelopmentDeck.OrderBy(c => _random.Next()).ToList();
            _logger.LogInformation($"Development deck created and shuffled for game {game.Id}");
        }

        private Card GenerateCard(string name, CardType type)
        {
            var card = new Card(name, type);

            card.ConstructionCost = GenerateRandomResources(1, 3);
            card.Production = GenerateRandomResources(1, 2);
            card.RecyclingBonus = GenerateRandomResources(1, 1);
            card.VictoryPoints = _random.Next(1, 6);

            if (_random.Next(100) < 20)
            {
                card.SpecialAbility = (SpecialAbility)_random.Next(1, Enum.GetValues(typeof(SpecialAbility)).Length);
            }
            else
            {
                card.SpecialAbility = SpecialAbility.None;
            }

            return card;
        }

        private Dictionary<ResourceType, int> GenerateRandomResources(int minResources, int maxResources)
        {
            var resources = new Dictionary<ResourceType, int>();
            int resourceCount = _random.Next(minResources, maxResources + 1);

            for (int i = 0; i < resourceCount; i++)
            {
                ResourceType resourceType = (ResourceType)_random.Next(0, 6);
                if (!resources.ContainsKey(resourceType))
                {
                    resources[resourceType] = 0;
                }
                resources[resourceType] += _random.Next(1, 4);
            }

            return resources;
        }

        private void DealCards(Game game)
        {
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
                int cardsToDraw = 7 + player.Empire.Count(c => c.SpecialAbility == SpecialAbility.ExtraCardDraw);
                for (int i = 0; i < cardsToDraw; i++)
                {
                    if (game.DevelopmentDeck.Any())
                    {
                        var card = game.DevelopmentDeck[0];
                        game.DevelopmentDeck.RemoveAt(0);
                        player.Hand.Add(card);
                    }
                }
                _logger.LogInformation($"Dealt {cardsToDraw} cards to player {player.Name} (ID: {player.Id}) in game {game.Id}");
            }
        }

        private void PassCards(Game game, Player currentPlayer)
        {
            var playerIndex = game.Players.IndexOf(currentPlayer);
            var nextPlayerIndex = (playerIndex + 1) % game.Players.Count;
            var nextPlayer = game.Players[nextPlayerIndex];

            _logger.LogInformation($"Passing cards from player {currentPlayer.Name} to player {nextPlayer.Name} in game {game.Id}");

            var cardsToPass = currentPlayer.Hand.ToList();
            currentPlayer.Hand.Clear();
            nextPlayer.Hand.AddRange(cardsToPass);

            _logger.LogInformation($"Cards passed successfully in game {game.Id}");
        }
    }
}
