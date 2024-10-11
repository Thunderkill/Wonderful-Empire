using System;
using System.Collections.Generic;

namespace ItsAWonderfulWorldAPI.Models
{
    public class Player
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Card> Hand { get; set; }
        public List<Card> ConstructionArea { get; set; }
        public List<Card> Empire { get; set; }
        public Dictionary<ResourceType, int> Resources { get; set; }
        public bool IsReady { get; set; }
        public bool HasDraftedThisRound { get; set; }  // New property to track drafting status

        public Player(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Hand = new List<Card>();
            ConstructionArea = new List<Card>();
            Empire = new List<Card>();
            Resources = new Dictionary<ResourceType, int>();
            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                Resources[resourceType] = 0;
            }
            IsReady = false;
            HasDraftedThisRound = false;  // Initialize HasDraftedThisRound to false
        }
    }
}
