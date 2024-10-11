using System;
using System.Collections.Generic;
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
        public ActionResult<Game> DraftCard(Guid gameId, [FromBody] DraftAction draftAction)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            try
            {
                _gameService.DraftCard(game, draftAction.PlayerId, draftAction.CardId);
                return Ok(game);
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
}
