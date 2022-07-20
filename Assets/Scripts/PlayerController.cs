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
        // determine if the game is over
        var player1Lost = TurnManager.Instance.players[0].team.All(c => !c.CanStillFight());
        var player2Lost = TurnManager.Instance.players[1].team.All(c => !c.CanStillFight());
        if (player1Lost && player2Lost) { IUI.Instance.GameOver(-1);                    return; }
        else if (player1Lost)           { IUI.Instance.GameOver(ITurnManager.PLAYER_2); return; }
        else if (player2Lost)           { IUI.Instance.GameOver(ITurnManager.PLAYER_1); return; }


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
