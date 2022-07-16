using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

public interface UIInterface
{
    public static UIInterface Instance { get; }

    public void SwapActiveCreature(int team, CreatureController switchTo); // change the sprite shown for "team" to the sprite corresponding to "switchTo"
    public void PlayDamageEffect(CreatureController beingDamaged); // update the HP bar, play special effect, etc. No delay between calls
    public void ForceSwitch(int player, HandleActionDelegate callback); // force this player into the switch menu, as if they had opened it themself. do not allow them to close it. once they've made a selection, call `callback(MakeMoveAction(...))`
    public void TurnManagerReadyToRecieveInput(); // called when a turn is over and the turn manager is ready to recieve input

    public void GameOver(int winningPlayer); // -1 means tie, 0 means player 1 won, 1 means player 2 won
}