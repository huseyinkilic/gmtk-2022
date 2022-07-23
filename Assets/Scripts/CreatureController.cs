using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public class CreatureController
{
    public enum StatusContidion { NONE, SLEEP, BURN, PARALYZED, POISONED }

    public static readonly int MAX_SLEEP_TURNS = 3;

    [Serializable]
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

        if (state.status == StatusContidion.BURN) ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} took {state.definition.hp/16f} damage from its burn!");
        if (state.status == StatusContidion.POISONED) ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} took {state.definition.hp/16f} damage from poison!");
    }

    public void ApplyStartOfTurnEffects()
    {
        // determine paralyzed / sleep wakeup
        if (state.status == StatusContidion.PARALYZED) 
        {  
            state.movesDisabled = !TurnManager.Instance.MakeBooleanRoll(0.75f, state.team);
            if (state.movesDisabled) ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} flinched due to paralysis and will not move this turn!");
        }

        if (state.status == StatusContidion.SLEEP)
        {
            if (state.turnsSleepingFor <= 0) 
            { 
                state.status = StatusContidion.NONE;
                ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} woke up!");
            }
            else 
            {
                state.movesDisabled = true;
                ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} is fast asleep!");
            }
        }
    }

    public void ApplyStatusCondition(StatusContidion condition)
    {
        if (state.status != StatusContidion.NONE) return;

        if (condition == StatusContidion.SLEEP) state.turnsSleepingFor = (MAX_SLEEP_TURNS+1)-TurnManager.Instance.MakeDiceRoll(state.team, 1, MAX_SLEEP_TURNS);
        state.status = condition;
        IUI.Instance.PlayStatusEffectGainEffect(this, condition);

        // log
        string conditionMessage = "";
        if (condition == StatusContidion.SLEEP) conditionMessage = "fell asleep";
        if (condition == StatusContidion.POISONED) conditionMessage = "was poisoned";
        if (condition == StatusContidion.PARALYZED) conditionMessage = "was paralyzed";
        if (condition == StatusContidion.BURN) conditionMessage = "was burned";
        if (condition == StatusContidion.BURN) conditionMessage = "gained a status condition";
        ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} {conditionMessage}!");

        ApplyStartOfTurnEffects();
    }

    public void TakeDamage(float damage) { TakeDamage(Mathf.FloorToInt(damage)); }
    public void TakeDamage(int damage)
    {
        state.currentDamage += damage;
        if (damage > 0)  IUI.Instance.PlayDamageEffect(this);
        if (damage == 0) BattleUI.Instance.PlayNoDamageEffect(this);
        if (damage > 0) ActionLogger.LogMessage($"Player {state.team+1}'s {state.definition.name} took {damage} damage!");
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
