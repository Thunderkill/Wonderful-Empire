using System;
using System.Collections.Generic;
using System.Linq;
using ItsAWonderfulWorldAPI.Models;
using Microsoft.Extensions.Logging;

namespace ItsAWonderfulWorldAPI.Services
{
    public class GameInitializationService
    {
        private static readonly Random _random = new Random();
        private readonly ILogger<GameInitializationService> _logger;
        private readonly CardGenerationService _cardGenerationService;

        public GameInitializationService(ILogger<GameInitializationService> logger, CardGenerationService cardGenerationService)
        {
            _logger = logger;
            _cardGenerationService = cardGenerationService;
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

        private void CreateDevelopmentDeck(Game game)
        {
            string[] cardNames = {
                "Recycling Plant", "Wind Turbines", "Research Lab", "Financial District", "Space Station",
                "Hydroelectric Dam", "Quantum Computer", "Stock Exchange", "Mars Colony", "Fusion Reactor",
                "AI Research Center", "Orbital Hotel", "Underwater City", "Time Machine", "Antimatter Factory"
            };

            for (int i = 0; i < 150; i++)
            {
                game.DevelopmentDeck.Add(_cardGenerationService.GenerateCard(cardNames[i % cardNames.Length], (CardType)(i % 5)));
            }

            game.DevelopmentDeck = game.DevelopmentDeck.OrderBy(c => _random.Next()).ToList();
            _logger.LogInformation($"Development deck created and shuffled for game {game.Id}");
        }

        private void DealInitialCards(Game game)
        {
            _logger.LogInformation($"Dealing initial cards for game {game.Id}");
            DealCards(game);
        }

        public void DealCards(Game game)
        {
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
                player.HasDraftedThisRound = false; // Reset the drafting status
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
    }
}