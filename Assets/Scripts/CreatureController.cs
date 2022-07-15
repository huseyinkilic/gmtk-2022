using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public class CreatureController : MonoBehaviour
{
    public struct CreatureDefinition
    {
        public string name;
        public int hp;
        public int attack;
        public int defense;
        public int speed;

        public int[] allowedMoves;
    }

    public struct CreatureState
    {
        public int currentDamage;
        public bool isActiveCreature;
    }

    public CreatureDefinition definition;
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
