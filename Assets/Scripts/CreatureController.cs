using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public CreatureDefinition definition;
    public int currentDamage;

    public bool isActiveCreature;
    

    public bool IsValidSwitchIn()
    {
        return currentDamage < definition.hp;
    }

    public void ApplyEndOfTurnEffects()
    {

    }

    public void ApplyStartOfTurnEffects()
    {

    }
}
