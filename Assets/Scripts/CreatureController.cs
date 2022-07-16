using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public class CreatureController
{
    public class CreatureState
    {
        public int team;
        public int indexOnTeam;

        public int currentDamage;
        public bool isActiveCreature;

        public Creature definition;
        public Creature.Type currentType;

        public Move[] knownMoves;

        public int attackBuffLevel;
        public int defenseBuffLevel;
        public int speedBuffLevel;
    }

    public CreatureState state;
    

    public bool CanStillFight()
    {
        return state.currentDamage < state.definition.hp;
    }

    
    public float GetSpeed(FieldState fieldState, SingleSidedFieldState singleSidedFieldState)
    {
        return GetSpeedStat(state);
    }

    public void ApplyEndOfTurnEffects()
    {

    }

    public void ApplyStartOfTurnEffects()
    {

    }

    public void TakeDamage(float damage) { TakeDamage(Mathf.FloorToInt(damage)); }
    public void TakeDamage(int damage)
    {
        state.currentDamage += damage;
        IUI.Instance.PlayDamageEffect(this);
    }
    
    public static float BuffLevelToMultiplier(int level)
    {
        switch(level)
        {
            case 6: return 4.0f;
            case 5: return 3.5f;
            case 4: return 3.0f;
            case 3: return 2.5f;
            case 2: return 2.0f;
            case 1: return 1.5f;

            case 0: return 1.0f;

            case -1: return 0.666f;
            case -2: return 0.5f;
            case -3: return 0.4f;
            case -4: return 0.333f;
            case -5: return 0.285f;
            case -6: return 0.25f;
        }

        return 1;
    }

    public static float GetAttackStat(CreatureState creature)
    {
        return creature.definition.attack*BuffLevelToMultiplier(creature.attackBuffLevel);
    }
    
    public static float GetDefenseStat(CreatureState creature)
    {
        return creature.definition.defense*BuffLevelToMultiplier(creature.defenseBuffLevel);
    }
    
    public static float GetSpeedStat(CreatureState creature)
    {
        return creature.definition.defense*BuffLevelToMultiplier(creature.speedBuffLevel);
    }
}
