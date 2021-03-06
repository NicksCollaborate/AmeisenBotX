﻿using AmeisenBotX.Core.Common;
using AmeisenBotX.Core.Data.Objects.WowObjects;
using AmeisenBotX.Core.Movement.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using AmeisenBotX.Core.Movement.Pathfinding.Objects;

namespace AmeisenBotX.Core.Quest.Objects.Objectives
{
    public class CollectQuestObjective : IQuestObjective
    {
        public CollectQuestObjective(WowInterface wowInterface, int itemId, int itemAmount, List<int> gameObjectIds, List<Vector3> positions)
        {
            WowInterface = wowInterface;
            ItemId = itemId;
            WantedItemAmount = itemAmount;
            GameObjectIds = gameObjectIds;
            Area = positions.Select(pos => new AreaNode(pos, 10.0)).ToList();
            RightClickEvent = new TimegatedEvent(TimeSpan.FromMilliseconds(1500));
        }

        public List<AreaNode> Area { get; set; }

        public bool Finished => Math.Abs(Progress - 100.0) < 0.0001;

        public double Progress
        {
            get
            {
                if (WantedItemAmount == 0)
                {
                    return 100.0;
                }
                
                var inventoryItem = WowInterface.CharacterManager.Inventory.Items.Find(item => item.Id == ItemId);
                return inventoryItem != null ? Math.Min(100.0 * ((float)inventoryItem.Count) / ((float)WantedItemAmount), 100.0) : 0.0;
            }
        }

        private int CurrentItemAmount => WowInterface.CharacterManager.Inventory.Items.Count(e => e.Id == ItemId);

        private int ItemId { get; }

        private List<int> GameObjectIds { get; }

        private TimegatedEvent RightClickEvent { get; }

        private int WantedItemAmount { get; }

        private WowInterface WowInterface { get; }

        public void Execute()
        {
            if (Finished) { return; }

            WowGameobject lootableObject = WowInterface.ObjectManager.WowObjects.OfType<WowGameobject>()
                .Where(e => GameObjectIds.Contains(e.EntryId))
                .OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position))
                .FirstOrDefault();

            if (lootableObject != null)
            {
                if (lootableObject.Position.GetDistance(WowInterface.ObjectManager.Player.Position) > 5.0)
                {
                    WowInterface.MovementEngine.SetMovementAction(MovementAction.Moving, lootableObject.Position);
                }
                else
                {
                    if (RightClickEvent.Run())
                    {
                        WowInterface.MovementEngine.Reset();
                        WowInterface.HookManager.WowStopClickToMove();
                        WowInterface.HookManager.WowObjectRightClick(lootableObject);
                    }
                }
            }
            else
            {
                AreaNode selectedArea = Area
                    .OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position))
                    .FirstOrDefault(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position) < e.Radius);

                if (selectedArea != null)
                {
                    WowInterface.MovementEngine.SetMovementAction(MovementAction.Moving, selectedArea.Position);
                }
            }
        }
    }
}