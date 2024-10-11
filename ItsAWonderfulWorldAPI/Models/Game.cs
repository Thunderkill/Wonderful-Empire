using System;
using System.Collections.Generic;

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
