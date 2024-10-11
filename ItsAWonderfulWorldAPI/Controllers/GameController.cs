using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ItsAWonderfulWorldAPI.Models;
using ItsAWonderfulWorldAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace ItsAWonderfulWorldAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private static List<Game> _games = new List<Game>();
        private readonly GameService _gameService;

        public GameController(GameService gameService)
        {
            _gameService = gameService;
        }

        [HttpGet("list")]
        public ActionResult<IEnumerable<GameSummary>> ListGames()
        {
            var gameSummaries = _games.Select(g => new GameSummary
            {
                Id = g.Id,
                PlayerCount = g.Players.Count,
                CurrentRound = g.CurrentRound,
                CurrentPhase = g.CurrentPhase
            });

            return Ok(gameSummaries);
        }

        [HttpGet("{gameId}")]
        public ActionResult<GameStatus> GetGameStatus(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            var gameStatus = new GameStatus
            {
                Id = game.Id,
                CurrentRound = game.CurrentRound,
                CurrentPhase = game.CurrentPhase,
                CurrentDraftDirection = game.CurrentDraftDirection,
                Players = game.Players.Select(p => new PlayerStatus
                {
                    Id = p.Id,
                    Name = p.Name,
                    HandCount = p.Hand.Count,
                    ConstructionAreaCount = p.ConstructionArea.Count,
                    BuiltCardsCount = p.Empire.Count,
                    Resources = p.Resources
                }).ToList()
            };

            return Ok(gameStatus);
        }

        [HttpPost("create")]
        public ActionResult<Game> CreateGame([FromBody] List<string> playerNames)
        {
            if (playerNames == null || playerNames.Count < 2 || playerNames.Count > 5)
            {
                return BadRequest("The game requires 2 to 5 players.");
            }

            var game = new Game();
            foreach (var playerName in playerNames)
            {
                game.Players.Add(new Player(playerName));
            }

            try
            {
                _gameService.InitializeGame(game);
                _games.Add(game);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/draft")]
        public ActionResult<DraftResult> DraftCard(Guid gameId, [FromBody] DraftAction draftAction)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            try
            {
                bool handsPassed = game.ShouldPassHands;
                _gameService.DraftCard(game, draftAction.PlayerId, draftAction.CardId);
                
                var result = new DraftResult
                {
                    Success = true,
                    HandsPassed = handsPassed,
                    CurrentDraftDirection = game.CurrentDraftDirection,
                    RemainingCards = game.Players.First(p => p.Id == draftAction.PlayerId).Hand.Count,
                    CurrentPhase = game.CurrentPhase,
                    CurrentRound = game.CurrentRound
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/plan")]
        public ActionResult<Game> PlanCard(Guid gameId, [FromBody] PlanAction planAction)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            try
            {
                _gameService.PlanCard(game, planAction.PlayerId, planAction.CardId, planAction.Construct);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/endplanning")]
        public ActionResult<Game> EndPlanningPhase(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            try
            {
                _gameService.EndPlanningPhase(game);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/produce")]
        public ActionResult<Game> Produce(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            try
            {
                _gameService.ProduceResources(game);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{gameId}/scores")]
        public ActionResult<Dictionary<Guid, int>> GetFinalScores(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            try
            {
                var scores = _gameService.CalculateFinalScores(game);
                return Ok(scores);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class DraftAction
    {
        public Guid PlayerId { get; set; }
        public Guid CardId { get; set; }
    }

    public class PlanAction
    {
        public Guid PlayerId { get; set; }
        public Guid CardId { get; set; }
        public bool Construct { get; set; }
    }

    public class GameSummary
    {
        public Guid Id { get; set; }
        public int PlayerCount { get; set; }
        public int CurrentRound { get; set; }
        public GamePhase CurrentPhase { get; set; }
    }

    public class DraftResult
    {
        public bool Success { get; set; }
        public bool HandsPassed { get; set; }
        public DraftDirection CurrentDraftDirection { get; set; }
        public int RemainingCards { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public int CurrentRound { get; set; }
    }

    public class GameStatus
    {
        public Guid Id { get; set; }
        public int CurrentRound { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public DraftDirection CurrentDraftDirection { get; set; }
        public List<PlayerStatus> Players { get; set; }
    }

    public class PlayerStatus
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int HandCount { get; set; }
        public int ConstructionAreaCount { get; set; }
        public int BuiltCardsCount { get; set; }
        public Dictionary<ResourceType, int> Resources { get; set; }
    }
}
