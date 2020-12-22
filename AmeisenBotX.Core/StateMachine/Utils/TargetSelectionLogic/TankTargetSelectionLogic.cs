﻿using AmeisenBotX.Core.Common;
using AmeisenBotX.Core.Data.Enums;
using AmeisenBotX.Core.Data.Objects.WowObjects;
using AmeisenBotX.Core.Movement.Pathfinding.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AmeisenBotX.Core.Statemachine.Utils.TargetSelectionLogic
{
    public class TankTargetSelectionLogic : ITargetSelectionLogic
    {
        public TankTargetSelectionLogic(WowInterface wowInterface, AmeisenBotConfig config)
        {
            WowInterface = wowInterface;
            PriorityTargets = new List<int>();
            BlacklistedTargets = new List<int>();
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
            if (WowInterface.ObjectManager.TargetGuid != 0
                && WowInterface.ObjectManager.Target != null
                && (WowInterface.ObjectManager.Target.IsDead
                || BlacklistedTargets.Contains(WowInterface.ObjectManager.Target.DisplayId)
                || WowInterface.HookManager.WowGetUnitReaction(WowInterface.ObjectManager.Player, WowInterface.ObjectManager.Target) == WowUnitReaction.Friendly
                || !BotUtils.IsValidUnit(WowInterface.ObjectManager.Target)))
            {
                WowInterface.HookManager.WowClearTarget();
                possibleTargets = null;
                return false;
            }

            Vector3 position = Config.StayCloseToGroupInCombat ? WowInterface.ObjectManager.MeanGroupPosition : WowInterface.ObjectManager.Player.Position;

            // get all enemies targeting our group
            IEnumerable<WowUnit> enemies = WowInterface.ObjectManager
                .GetEnemiesTargetingPartymembers<WowUnit>(position, 64.0)
                .Where(e => e.IsInCombat
                    && !BlacklistedTargets.Contains(e.DisplayId)
                    && !(WowInterface.ObjectManager.MapId == MapId.PitOfSaron && e.Name == "Rimefang")
                    && !(WowInterface.ObjectManager.MapId == MapId.HallsOfReflection && e.Name == "The Lich King")
                    && !(WowInterface.ObjectManager.MapId == MapId.DrakTharonKeep && WowInterface.ObjectManager.WowObjects.OfType<WowDynobject>().Any(e => e.SpellId == 47346) && e.Name.Contains("novos the summoner", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(e => e.Position.GetDistance(position));

            bool keepCurrentTarget = (WowInterface.ObjectManager.TargetGuid != 0 && WowInterface.ObjectManager.Target != null)
                && (WowInterface.ObjectManager.Target?.GetType() == typeof(WowPlayer)
                || WowInterface.ObjectManager.Target?.TargetGuid != WowInterface.ObjectManager.PlayerGuid
                || !enemies.Any(e => e.TargetGuid != WowInterface.ObjectManager.PlayerGuid));

            if (keepCurrentTarget)
            {
                possibleTargets = null;
                return false;
            }

            enemies = enemies.Concat(WowInterface.ObjectManager.GetNearEnemies<WowPlayer>(position, 64.0).Where(e => e.IsInCombat));

            if (enemies.Any())
            {
                // filter out enemies already attacking me (keep players for pvp)
                IEnumerable<WowUnit> enemiesNotTargetingMe = enemies
                    .Where(e => e.GetType() == typeof(WowPlayer) || e.TargetGuid != WowInterface.ObjectManager.PlayerGuid);

                if (enemiesNotTargetingMe.Any())
                {
                    IEnumerable<WowUnit> targetUnits = enemiesNotTargetingMe
                        .Where(e => WowInterface.ObjectManager.TargetGuid != e.Guid)
                        .OrderBy(e => e.GetType().Name) // make sure players are at the top (pvp)
                        .ThenBy(e => e.Position.GetDistance(position));

                    if (targetUnits != null && targetUnits.Any())
                    {
                        // target closest enemy
                        possibleTargets = targetUnits;
                        return true;
                    }
                }
                else
                {
                    // target the unit with the most health, likely to be the boss
                    IEnumerable<WowUnit> targetUnits = enemies
                        .Where(e => WowInterface.ObjectManager.TargetGuid != e.Guid
                            && !BlacklistedTargets.Contains(e.DisplayId))
                        .OrderBy(e => e.GetType().Name) // make sure players are at the top (pvp)
                        .ThenByDescending(e => e.Health);

                    if (targetUnits != null && targetUnits.Any())
                    {
                        // target closest enemy
                        possibleTargets = targetUnits;
                        return true;
                    }
                }

                possibleTargets = enemies.Where(e => e.IsTaggedByMe || !e.IsTaggedByOther).ToList();
                return true;
            }
            else
            {
                // get near players and attack them
                enemies = WowInterface.ObjectManager
                    .GetNearEnemies<WowPlayer>(position, 64.0)
                    .OrderBy(e => e.Position.GetDistance(position));

                if (enemies.Any())
                {
                    possibleTargets = enemies;
                    return true;
                }
            }

            possibleTargets = null;
            return false;
        }
    }
}