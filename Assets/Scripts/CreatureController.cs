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
    }

    public Creature definition;
    public CreatureState state;
    

    public bool IsValidSwitchIn()
    {
        return state.currentDamage < definition.hp;
    }

    
    public int GetSpeed(FieldState fieldState, SingleSidedFieldState singleSidedFieldState)
    {
        return definition.speed;
    }

    public void ApplyEndOfTurnEffects()
    {

    }

    public void ApplyStartOfTurnEffects()
    {

    }
}
