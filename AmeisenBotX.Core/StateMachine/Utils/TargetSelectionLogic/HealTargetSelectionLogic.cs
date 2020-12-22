using AmeisenBotX.Core.Data.Objects.WowObjects;
using System.Collections.Generic;
using System.Linq;

namespace AmeisenBotX.Core.Statemachine.Utils.TargetSelectionLogic
{
    public class HealTargetSelectionLogic : ITargetSelectionLogic
    {
        public HealTargetSelectionLogic(WowInterface wowInterface, AmeisenBotConfig config)
        {
            WowInterface = wowInterface;
            Config = config;
        }

        public IEnumerable<int> BlacklistedTargets { get; set; }

        public IEnumerable<int> PriorityTargets { get; set; }

        private WowInterface WowInterface { get; }

        private AmeisenBotConfig Config { get; }

        public void Reset()
        {
        }

        public bool SelectTarget(out IEnumerable<WowUnit> possibleTargets)
        {
            List<WowUnit> healableUnits = new List<WowUnit>(WowInterface.ObjectManager.Partymembers)
            {
                // healableUnits.AddRange(WowInterface.ObjectManager.PartyPets);
                WowInterface.ObjectManager.Player
            };

            // order by type id, so that players have priority
            possibleTargets = healableUnits
                .Where(e => e.Health < e.MaxHealth && !e.IsDead)
                .OrderByDescending(e => e.Type)
                .ThenByDescending(e => e.MaxHealth - e.Health);

            return possibleTargets.Any();
        }
    }
}