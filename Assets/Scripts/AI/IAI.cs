using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public interface IAI 
{
    public PlayerAction GetAction(int playerNum, GameState state);
    public PlayerAction ForceSwitch(int playerNum, GameState state);
}
