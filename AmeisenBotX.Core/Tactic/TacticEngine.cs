﻿using AmeisenBotX.Core.Statemachine.Enums;
using System.Collections.Generic;

namespace AmeisenBotX.Core.Tactic
{
    public class TacticEngine
    {
        public TacticEngine()
        {
            Tactics = new SortedList<int, ITactic>();
        }

        private SortedList<int, ITactic> Tactics { get; set; }

        public bool Execute(CombatClassRole role, bool isMelee, out bool preventMovement, out bool allowAttacking)
        {
            if (Tactics.Count > 0)
            {
                foreach (ITactic tactic in Tactics.Values)
                {
                    if (tactic.ExecuteTactic(role, isMelee, out preventMovement, out allowAttacking))
                    {
                        return true;
                    }
                }
            }

            preventMovement = false;
            allowAttacking = true;
            return false;
        }

        public void LoadTactics(params ITactic[] tactics)
        {
            Tactics = new SortedList<int, ITactic>();

            for (int i = 0; i < tactics.Length; ++i)
            {
                Tactics.Add(i, tactics[i]);
            }
        }

        public void Reset()
        {
            Tactics.Clear();
        }
        
        public bool HasTactics()
        {
            return Tactics.Count > 0;
        }
    }
}