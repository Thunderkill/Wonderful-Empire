using System;
using System.Collections.Generic;
using System.Linq;
using ItsAWonderfulWorldAPI.Models;
using Microsoft.Extensions.Logging;

namespace ItsAWonderfulWorldAPI.Services
{
    public class GamePlayService
    {
        private readonly ILogger<GamePlayService> _logger;

        public event Action<Game> GameOverEvent;
        public event Action<Game> NewRoundEvent;

        public GamePlayService(ILogger<GamePlayService> logger)
        {
            _logger = logger;
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

            if (player.HasDraftedThisRound)
                throw new InvalidOperationException("Player has already drafted this round.");

            var card = player.Hand.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's hand.", nameof(cardId));

            _logger.LogInformation($"Player {player.Name} (ID: {playerId}) drafting card {card.Name} (ID: {cardId}) in game {game.Id}");

            player.Hand.Remove(card);
            player.DraftingArea.Add(card);
            player.HasDraftedThisRound = true;

            game.PlayersDrafted.Add(playerId);

            if (game.PlayersDrafted.Count == game.Players.Count)
            {
                game.ShouldPassHands = true;
            }

            PassHandsIfNeeded(game);

            CheckDraftingPhaseEnd(game);

            _logger.LogInformation($"Card drafted successfully in game {game.Id}");
        }

        public void SetPlayerReady(Game game, Guid playerId)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("The game is not in progress.");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            if (game.CurrentPhase == GamePhase.Planning)
            {
                if (player.DraftingArea.Any())
                    throw new InvalidOperationException("Player's drafting area is not empty.");
            }
            else if (game.CurrentPhase != GamePhase.Production)
            {
                throw new InvalidOperationException("It's not a phase where players can be ready.");
            }

            player.IsReady = true;
            _logger.LogInformation($"Player {player.Name} (ID: {playerId}) is ready in game {game.Id}");

            if (game.AreAllPlayersReady())
            {
                if (game.CurrentPhase == GamePhase.Planning)
                {
                    EndPlanningPhase(game);
                }
                else if (game.CurrentPhase == GamePhase.Production)
                {
                    MoveToNextProductionStep(game);
                }
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

            if (!game.AreAllPlayersReady())
                throw new InvalidOperationException("Not all players are ready.");

            ConvertExcessResources(game);
            
            game.CurrentPhase = GamePhase.Production;
            _logger.LogInformation($"Planning phase ended for game {game.Id}. Moving to Production phase.");

            ProduceResources(game);
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

            // Initialize the production step
            game.CurrentProductionStep = ResourceType.Materials;

            // Produce the first resource type
            ProduceResourceStep(game);
        }

        public void ProduceResourceStep(Game game)
        {
            if (game.CurrentProductionStep == null)
            {
                throw new InvalidOperationException("Current production step is not set.");
            }

            foreach (var player in game.Players)
            {
                ProduceResourceForPlayer(player, game.CurrentProductionStep.Value);
                player.IsReady = false; // Reset ready state for the next step
            }

            _logger.LogInformation($"Production step completed for {game.CurrentProductionStep} in game {game.Id}");
        }

        private void ProduceResourceForPlayer(Player player, ResourceType resourceType)
        {
            int production = player.Empire.Sum(card =>
            {
                int baseProduction = card.Production.GetValueOrDefault(resourceType, 0);
                return card.SpecialAbility == SpecialAbility.DoubleProduction ? baseProduction * 2 : baseProduction;
            });

            player.Resources[resourceType] += production;

            _logger.LogInformation($"Player {player.Name} (ID: {player.Id}) produced {production} {resourceType}");
        }

        public void AddResourceToCard(Game game, Guid playerId, Guid cardId, ResourceType resourceType)
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.ConstructionArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's construction area.", nameof(cardId));

            if (!card.ConstructionCost.ContainsKey(resourceType))
                throw new InvalidOperationException("This resource is not required for the card's construction.");

            if (card.InvestedResources.GetValueOrDefault(resourceType, 0) >= card.ConstructionCost[resourceType])
                throw new InvalidOperationException("This resource is already fully added to the card.");

            if (player.Resources[resourceType] == 0)
                throw new InvalidOperationException("Player doesn't have this resource.");

            player.Resources[resourceType]--;

            if (!card.InvestedResources.ContainsKey(resourceType))
                card.InvestedResources[resourceType] = 0;
            card.InvestedResources[resourceType]++;

            _logger.LogInformation($"Added {resourceType} to card {card.Name} (ID: {cardId}) for player {player.Name} (ID: {playerId}) in game {game.Id}");

            if (card.IsConstructed())
            {
                MoveCardToEmpire(game, playerId, cardId);
                _logger.LogInformation($"Card {card.Name} (ID: {cardId}) constructed and moved to empire for player {player.Name} (ID: {playerId}) in game {game.Id}");
            }
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

            var card = player.DraftingArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's drafting area.", nameof(cardId));

