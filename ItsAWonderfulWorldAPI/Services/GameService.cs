using System;
using System.Collections.Generic;
using System.Linq;
using ItsAWonderfulWorldAPI.Models;
using Microsoft.Extensions.Logging;

namespace ItsAWonderfulWorldAPI.Services
{
    public class GameService
    {
        private readonly ILogger<GameService> _logger;
        private readonly GameInitializationService _initializationService;
        private readonly GamePlayService _playService;
        private readonly GameScoringService _scoringService;

        public GameService(
            ILogger<GameService> logger,
            GameInitializationService initializationService,
            GamePlayService playService,
            GameScoringService scoringService)
        {
            _logger = logger;
            _initializationService = initializationService;
            _playService = playService;
            _scoringService = scoringService;

            _playService.GameOverEvent += HandleGameOver;
            _playService.NewRoundEvent += HandleNewRound;
        }

        // Game Initialization
        public Game CreateLobby(User host, int maxPlayers) => _initializationService.CreateLobby(host, maxPlayers);
        public void JoinLobby(Game game, User user) => _initializationService.JoinLobby(game, user);
        public void StartGame(Game game, Guid hostId)
        {
            _initializationService.StartGame(game, hostId);
            InitializeNewRound(game);
        }

        // Game Play
        public void DraftCard(Game game, Guid playerId, Guid cardId) => _playService.DraftCard(game, playerId, cardId);
        public void SetPlayerReady(Game game, Guid playerId) => _playService.SetPlayerReady(game, playerId);
        public void AddResourceToCard(Game game, Guid playerId, Guid cardId, ResourceType resourceType) =>
            _playService.AddResourceToCard(game, playerId, cardId, resourceType);
        public void MoveCardToEmpire(Game game, Guid playerId, Guid cardId) =>
            _playService.MoveCardToEmpire(game, playerId, cardId);
        public Dictionary<ResourceType, int> DiscardCard(Game game, Guid playerId, Guid cardId) =>
            _playService.DiscardCard(game, playerId, cardId);
        public void MoveCardToConstructionArea(Game game, Guid playerId, Guid cardId) =>
            _playService.MoveCardToConstructionArea(game, playerId, cardId);
        public void EndPlanningPhase(Game game) => _playService.EndPlanningPhase(game);
        public void ProduceResources(Game game) => _playService.ProduceResources(game);

        // Game Scoring
        public Dictionary<Guid, int> CalculateFinalScores(Game game) => _scoringService.CalculateFinalScores(game);

        // Utility Methods
        public bool HasUserJoinedGame(Game game, Guid userId) => game.Players.Any(p => p.Id == userId);

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
                CurrentProductionStep = game.CurrentProductionStep,
                Host = CreatePlayerStatus(game.Host),
                CurrentPlayer = CreatePlayerStatus(currentPlayer),
                OtherPlayers = otherPlayers.Select(CreatePlayerStatus).ToList()
            };

            if (game.State == GameState.Finished)
            {
                gameStatus.FinalScores = CalculateFinalScores(game);
                gameStatus.WinnerId = game.WinnerId;
            }

            _logger.LogInformation($"Game status retrieved for player {currentPlayer.Name} (ID: {playerId}) in game {game.Id}");

            return gameStatus;
        }

        private PlayerStatus CreatePlayerStatus(Player player)
        {
            return new PlayerStatus
            {
                Id = player.Id,
                Name = player.Name,
                Resources = player.Resources,
                Characters = player.Characters,
                Hand = player.Hand,
                HandCount = player.Hand.Count,
                DraftingArea = player.DraftingArea,
                ConstructionArea = player.ConstructionArea,
                Empire = player.Empire,
                IsReady = player.IsReady,
                HasDraftedThisRound = player.HasDraftedThisRound,
                DiscardedResourcePool = player.DiscardedResourcePool
            };
        }

        private void HandleGameOver(Game game)
        {
            _logger.LogInformation($"Game {game.Id} has ended. Calculating final scores.");
            var finalScores = CalculateFinalScores(game);
            game.WinnerId = finalScores.OrderByDescending(kvp => kvp.Value).First().Key;
            _logger.LogInformation($"Game {game.Id} winner: Player {game.WinnerId}");
        }

        private void HandleNewRound(Game game)
        {
            _logger.LogInformation($"Starting new round for game {game.Id}");
            InitializeNewRound(game);
        }

        private void InitializeNewRound(Game game)
        {
            _initializationService.DealCards(game);
            game.CurrentPhase = GamePhase.Draft;
            game.PlayersDrafted.Clear();
            foreach (var player in game.Players)
            {
                player.HasDraftedThisRound = false;
                player.IsReady = false;
            }
            _logger.LogInformation($"New round initialized for game {game.Id}. Current round: {game.CurrentRound}");
        }
    }

    public class GameStatus
    {
        public Guid GameId { get; set; }
        public GameState GameState { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public int CurrentRound { get; set; }
        public ResourceType? CurrentProductionStep { get; set; }
        public PlayerStatus Host { get; set; }
        public PlayerStatus CurrentPlayer { get; set; }
        public List<PlayerStatus> OtherPlayers { get; set; }
        public Dictionary<Guid, int> FinalScores { get; set; }
        public Guid? WinnerId { get; set; }
    }

    public class PlayerStatus
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Dictionary<ResourceType, int> Resources { get; set; }
        public Dictionary<CharacterType, int> Characters { get; set; }
        public List<Card> Hand { get; set; }
        public int HandCount { get; set; }
        public List<Card> DraftingArea { get; set; }
        public List<Card> ConstructionArea { get; set; }
        public List<Card> Empire { get; set; }
        public bool IsReady { get; set; }
        public bool HasDraftedThisRound { get; set; }
        public int DiscardedResourcePool { get; set; }
    }
}
