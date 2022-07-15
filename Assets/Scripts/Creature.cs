using UnityEngine;

[CreateAssetMenu(fileName = "Creature", menuName = "Pokemon/Creature")]
public class Creature : ScriptableObject
{
    public new string name;
    public int hp;
    public int attack;
    public int defense;
    public int speed;
    public int[] allowedMoves;
}