            player.DraftingArea.Remove(card);
            player.Resources[card.RecyclingBonus]++;

            _logger.LogInformation($"Discarded card {card.Name} (ID: {cardId}) for player {player.Name} (ID: {playerId}) in game {game.Id}. Recycling bonus applied.");

            return new Dictionary<ResourceType, int> { { card.RecyclingBonus, 1 } };
        }

        public void MoveCardToConstructionArea(Game game, Guid playerId, Guid cardId)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("The game is not in progress.");

            if (game.CurrentPhase != GamePhase.Planning)
                throw new InvalidOperationException("It's not the planning phase.");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId)
                ?? throw new ArgumentException("Player not found.", nameof(playerId));

            var card = player.DraftingArea.FirstOrDefault(c => c.Id == cardId)
                ?? throw new ArgumentException("Card not found in player's drafting area.", nameof(cardId));

            player.DraftingArea.Remove(card);
            player.ConstructionArea.Add(card);

            _logger.LogInformation($"Moved card {card.Name} (ID: {cardId}) from drafting area to construction area for player {player.Name} (ID: {playerId}) in game {game.Id}");
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
                GameOverEvent?.Invoke(game);
            }
            else
            {
                game.CurrentPhase = GamePhase.Draft;
                _logger.LogInformation($"Starting new round for game {game.Id}. Current round: {game.CurrentRound}");
                NewRoundEvent?.Invoke(game);
            }
        }

        private void ConvertExcessResources(Game game)
        {
            foreach (var player in game.Players)
            {
                foreach (var resource in player.Resources)
                {
                    if (resource.Key != ResourceType.Krystallium)
                    {
                        player.DiscardedResourcePool += resource.Value;
                        player.Resources[resource.Key] = 0;
                    }
                }

                int krystalliumGained = player.DiscardedResourcePool / 5;
                player.Resources[ResourceType.Krystallium] += krystalliumGained;
                player.DiscardedResourcePool %= 5;
                
                _logger.LogInformation($"Player {player.Name} (ID: {player.Id}) converted {krystalliumGained * 5} discarded resources to {krystalliumGained} Krystallium. Remaining in discarded pool: {player.DiscardedResourcePool}");
            }
        }

        private void MoveToNextProductionStep(Game game)
        {
            ResourceType[] productionOrder = { ResourceType.Materials, ResourceType.Energy, ResourceType.Science, ResourceType.Gold, ResourceType.Exploration };
            int currentIndex = game.CurrentProductionStep.HasValue ? Array.IndexOf(productionOrder, game.CurrentProductionStep.Value) : -1;

            if (currentIndex < productionOrder.Length - 1)
            {
                // Move to the next production step
                game.CurrentProductionStep = productionOrder[currentIndex + 1];
                ProduceResourceStep(game);
            }
            else
            {
                // Production phase is complete
                FinishProductionPhase(game);
            }
        }

        private void FinishProductionPhase(Game game)
        {
            foreach (var player in game.Players)
            {
                // Apply special abilities that depend on total resources
                foreach (var card in player.Empire.Where(c => c.SpecialAbility == SpecialAbility.VictoryPointBonus))
                {
                    card.VictoryPoints += player.Resources.Values.Sum() / 5;
                }

                // Apply resource conversion special ability
                ApplyResourceConversion(player);

                player.IsReady = false; // Reset ready state for next round
            }

            game.CurrentRound++;

            game.CurrentDraftDirection = game.CurrentDraftDirection == DraftDirection.Clockwise
                ? DraftDirection.Counterclockwise
                : DraftDirection.Clockwise;

            _logger.LogInformation($"Production phase completed for game {game.Id}. Current round: {game.CurrentRound}. New draft direction: {game.CurrentDraftDirection}");

            CheckGameOver(game);
        }

        private void ApplyResourceConversion(Player player)
        {
            foreach (var card in player.Empire.Where(c => c.SpecialAbility == SpecialAbility.ResourceConversion))
            {
                ConvertResources(player);
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

        private void PassCards(Game game)
        {
            List<List<Card>> hands = game.Players.Select(p => p.Hand.ToList()).ToList();

            for (int i = 0; i < game.Players.Count; i++)
            {
                int nextIndex = game.CurrentDraftDirection == DraftDirection.Clockwise
                    ? (i + 1) % game.Players.Count
                    : (i - 1 + game.Players.Count) % game.Players.Count;

                game.Players[nextIndex].Hand = hands[i];
                game.Players[nextIndex].HasDraftedThisRound = false; // Reset the drafting status
            }

            _logger.LogInformation($"Cards passed successfully in game {game.Id}");
        }
    }

    public static class CardExtensions
    {
        public static bool IsConstructed(this Card card)
        {
            return card.ConstructionCost.All(cost => 
                card.InvestedResources.GetValueOrDefault(cost.Key, 0) >= cost.Value
            );
        }
    }
}