using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Pokemon/Move")]
public class Move : ScriptableObject
{
    public new string name;
    public int id;
    public int accuracy; // 0-100
    public int basePower;
    public int delayTurns;

    [SerializeField]
    public List<SecondaryEffect> secondaryEffects = new List<SecondaryEffect>();

    [System.Serializable]
    public class SecondaryEffect
    {
        public string name;
        public string[] parameters;
    }
}
