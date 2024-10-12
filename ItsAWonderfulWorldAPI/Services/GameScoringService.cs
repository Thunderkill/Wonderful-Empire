using System;
using System.Collections.Generic;
using System.Linq;
using ItsAWonderfulWorldAPI.Models;
using Microsoft.Extensions.Logging;

namespace ItsAWonderfulWorldAPI.Services
{
    public class GameScoringService
    {
        private readonly ILogger<GameScoringService> _logger;

        public GameScoringService(ILogger<GameScoringService> logger)
        {
            _logger = logger;
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
                score += CalculateGrossVictoryPoints(player);
                score += CalculateComboVictoryPoints(player);
                score += CalculateGeneralsVictoryPoints(player);
                score += CalculateFinanciersVictoryPoints(player);
                scores[player.Id] = score;

                _logger.LogInformation($"Final score for player {player.Name} (ID: {player.Id}): {score}");
            }

            DetermineWinner(game, scores);

            return scores;
        }

        private int CalculateGrossVictoryPoints(Player player)
        {
            return player.Empire.Sum(card => card.VictoryPoints);
        }

        private int CalculateComboVictoryPoints(Player player)
        {
            var cardTypeCounts = player.Empire.GroupBy(card => card.Type).ToDictionary(g => g.Key, g => g.Count());
            return player.Empire.Sum(card => card.ComboVictoryPoints * (cardTypeCounts[card.Type] - 1));
        }

        private int CalculateGeneralsVictoryPoints(Player player)
        {
            int basePoints = player.Characters[CharacterType.General];
            int bonusPoints = player.Empire.Sum(card => card.GeneralVictoryPointsBonus);
            return basePoints + bonusPoints;
        }

        private int CalculateFinanciersVictoryPoints(Player player)
        {
            int basePoints = player.Characters[CharacterType.Financier];
            int bonusPoints = player.Empire.Sum(card => card.FinancierVictoryPointsBonus);
            return basePoints + bonusPoints;
        }

        private void DetermineWinner(Game game, Dictionary<Guid, int> scores)
        {
            var maxScore = scores.Max(s => s.Value);
            var winners = scores.Where(s => s.Value == maxScore).Select(s => s.Key).ToList();

            if (winners.Count == 1)
            {
                game.WinnerId = winners[0];
                _logger.LogInformation($"Player {game.Players.First(p => p.Id == game.WinnerId).Name} (ID: {game.WinnerId}) wins the game {game.Id}");
            }
            else
            {
                // Tiebreaker: Most cards in Empire
                var mostCards = winners.Max(w => game.Players.First(p => p.Id == w).Empire.Count);
                winners = winners.Where(w => game.Players.First(p => p.Id == w).Empire.Count == mostCards).ToList();

                if (winners.Count == 1)
                {
                    game.WinnerId = winners[0];
                    _logger.LogInformation($"Player {game.Players.First(p => p.Id == game.WinnerId).Name} (ID: {game.WinnerId}) wins the game {game.Id} after first tiebreaker");
                }
                else
                {
                    // Second tiebreaker: Most Character tokens
                    var mostTokens = winners.Max(w => {
                        var player = game.Players.First(p => p.Id == w);
                        return player.Characters[CharacterType.General] + player.Characters[CharacterType.Financier];
                    });
                    winners = winners.Where(w => {
                        var player = game.Players.First(p => p.Id == w);
                        return player.Characters[CharacterType.General] + player.Characters[CharacterType.Financier] == mostTokens;
                    }).ToList();

                    if (winners.Count == 1)
                    {
                        game.WinnerId = winners[0];
                        _logger.LogInformation($"Player {game.Players.First(p => p.Id == game.WinnerId).Name} (ID: {game.WinnerId}) wins the game {game.Id} after second tiebreaker");
                    }
                    else
                    {
                        game.WinnerId = null;
                        _logger.LogInformation($"Game {game.Id} ends in a tie between players: {string.Join(", ", winners)}");
                    }
                }
            }
        }
    }
}