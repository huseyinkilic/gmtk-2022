using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CreatureController;

public class TurnManager : MonoBehaviour
{
    public class PlayerManager { }

    public enum Type
    {
        ATTACK, DEFEND, NEUTRAL
    }

    

    public struct FieldState
    {
        // stub, in case we want to add this
    }

    public struct SingleSidedFieldState 
    {
        // stub, in case we want to add this
    }

    public struct State
    {
        public int turnNumber;

        public float luckBalance; // positive means in favor of player1, negative means in favor of player2
        public CreatureState[] player1Team;
        public CreatureState[] player2Team;

        public FieldState fieldState; // stub, in case we want to add this
        public SingleSidedFieldState[] playersSideStates; // stub, in case we want to add this

        public List<Move> pendingMoves; // stub for now
    }

    public struct Move
    {
        public int priority; // stub, in case we want to add this

        public string name;
        public int id;
        public int power;
        public int accuracy;
        public int delayTurns;
        public Type type;
    }

    public struct PlayerAction
    {
        public int side; // 0 for player 1, 1 for player 2
        public CreatureController activeCreature;

        public bool isSwitchAction;
        public CreatureController switchToCreature;

        public Move moveTaken;
    }


    public State currentState;

    public void RunTurn(List<PlayerAction> playerActions)
    {
        // sort actions by priority breaking ties by speed
        playerActions.Sort((PlayerAction a, PlayerAction b) => b.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[b.side]) - a.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[a.side]));
        playerActions.Sort((PlayerAction a, PlayerAction b) => GetPriority(b, currentState) - GetPriority(a, currentState));

        foreach(PlayerAction action in playerActions)
        {
            
        }
    }

    private static int GetPriority(PlayerAction action, State state)
    {
        // state is here as a parameter in case we want to allow the state to affect priority later
        return action.isSwitchAction ? 9999 : action.moveTaken.priority;
    }

    private static void ApplySwapEffects(CreatureController from, CreatureController to, FieldState globalFieldState, SingleSidedFieldState playerSideFieldState)
    {
        
    }
}
