using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Database : MonoBehaviour
{
    public static Database Instance { get; private set; }


    private Dictionary<string, Move> movesByID = new();
    [SerializeField] private List<Move> moveDefinitions;
    [SerializeField] private List<Creature> creatureDefinitions;

    public Dictionary<string, Sprite> creatureSprites;
    public void Awake()
    {
        Instance = this;
        moveDefinitions.ForEach(move => { if (move != null) movesByID[move.id] = move; }); 
        creatureDefinitions = creatureDefinitions.Where(c => c != null).ToList();
        creatureDefinitions.Sort((a, b) => a.name.CompareTo(b.name));
    }

    public Move MoveFromID(string moveId)
    {
        if (!movesByID.ContainsKey(moveId)) Debug.LogError($"No move exists with id {moveId}");
        return movesByID.GetValueOrDefault(moveId);
    }

    public List<Move> GetLearnableMovesForCreature(Creature c)
    {
        return c.allowedMoves.Select(moveId => MoveFromID(moveId)).ToList();
    }

    public List<Creature> GetAllCreatures()
    {
        return creatureDefinitions.ToList();
    }
}
