using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CreatureController;

// Misnamed, this is the debug helper
public class UIHelper : MonoBehaviour
{
    void Start()
    {
        DebugInitialize();
    }

    public void DebugInitialize()
    {
        BattleUI.Instance.player2AI = new SimpleAI();

        var creatures = Database.Instance.GetAllCreatures();
        for (int i = 0; i < 2; i++)
        {
            CreatureState[] team = new CreatureState[3];
            for (int j = 0; j < team.Length; j++)
            {
                // definition, knownMoves
                team[j] = new CreatureState()
                {
                    definition = creatures[creatures.Count-1-j], // just use the last 3 creatures listed in the database (test creatures)
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
