using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var gameSummaries = _games.Select(g => new GameSummary
            {
                Id = g.Id,
                PlayerCount = g.Players.Count,
                CurrentRound = g.CurrentRound,
                CurrentPhase = g.CurrentPhase,
                State = g.State,
                MaxPlayers = g.MaxPlayers,
                HasCurrentUserJoined = g.HasCurrentUserJoined(currentUserId),
                HostName = g.Host.Name
            });

            return Ok(gameSummaries);
        }

        [HttpGet("{gameId}")]
        public ActionResult<GameStatus> GetGameStatus(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            if (!_gameService.HasUserJoinedGame(game, currentUserId))
                return BadRequest("You are not a player in this game");

            var gameStatus = _gameService.GetGameStatus(game, currentUserId);

            if (game.State == GameState.Finished)
            {
                var finalScores = _gameService.CalculateFinalScores(game);
                gameStatus.FinalScores = finalScores;
                gameStatus.WinnerId = game.WinnerId;
            }

            return Ok(gameStatus);
        }

        [HttpPost("create-lobby")]
        public ActionResult<Game> CreateLobby([FromBody] CreateLobbyRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("User not found");
            }

            try
            {
                var user = new User { Id = userId, Username = username };
                var game = _gameService.CreateLobby(user, request.MaxPlayers);
                _games.Add(game);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/join")]
        public ActionResult<Game> JoinLobby(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("User not found");
            }

            try
            {
                var user = new User { Id = userId, Username = username };
                _gameService.JoinLobby(game, user);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/start")]
        public ActionResult<Game> StartGame(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                _gameService.StartGame(game, userId);
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

            if (game.State != GameState.InProgress)
                return BadRequest("The game is not in progress");

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

        [HttpPost("{gameId}/addresource")]
        public ActionResult<PlanResult> AddResource(Guid gameId, [FromBody] AddResourceAction action)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            if (game.State != GameState.InProgress)
                return BadRequest("The game is not in progress");

            try
            {
                _gameService.AddResourceToCard(game, action.PlayerId, action.CardId, action.ResourceType);
                
                var player = game.Players.FirstOrDefault(p => p.Id == action.PlayerId);
                var card = player.Empire.FirstOrDefault(c => c.Id == action.CardId) ?? 
                           player.ConstructionArea.FirstOrDefault(c => c.Id == action.CardId);

                bool constructed = card.IsConstructed();
                if (constructed)
                {
                    _gameService.MoveCardToEmpire(game, action.PlayerId, action.CardId);
                }

                var result = new PlanResult
                {
                    Success = true,
                    ActionType = PlanActionType.AddResource,
                    CardName = card?.Name,
                    ResourceAdded = action.ResourceType,
                    Constructed = constructed,
                    UpdatedResources = player.Resources,
                    RemainingConstructionAreaCards = player.ConstructionArea.Count,
                    ConstructionAreaCards = player.ConstructionArea.Select(c => new CardStatus
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ConstructionCost = c.ConstructionCost,
                        InvestedResources = c.InvestedResources
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{gameId}/discard")]
        public ActionResult<PlanResult> Discard(Guid gameId, [FromBody] DiscardAction action)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            if (game.State != GameState.InProgress)
                return BadRequest("The game is not in progress");

            try
            {
                var recyclingBonus = _gameService.DiscardCard(game, action.PlayerId, action.CardId);
                
                var player = game.Players.FirstOrDefault(p => p.Id == action.PlayerId);

                var result = new PlanResult
                {
                    Success = true,
                    ActionType = PlanActionType.Discard,
                    RecyclingBonus = recyclingBonus,
                    UpdatedResources = player.Resources,
                    RemainingConstructionAreaCards = player.ConstructionArea.Count,
                    ConstructionAreaCards = player.ConstructionArea.Select(c => new CardStatus
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ConstructionCost = c.ConstructionCost,
                        InvestedResources = c.InvestedResources
                    }).ToList()
                };

                return Ok(result);
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

            if (game.State != GameState.InProgress)
                return BadRequest("The game is not in progress");

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

            if (game.State != GameState.InProgress)
                return BadRequest("The game is not in progress");

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

            if (game.State != GameState.Finished)
                return BadRequest("The game is not finished yet");

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

        [HttpPost("{gameId}/ready")]
        public ActionResult<Game> SetPlayerReady(Guid gameId)
        {
            var game = _games.FirstOrDefault(g => g.Id == gameId);
            if (game == null)
                return NotFound("Game not found");

            if (game.State != GameState.InProgress)
                return BadRequest("The game is not in progress");

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                _gameService.SetPlayerReady(game, userId);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class CreateLobbyRequest
    {
        public int MaxPlayers { get; set; }
    }

    public class DraftAction
    {
        public Guid PlayerId { get; set; }
        public Guid CardId { get; set; }
    }

    public enum PlanActionType
    {
        AddResource,
        Discard
    }

    public class AddResourceAction
    {
        public Guid PlayerId { get; set; }
        public Guid CardId { get; set; }
        public ResourceType ResourceType { get; set; }
    }

    public class DiscardAction
    {
        public Guid PlayerId { get; set; }
        public Guid CardId { get; set; }
    }

    public class GameSummary
    {
        public Guid Id { get; set; }
        public int PlayerCount { get; set; }
        public int CurrentRound { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public GameState State { get; set; }
        public int MaxPlayers { get; set; }
        public bool HasCurrentUserJoined { get; set; }
        public string HostName { get; set; }
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

    public class PlanResult
    {
        public bool Success { get; set; }
        public PlanActionType ActionType { get; set; }
        public string CardName { get; set; }
        public ResourceType? ResourceAdded { get; set; }
        public bool? Constructed { get; set; }
        public Dictionary<ResourceType, int> RecyclingBonus { get; set; }
        public Dictionary<ResourceType, int> UpdatedResources { get; set; }
        public int RemainingConstructionAreaCards { get; set; }
        public List<CardStatus> ConstructionAreaCards { get; set; }
    }

    public class CardStatus
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Dictionary<ResourceType, int> ConstructionCost { get; set; }
        public Dictionary<ResourceType, int> InvestedResources { get; set; }
    }
}
