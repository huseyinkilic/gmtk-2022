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

    public void Start()
    {
        Instance = this;
        moveDefinitions.ForEach(move => movesByID[move.id] = move); 
        creatureDefinitions.Sort((a, b) => a.name.CompareTo(b.name));
    }

    public Move MoveFromID(string moveId)
    {
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
