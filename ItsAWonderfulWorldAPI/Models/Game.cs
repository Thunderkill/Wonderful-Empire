using System;
using System.Collections.Generic;
using System.Linq;

namespace ItsAWonderfulWorldAPI.Models
{
    public class Game
    {
        public Guid Id { get; set; }
        public List<Player> Players { get; set; }
        public Player Host { get; set; }
        public int CurrentRound { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public GameState State { get; set; }
        public List<Card> DevelopmentDeck { get; set; }
        public HashSet<Guid> PlayersDrafted { get; set; }
        public bool ShouldPassHands { get; set; }
        public DraftDirection CurrentDraftDirection { get; set; }
        public int MaxPlayers { get; set; }
        public Guid? WinnerId { get; set; }
        public ResourceType? CurrentProductionStep { get; set; }

        // New property to indicate if the current user has joined
        public bool HasCurrentUserJoined(Guid? currentUserId)
        {
            return currentUserId.HasValue && Players.Any(p => p.Id == currentUserId.Value);
        }

        // New method to check if all players are ready
        public bool AreAllPlayersReady()
        {
            return Players.All(p => p.IsReady);
        }

        public Game()
        {
            Id = Guid.NewGuid();
            Players = new List<Player>();
            CurrentRound = 1;
            CurrentPhase = GamePhase.Draft;
            State = GameState.Lobby;
            DevelopmentDeck = new List<Card>();
            PlayersDrafted = new HashSet<Guid>();
            ShouldPassHands = false;
            CurrentDraftDirection = DraftDirection.Clockwise;
            MaxPlayers = 5; // Default max players, can be changed
            WinnerId = null;
            CurrentProductionStep = null;
        }
    }

    public enum GamePhase
    {
        Draft,
        Planning,
        Production,
        GameOver
    }

    public enum GameState
    {
        Lobby,
        InProgress,
        Finished
    }

    public enum DraftDirection
    {
        Clockwise,
        Counterclockwise
    }
}
