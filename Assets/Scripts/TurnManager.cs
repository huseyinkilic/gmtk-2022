using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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

    public struct TeamState
    {
        public CreatureState[] team;
    }

    public struct State
    {
        public int turnNumber;

        public float luckBalance; // positive means in favor of player1, negative means in favor of player2
        public TeamState[] playersTeams;

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
        public int team; // 0 for player 1, 1 for player 2
        public CreatureController activeCreature;

        public bool isSwitchAction;
        public CreatureController switchToCreature;

        public Move moveTaken;
    }



    public class PlayerController // Temp, will make its own file later
    {
        public List<CreatureController> team;
        public CreatureController activeCreature;
    }

    public List<State> previousStates;
    public State currentState;
    public List<PlayerController> players;

    public void RunTurn(List<PlayerAction> playerActions)
    {
        // sort actions by priority breaking ties by speed
        playerActions.Sort((PlayerAction a, PlayerAction b) => b.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[b.team]) - a.activeCreature.GetSpeed(currentState.fieldState, currentState.playersSideStates[a.team]));
        playerActions.Sort((PlayerAction a, PlayerAction b) => GetPriority(b, currentState) - GetPriority(a, currentState));

        foreach(PlayerAction action in playerActions)
        {
            PlayerController player = players[action.team];
            SingleSidedFieldState playerFieldSideState = currentState.playersSideStates[action.team];

            if (action.isSwitchAction)
            {
                action.activeCreature.state.isActiveCreature = false;
                action.switchToCreature.state.isActiveCreature = true;
                
                CreatureController switchFrom = player.team[action.switchToCreature.state.indexOnTeam];
                CreatureController switchTo   = player.team[action.activeCreature.state.indexOnTeam];
                player.activeCreature = player.team[action.switchToCreature.state.indexOnTeam];
                
                ApplySwapEffects(switchFrom, switchTo, currentState.fieldState, playerFieldSideState);
            } 
            else
            {
                // take move
            }
        }

        CopyStateToStack();
    }

    private void CopyStateToStack()
    {
        previousStates.Add(DeepCopy(currentState));
    }

    private static int GetPriority(PlayerAction action, State state)
    {
        // state is here as a parameter in case we want to allow the state to affect priority later
        return action.isSwitchAction ? 9999 : action.moveTaken.priority;
    }

    private static void ApplySwapEffects(CreatureController from, CreatureController to, FieldState globalFieldState, SingleSidedFieldState playerSideFieldState)
    {
        
    }


    // https://stackoverflow.com/a/11336951/9643841s
    static public T DeepCopy<T>(T obj)
    {
        BinaryFormatter s = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            s.Serialize(ms, obj);
            ms.Position = 0;
            T t = (T)s.Deserialize(ms);

            return t;
        }
    }
}
