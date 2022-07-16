using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseMoveButton : MonoBehaviour
{
    public int team;
    public int moveIndex;

    public void MakeAndSubmitAction()
    {
        int opposingTeam = 1-team;
        var action = TurnManager.Instance.MakeMoveAction(team, moveIndex, TurnManager.Instance.GetActiveCreature(opposingTeam));
        TurnManager.Instance.SubmitAction(action);
    }
}
