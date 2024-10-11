using System;
using System.Collections.Generic;

namespace ItsAWonderfulWorldAPI.Models
{
    public class Game
    {
        public Guid Id { get; set; }
        public List<Player> Players { get; set; }
        public int CurrentRound { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public List<Card> DevelopmentDeck { get; set; }
        public HashSet<Guid> PlayersDrafted { get; set; }
        public bool ShouldPassHands { get; set; }
        public DraftDirection CurrentDraftDirection { get; set; }

        public Game()
        {
            Id = Guid.NewGuid();
            Players = new List<Player>();
            CurrentRound = 1;
            CurrentPhase = GamePhase.Draft;
            DevelopmentDeck = new List<Card>();
            PlayersDrafted = new HashSet<Guid>();
            ShouldPassHands = false;
            CurrentDraftDirection = DraftDirection.Clockwise;
        }
    }

    public enum GamePhase
    {
        Draft,
        Planning,
        Production,
        GameOver
    }

    public enum DraftDirection
    {
        Clockwise,
        Counterclockwise
    }
}
