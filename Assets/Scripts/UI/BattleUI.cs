using UnityEngine;
using UnityEngine.UI;
using static TurnManager;

public class BattleUI : MonoBehaviour, IUI
{

    public GameObject menuUI;
    public GameObject buffsUI;
    public GameObject swapUI;
    public GameObject statsUI;

    // change the sprite shown for "team" to the sprite corresponding to "switchTo"
    public void SwapActiveCreature(int team, CreatureController switchTo)
    {
    }

    // update the HP bar, play special effect, etc. No delay between calls
    public void PlayDamageEffect(CreatureController beingDamaged)
    {
    }

    public void PlayStatBuffEffect(CreatureController beingBuffed, string statBeingBuffed, int buffLevel)
    {
    }

    // force this player into the switch menu, as if they had opened it themself.
    // do not allow them to close it. once they've made a selection, call `callback(MakeMoveAction(...))`
    public void ForceSwitch(int player, HandleActionDelegate callback)
    {
    }

    // called when a turn is over and the turn manager is ready to recieve input. Also called before the first turn, once both players have been initialized
    public void TurnManagerReadyToRecieveInput()
    {
    }

    // -1 means tie, 0 means player 1 won, 1 means player 2 won
    public void GameOver(int winningPlayer)
    {
    }

    public void OnBattleClick()
    {
        menuUI.SetActive(false);
        buffsUI.SetActive(true);
    }

    public void OnSwapClick()
    {
        menuUI.SetActive(false);
        swapUI.SetActive(true);
    }

    public void OnStatsClick()
    {
        menuUI.SetActive(false);
        statsUI.SetActive(true);
    }

    public void OnBackClick()
    {
        menuUI.SetActive(true);
        swapUI.SetActive(false);
        statsUI.SetActive(false);
    }

    private void Start()
    {
        menuUI.SetActive(true);
        buffsUI.SetActive(false);
        swapUI.SetActive(false);
        statsUI.SetActive(false);
    }

    private void Update()
    {
        
    }
}
