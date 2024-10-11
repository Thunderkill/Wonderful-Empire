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

        public Game()
        {
            Id = Guid.NewGuid();
            Players = new List<Player>();
            CurrentRound = 1;
            CurrentPhase = GamePhase.Draft;
            DevelopmentDeck = new List<Card>();
        }
    }

    public enum GamePhase
    {
        Draft,
        Planning,
        Production,
        GameOver
    }
}
