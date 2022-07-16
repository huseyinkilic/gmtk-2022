using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public class CreatureController : MonoBehaviour
{
    public struct CreatureState
    {
        public int team;
        public int indexOnTeam;

        public int currentDamage;
        public bool isActiveCreature;

        public Creature definition;
    }

    public CreatureState state;
    

    public bool CanStillFight()
    {
        return state.currentDamage < state.definition.hp;
    }

    
    public int GetSpeed(FieldState fieldState, SingleSidedFieldState singleSidedFieldState)
    {
        return state.definition.speed;
    }

    public void ApplyEndOfTurnEffects()
    {

    }

    public void ApplyStartOfTurnEffects()
    {

    }

    public void TakeDamage(int damage)
    {
        state.currentDamage += damage;
        UIInterface.Instance.PlayDamageEffect(this);
    }
}
