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

    public class State
    {
        public float luckBalance; // positive means in favor of player1, negative means in favor of player2
        public CreatureState[] player1Team;
        public CreatureState[] player2Team;
    }

    public class Move
    {
        public int priority = 0; // stub, in case we want to add this

        public string name;
        public int id;
        public int power;
        public int accuracy;
        public int delayTurns;
        public Type type;
    }

    public List<Move> pendingMoves;


    public class PlayerAction
    {
        public bool isSwitchAction;
        public int switchToIndex;

        public Move moveTaken;
    }

    public void RunTurn(PlayerAction player1Action, PlayerAction player2Action)
    {
        
    }
}
