using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Pokemon/Move")]
public class Move : ScriptableObject
{
    [System.Serializable]
    public class SecondaryEffect
    {
        public string name;
        public string[] parameters;
    }

    public enum Type
    {
        ATTACK, DEFEND, NEUTRAL
    }

    public new string name;
    public string id;
    public int accuracy; // 0-100
    public int basePower;
    public int delayTurns;
    public int priority;
    public Type type;

    [SerializeField]
    public List<SecondaryEffect> secondaryEffects = new List<SecondaryEffect>();
}
