﻿using System;
using AmeisenBotX.Core.Data.Objects.WowObjects;
using AmeisenBotX.Core.Movement.Enums;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AmeisenBotX.Core.Data.CombatLog.Enums;
using AmeisenBotX.Core.Data.CombatLog.Objects;
using AmeisenBotX.Core.Movement.Pathfinding.Objects;

namespace AmeisenBotX.Core.Quest.Objects.Objectives
{
    public class KillAndLootQuestObjective : IQuestObjective, IObserverBasicCombatLogEntry
    {
        public KillAndLootQuestObjective(WowInterface wowInterface, List<int> npcIds, int collectOrKillAmount, int questItemId, List<List<Vector3>> areas)
        {
            WowInterface = wowInterface;
            NpcIds = npcIds;
            CollectOrKillAmount = collectOrKillAmount;
            QuestItemId = questItemId;
            SearchAreas = new SearchAreaEnsamble(areas);

            if (!CollectQuestItem)
            {
                wowInterface.Db.GetCombatLogSubject().Register(this);
            }
        }

        private bool CollectQuestItem => QuestItemId > 0;
        
        private int QuestItemId { get; }
        
        private List<int> NpcIds { get; }
        
        private int CollectOrKillAmount { get; }
        
        private SearchAreaEnsamble SearchAreas { get; }
        
        private int Killed { get; set; }
        
        public bool Finished => Math.Abs(Progress - 100.0f) < 0.00001;

        public double Progress
        {
            get
            {
                if (CollectOrKillAmount == 0)
                {
                    return 100.0;
                }

                var amount = Killed;
                if (CollectQuestItem)
                {
                    var inventoryItem =
                        WowInterface.CharacterManager.Inventory.Items.Find(item => item.Id == QuestItemId);
                    if (inventoryItem != null)
                    {
                        amount = inventoryItem.Count;
                    }
                    else
                    {
                        return 0.0;
                    }
                } 
                
                return Math.Min(100.0 * ((float)amount) / ((float) CollectOrKillAmount), 100.0);
            }
        }

        private WowInterface WowInterface { get; }

        private WowUnit WowUnit { get; set; }
        
        private DateTime LastUnitCheck { get; set; } = DateTime.Now;

        public void Execute()
        {
            if (Finished || WowInterface.ObjectManager.Player.IsCasting) { return; }

            if (!WowInterface.ObjectManager.Player.IsInCombat && DateTime.Now.Subtract(LastUnitCheck).TotalMilliseconds >= 1250.0)
            {
                LastUnitCheck = DateTime.Now;
                WowUnit = WowInterface.ObjectManager.WowObjects
                    .OfType<WowUnit>()
                    .Where(e => !e.IsDead && NpcIds.Contains(WowGUID.NpcId(e.Guid)) && !e.IsNotAttackable 
                                && WowInterface.HookManager.WowGetUnitReaction(WowInterface.ObjectManager.Player, e) != WowUnitReaction.Friendly)
                    .OrderBy(e => e.Position.GetDistance(WowInterface.ObjectManager.Player.Position))
                    .Take(3)
                    .OrderBy(e => WowInterface.PathfindingHandler.GetPathDistance((int)WowInterface.ObjectManager.MapId, WowInterface.ObjectManager.Player.Position, e.Position))
                    .FirstOrDefault();
                
                // Kill enemies in the path
                if (WowUnit != null && !WowInterface.CombatClass.IsTargetAttackable(WowUnit))
                {
                    var path = WowInterface.PathfindingHandler.GetPath((int)WowInterface.ObjectManager.MapId,
                    WowInterface.ObjectManager.Player.Position, WowUnit.Position);
                    if (path != null)
                    {
                        var nearEnemies =
                            WowInterface.ObjectManager.GetHostilesInPath<WowUnit>(path, 10.0);
                        if (nearEnemies.Any())
                        {
                            WowUnit = nearEnemies.FirstOrDefault();
                        }
                    }
                }

                if (WowUnit != null)
                {
                    WowInterface.HookManager.WowTargetGuid(WowUnit.Guid);
                }
            }

            if (WowUnit != null)
            {
                SearchAreas.NotifyDetour();
                WowInterface.CombatClass.AttackTarget();
            }
            else if (WowInterface.MovementEngine.IsAtTargetPosition || SearchAreas.HasAbortedPath() || WowInterface.MovementEngine.MovementAction == MovementAction.None)
            {
                WowInterface.MovementEngine.SetMovementAction(MovementAction.Moving,
                    SearchAreas.GetNextPosition(WowInterface));
            }
        }

        public void CombatLogChanged(BasicCombatLogEntry entry)
        {
            if (entry.Subtype == CombatLogEntrySubtype.KILL && NpcIds.Contains(WowGUID.NpcId(entry.DestinationGuid)))
            {
                ++Killed;
            }
        }
    }
}