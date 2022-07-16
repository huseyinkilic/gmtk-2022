using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Creature", menuName = "Pokemon/Creature")]
public class Creature : ScriptableObject
{
    [Serializable]
    public enum Type
    {
        ATTACK, DEFEND, NEUTRAL
    }

    public new string name;
    public int hp;
    public int attack;
    public int defense;
    public int speed;
    public Type type;

    public string[] allowedMoves;
}
