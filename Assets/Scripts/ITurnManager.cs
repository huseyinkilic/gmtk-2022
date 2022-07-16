using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public interface ITurnManager 
{
    public static ITurnManager Instance { get; }
    public static readonly int PLAYER_1 = 0;
    public static readonly int PLAYER_2 = 1;

    //
    // SETTERS
    //

    // once both players have submitted an action of either type, the TurnManager automatically runs the turn
    // how to use:
        // when player1 clicks the second move button, call
        // ITurnManager.SubmitAction(ITurnManager.MakeMoveAction(ITurnManager.PLAYER_1, 1, ITurnManager.GetActiveCreature(ITurnManager.PLAYER_2)))

        // when player1 clicks the button to switch to their creature at index 3, call 
        // ITurnManager.SubmitAction(ITurnManager.MakeMoveAction(ITurnManager.PLAYER_1, 3))
    public PlayerAction MakeSwitchAction(int playerNum, int switchToIndex); 
    public PlayerAction MakeMoveAction(int playerNum, int moveIndex, CreatureController targetCreature);
    public void SubmitAction(PlayerAction action); 

    //
    // GETTERS
    //

    public int TurnNumber { get; }
    public float LuckBalance { get; }

    public float GetLuckAdustedAccuracy(int playerNum, Move move);

    public int GetActiveCreatureCurrentHP(int playerNum);
    public int GetActiveCreatureMaxHP(int playerNum);
    public CreatureController GetActiveCreature(int playerNum);

    public Move[] GetUsableMoves(int playerNum); // returns a list of moves that the the player can use this turn
    public List<CreatureController> GetPlayerCreatures(int playerNum); // useful for displaying the switch menu
}
