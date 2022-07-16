using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CreatureController;

public class UIHelper : MonoBehaviour
{
    void Start()
    {
        DebugInitialize();
    }

    public void DebugInitialize()
    {
        var creatures = Database.Instance.GetAllCreatures();
        for (int i = 0; i < 2; i++)
        {
            CreatureState[] team = new CreatureState[3];
            for (int j = 0; j < team.Length; j++)
            {
                // definition, knownMoves
                team[j] = new CreatureState()
                {
                    definition = creatures[j], // just use the first 3 creatures listed in the database
                    knownMoves = new Move[]
                    {
                        Database.Instance.MoveFromID(creatures[j].allowedMoves[0]), // just use the first 4 allowable moves
                        Database.Instance.MoveFromID(creatures[j].allowedMoves[1]),
                        Database.Instance.MoveFromID(creatures[j].allowedMoves[2]),
                        Database.Instance.MoveFromID(creatures[j].allowedMoves[3])
                    }
                };
            }

            TurnManager.Instance.InitializePlayer(i, team);
        }   
    }
}
