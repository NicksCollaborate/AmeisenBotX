using AmeisenBotX.Core.Movement.Pathfinding.Objects;
using AmeisenBotX.Core.Quest.Objects.Objectives;
using AmeisenBotX.Core.Quest.Objects.Quests;
using System.Collections.Generic;

namespace AmeisenBotX.Core.Quest.Quests.Durotar.RazorHill
{
    class QAPeonBurden : BotQuest
    {
        public QAPeonBurden(WowInterface wowInterface)
            : base(wowInterface, 2161, "A Peon's Burden", 1, 1,
                () => (wowInterface.ObjectManager.GetClosestWowUnitByNpcId(new List<int> { 6786 }), new Vector3(-599.45f, -4715.32f, 35.23f)),
                () => (wowInterface.ObjectManager.GetClosestWowUnitByNpcId(new List<int> { 6928 }), new Vector3(340.36f, -4686.29f, 16.54f)),
                null)
        {}
    }
}
