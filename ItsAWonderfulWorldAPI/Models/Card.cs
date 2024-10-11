using System;
using System.Collections.Generic;

namespace ItsAWonderfulWorldAPI.Models
{
    public class Card
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public CardType Type { get; set; }
        public Dictionary<ResourceType, int> ConstructionCost { get; set; }
        public Dictionary<ResourceType, int> Production { get; set; }
        public int VictoryPoints { get; set; }
        public Dictionary<ResourceType, int> RecyclingBonus { get; set; }
        public SpecialAbility SpecialAbility { get; set; }
        public int ComboVictoryPoints { get; set; }
        public int GeneralVictoryPointsBonus { get; set; }
        public int FinancierVictoryPointsBonus { get; set; }

        public Card(string name, CardType type)
        {
            Id = Guid.NewGuid();
            Name = name;
            Type = type;
            ConstructionCost = new Dictionary<ResourceType, int>();
            Production = new Dictionary<ResourceType, int>();
            RecyclingBonus = new Dictionary<ResourceType, int>();
            ComboVictoryPoints = 0;
            GeneralVictoryPointsBonus = 0;
            FinancierVictoryPointsBonus = 0;
        }
    }

    public enum CardType
    {
        Structure,
        Vehicle,
        Research,
        Project,
        Discovery
    }

    public enum SpecialAbility
    {
        None,
        DoubleProduction,
        ExtraCardDraw,
        ResourceConversion,
        VictoryPointBonus,
        ReducedConstructionCost
    }
}
