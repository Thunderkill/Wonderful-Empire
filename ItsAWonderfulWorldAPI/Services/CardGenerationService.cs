using System;
using System.Collections.Generic;
using ItsAWonderfulWorldAPI.Models;

namespace ItsAWonderfulWorldAPI.Services
{
    public class CardGenerationService
    {
        private static readonly Random _random = new Random();

        public Card GenerateCard(string name, CardType type)
        {
            var card = new Card(name, type);

            card.ConstructionCost = GenerateRandomResources(1, 3);
            card.Production = GenerateRandomResources(1, 2);
            card.RecyclingBonus = GenerateRandomRecyclingBonus();
            card.VictoryPoints = _random.Next(1, 6);
            card.ComboVictoryPoints = _random.Next(0, 3);
            card.GeneralVictoryPointsBonus = _random.Next(0, 2);
            card.FinancierVictoryPointsBonus = _random.Next(0, 2);

            if (_random.Next(100) < 20)
            {
                card.SpecialAbility = (SpecialAbility)_random.Next(1, Enum.GetValues(typeof(SpecialAbility)).Length);
            }
            else
            {
                card.SpecialAbility = SpecialAbility.None;
            }

            return card;
        }

        private Dictionary<ResourceType, int> GenerateRandomResources(int minResources, int maxResources)
        {
            var resources = new Dictionary<ResourceType, int>();
            int resourceCount = _random.Next(minResources, maxResources + 1);

            for (int i = 0; i < resourceCount; i++)
            {
                ResourceType resourceType = (ResourceType)_random.Next(0, Enum.GetValues(typeof(ResourceType)).Length);
                if (!resources.ContainsKey(resourceType))
                {
                    resources[resourceType] = 0;
                }
                resources[resourceType] += _random.Next(1, 4);
            }

            return resources;
        }

        private ResourceType GenerateRandomRecyclingBonus()
        {
            ResourceType[] validTypes = { ResourceType.Materials, ResourceType.Energy, ResourceType.Science, ResourceType.Gold, ResourceType.Exploration };
            return validTypes[_random.Next(validTypes.Length)];
        }
    }
}