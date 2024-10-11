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

        public Game CreateLobby(User host, int maxPlayers)
        {
            if (maxPlayers < 2 || maxPlayers > 5)
                throw new ArgumentException("The game requires 2 to 5 players.", nameof(maxPlayers));

            var game = new Game
            {
                Host = new Player(host.Username) { Id = host.Id },
                MaxPlayers = maxPlayers,
                State = GameState.Lobby
            };

            game.Players.Add(game.Host);

            _logger.LogInformation($"Lobby created for game {game.Id} with host {host.Username} (ID: {host.Id})");

            return game;
        }

        public void JoinLobby(Game game, User user)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.Lobby)
                throw new InvalidOperationException("The game is not in lobby state.");

            if (game.Players.Count >= game.MaxPlayers)
                throw new InvalidOperationException("The lobby is full.");

            if (game.Players.Any(p => p.Id == user.Id))
                throw new InvalidOperationException("The user is already in the lobby.");

            var player = new Player(user.Username) { Id = user.Id };
            game.Players.Add(player);

            _logger.LogInformation($"User {user.Username} (ID: {user.Id}) joined lobby for game {game.Id}");
        }

        public bool HasUserJoinedGame(Game game, Guid userId)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            return game.Players.Any(p => p.Id == userId);
        }

        public void StartGame(Game game, Guid hostId)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.Lobby)
                throw new InvalidOperationException("The game is not in lobby state.");

            if (game.Host.Id != hostId)
                throw new InvalidOperationException("Only the host can start the game.");

            if (game.Players.Count < 2)
                throw new InvalidOperationException("At least 2 players are required to start the game.");

            InitializeGame(game);

            game.State = GameState.InProgress;

            _logger.LogInformation($"Game {game.Id} started by host {game.Host.Name} (ID: {hostId})");
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

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("The game is not in progress.");

            if (game.CurrentPhase != GamePhase.Draft)
                throw new InvalidOperationException("It's not the drafting phase.");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId) 
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            if (game.PlayersDrafted.Contains(playerId))
                throw new InvalidOperationException("Player has already drafted this round.");

            var card = player.Hand.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's hand.", nameof(cardId));

            _logger.LogInformation($"Player {player.Name} (ID: {playerId}) drafting card {card.Name} (ID: {cardId}) in game {game.Id}");

            player.Hand.Remove(card);
            player.ConstructionArea.Add(card);

            game.PlayersDrafted.Add(playerId);

            if (game.PlayersDrafted.Count == game.Players.Count)
            {
                game.ShouldPassHands = true;
            }

            PassHandsIfNeeded(game);

            CheckDraftingPhaseEnd(game);

            _logger.LogInformation($"Card drafted successfully in game {game.Id}");
        }

        public void PlanCard(Game game, Guid playerId, Guid cardId, bool construct)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("The game is not in progress.");

            if (game.CurrentPhase != GamePhase.Planning)
                throw new InvalidOperationException("It's not the planning phase.");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.ConstructionArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's construction area.", nameof(cardId));

            _logger.LogInformation($"Player {player.Name} (ID: {playerId}) planning card {card.Name} (ID: {cardId}) in game {game.Id}. Construct: {construct}");

            if (construct)
            {
                if (card.IsConstructed())
                {
                    MoveCardToEmpire(game, playerId, cardId);
                    _logger.LogInformation($"Card {card.Name} (ID: {cardId}) constructed successfully in game {game.Id}");
                }
                else
                {
                    _logger.LogWarning($"Not enough resources to construct card {card.Name} (ID: {cardId}) in game {game.Id}");
                    throw new InvalidOperationException("Not enough resources to construct this card.");
                }
            }
            else
            {
                DiscardCard(game, playerId, cardId);
                _logger.LogInformation($"Card {card.Name} (ID: {cardId}) discarded and recycling bonus received in game {game.Id}");
            }
        }

        public void EndPlanningPhase(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("The game is not in progress.");

            if (game.CurrentPhase != GamePhase.Planning)
                throw new InvalidOperationException("It's not the planning phase.");

            game.CurrentPhase = GamePhase.Production;
            _logger.LogInformation($"Planning phase ended for game {game.Id}. Moving to Production phase.");
        }

        public void ProduceResources(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("The game is not in progress.");

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

            // Change draft direction for the next round
            game.CurrentDraftDirection = game.CurrentDraftDirection == DraftDirection.Clockwise
                ? DraftDirection.Counterclockwise
                : DraftDirection.Clockwise;

            _logger.LogInformation($"Production phase completed for game {game.Id}. Current round: {game.CurrentRound}. New draft direction: {game.CurrentDraftDirection}");

            CheckGameOver(game);
        }

        public Dictionary<Guid, int> CalculateFinalScores(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.Finished)
                throw new InvalidOperationException("The game is not finished yet.");

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

        public GameStatus GetGameStatus(Game game, Guid playerId)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            var currentPlayer = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found in the game.", nameof(playerId));

            var otherPlayers = game.Players.Where(p => p.Id != playerId).ToList();

            var gameStatus = new GameStatus
            {
                GameId = game.Id,
                GameState = game.State,
                CurrentPhase = game.CurrentPhase,
                CurrentRound = game.CurrentRound,
                Host = new PlayerStatus
                {
                    Id = game.Host.Id,
                    Name = game.Host.Name
                },
                CurrentPlayer = new PlayerStatus
                {
                    Id = currentPlayer.Id,
                    Name = currentPlayer.Name,
                    Resources = currentPlayer.Resources,
                    Hand = currentPlayer.Hand,
                    ConstructionArea = currentPlayer.ConstructionArea,
                    Empire = currentPlayer.Empire
                },
                OtherPlayers = otherPlayers.Select(p => new PlayerStatus
                {
                    Id = p.Id,
                    Name = p.Name,
                    Resources = p.Resources,
                    HandCount = p.Hand.Count,
                    ConstructionArea = p.ConstructionArea,
                    Empire = p.Empire
                }).ToList()
            };

            _logger.LogInformation($"Game status retrieved for player {currentPlayer.Name} (ID: {playerId}) in game {game.Id}");

            return gameStatus;
        }

        public void AddResourceToCard(Game game, Guid playerId, Guid cardId, ResourceType resourceType)
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.ConstructionArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's construction area.", nameof(cardId));

            if (!card.ConstructionCost.ContainsKey(resourceType))
                throw new InvalidOperationException("This resource is not required for the card's construction.");

            if (card.ConstructionCost[resourceType] == 0)
                throw new InvalidOperationException("This resource is already fully added to the card.");

            if (player.Resources[resourceType] == 0)
                throw new InvalidOperationException("Player doesn't have this resource.");

            card.ConstructionCost[resourceType]--;
            player.Resources[resourceType]--;

            _logger.LogInformation($"Added {resourceType} to card {card.Name} (ID: {cardId}) for player {player.Name} (ID: {playerId}) in game {game.Id}");
        }

        public void MoveCardToEmpire(Game game, Guid playerId, Guid cardId)
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.ConstructionArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's construction area.", nameof(cardId));

            player.ConstructionArea.Remove(card);
            player.Empire.Add(card);

            _logger.LogInformation($"Moved card {card.Name} (ID: {cardId}) from construction area to empire for player {player.Name} (ID: {playerId}) in game {game.Id}");
        }

        public Dictionary<ResourceType, int> DiscardCard(Game game, Guid playerId, Guid cardId)
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.ConstructionArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's construction area.", nameof(cardId));

            player.ConstructionArea.Remove(card);

            foreach (var resource in card.RecyclingBonus)
            {
                player.Resources[resource.Key] += resource.Value;
            }

            _logger.LogInformation($"Discarded card {card.Name} (ID: {cardId}) for player {player.Name} (ID: {playerId}) in game {game.Id}. Recycling bonus applied.");

            return card.RecyclingBonus;
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

        private void DealInitialCards(Game game)
        {
            _logger.LogInformation($"Dealing initial cards for game {game.Id}");
            DealCards(game);
        }

        private void PassHandsIfNeeded(Game game)
        {
            if (game.ShouldPassHands)
            {
                PassCards(game);
                game.PlayersDrafted.Clear();
                game.ShouldPassHands = false;

                _logger.LogInformation($"Hands passed in game {game.Id}. Current draft direction: {game.CurrentDraftDirection}");
            }
        }

        private void CheckDraftingPhaseEnd(Game game)
        {
            if (game.Players.All(p => p.Hand.Count == 0))
            {
                game.CurrentPhase = GamePhase.Planning;
                _logger.LogInformation($"Drafting phase ended for game {game.Id}. Moving to Planning phase.");
            }
        }

        private void CheckGameOver(Game game)
        {
            if (game.CurrentRound > 4)
            {
                game.CurrentPhase = GamePhase.GameOver;
                game.State = GameState.Finished;
                _logger.LogInformation($"Game {game.Id} is over after 4 rounds");
            }
            else
            {
                game.CurrentPhase = GamePhase.Draft;
                DealCards(game);
                _logger.LogInformation($"Starting new round for game {game.Id}. Current round: {game.CurrentRound}");
            }
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

        private void PassCards(Game game)
        {
            List<List<Card>> hands = game.Players.Select(p => p.Hand.ToList()).ToList();

            for (int i = 0; i < game.Players.Count; i++)
            {
                int nextIndex = game.CurrentDraftDirection == DraftDirection.Clockwise
                    ? (i + 1) % game.Players.Count
                    : (i - 1 + game.Players.Count) % game.Players.Count;

                game.Players[nextIndex].Hand = hands[i];
            }

            _logger.LogInformation($"Cards passed successfully in game {game.Id}");
        }
    }

    public class GameStatus
    {
        public Guid GameId { get; set; }
        public GameState GameState { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public int CurrentRound { get; set; }
        public PlayerStatus Host { get; set; }
        public PlayerStatus CurrentPlayer { get; set; }
        public List<PlayerStatus> OtherPlayers { get; set; }
    }

    public class PlayerStatus
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Dictionary<ResourceType, int> Resources { get; set; }
        public List<Card> Hand { get; set; }
        public int HandCount { get; set; }
        public List<Card> ConstructionArea { get; set; }
        public List<Card> Empire { get; set; }
    }

    public static class CardExtensions
    {
        public static bool IsConstructed(this Card card)
        {
            return card.ConstructionCost.All(cost => cost.Value == 0);
        }
    }
}
