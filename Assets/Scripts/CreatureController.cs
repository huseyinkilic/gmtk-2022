using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public class CreatureController
{
    public enum StatusContidion { NONE, SLEEP, BURN, PARALYZED, POISONED }

    public static readonly int MAX_SLEEP_TURNS = 3;

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

        public StatusContidion status;
        public bool movesDisabled;
        public float turnsSleepingFor;
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
        if (state.status == StatusContidion.BURN || state.status == StatusContidion.POISONED) TakeDamage(state.definition.hp/16f);
    }

    public void ApplyStartOfTurnEffects()
    {
        // determine paralyzed / sleep wakeup
        if (state.status == StatusContidion.PARALYZED) state.movesDisabled = !TurnManager.Instance.MakeBooleanRoll(0.75f, state.team);
        if (state.status == StatusContidion.SLEEP)
        {
             if (state.turnsSleepingFor <= 0) state.status = StatusContidion.NONE;
             else state.movesDisabled = true;
        }
    }

    public void ApplyStatusCondition(StatusContidion condition)
    {
        if (state.status != StatusContidion.NONE) return;

        if (condition == StatusContidion.SLEEP) state.turnsSleepingFor = (MAX_SLEEP_TURNS+1)-TurnManager.Instance.MakeDiceRoll(state.team, 1, MAX_SLEEP_TURNS);
        state.status = condition;
        IUI.Instance.PlayStatusEffectGainEffect(this, condition);
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
        var statusStatDrop = creature.status == StatusContidion.BURN ? 0.5f : 1;
        return creature.definition.attack * BuffLevelToMultiplier(creature.attackBuffLevel) * statusStatDrop;
    }
    
    public static float GetDefenseStat(CreatureState creature)
    {
        var statusStatDrop = creature.status == StatusContidion.POISONED ? 0.5f : 1;
        return creature.definition.defense * BuffLevelToMultiplier(creature.defenseBuffLevel) * statusStatDrop;
    }
    
    public static float GetSpeedStat(CreatureState creature)
    {
        var statusStatDrop = creature.status == StatusContidion.PARALYZED ? 0.5f : 1;
        return creature.definition.speed * BuffLevelToMultiplier(creature.speedBuffLevel) * statusStatDrop;
    }
}
