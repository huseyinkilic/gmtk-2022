using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static TurnManager;

public class PlayerController
{
    public int teamNumber;
    public List<CreatureController> team;
    public CreatureController activeCreature { get { return team.Where(creature => creature.state.isActiveCreature).FirstOrDefault(); } }

    public bool pendingForcedSwitch = false;

    public void ForceSwitch() 
    { 
        pendingForcedSwitch = true;
        IUI.Instance.ForceSwitch(this.teamNumber); 
    }

    public PlayerAction MakeSwitchAction(int switchToIndex)
    {
        Debug.LogError($"MAKING SWITCH ACTION {switchToIndex}");
        return new PlayerAction()
        {
            team = this.teamNumber,
            targetTeam = this.teamNumber,
            madeOnTurn = TurnManager.Instance.TurnNumber,
            activeCreature = activeCreature,
            actionType = PlayerAction.ActionType.SWITCH,
            targetCreature = team[switchToIndex],
            moveTaken = null
        };
    }

    public PlayerAction MakeMoveAction(int moveIndex, CreatureController targetCreature)
    {
        return new PlayerAction()
        {
            team = this.teamNumber,
            targetTeam = targetCreature.state.team,
            madeOnTurn = TurnManager.Instance.TurnNumber,
            activeCreature = activeCreature,
            actionType = PlayerAction.ActionType.MOVE,
            targetCreature = targetCreature,
            moveTaken = activeCreature.state.knownMoves[moveIndex]
        };
    }
}
