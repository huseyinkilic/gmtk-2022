using UnityEngine;
using UnityEngine.UI;
using static TurnManager;
using static AudioManager;
using System.Collections.Generic;
using System.Collections;

public class BattleUI : MonoBehaviour, IUI
{
    public static BattleUI Instance { get; set; }

    public GameObject menuUI;
    public GameObject actionsUI;
    public GameObject swapUI;
    public GameObject statsUI;

    [HideInInspector] public IAI player2AI; // if player 2 is a human, leave null
    
    private List<Coroutine> pendingAnimations = new();
    private Coroutine currentAnimation = null;
    private bool isPlayingAnimation = false;

    private bool forceSwitchPendingP1 = false;
    private bool forceSwitchPendingP2 = false;

    private void Awake()
    {
        Instance = this;
    }

    //
    // Animation functions
    //

    // change the sprite shown for "team" to the sprite corresponding to "switchTo"
    public void SwapActiveCreature(int team, CreatureController switchTo)
    {
        // TODO: make some coroutine that plays an animation for this effect and exits when the animation is over, and add it to pendingAnimations
        // if no animation desired, make a coroutine that switches the creature sprite and immediately exits
    }

    // update the HP bar, play special effect, etc. No delay between calls
    public void PlayDamageEffect(CreatureController beingDamaged)
    {
        // TODO: make some coroutine that plays the damage effect and exits when the animation is over, and add it to pendingAnimations
    }

    public void PlayStatBuffEffect(CreatureController beingBuffed, string statBeingBuffed, int buffLevel)
    {
        // TODO: make some coroutine that plays an animation for this effect and exits when the animation is over, and add it to pendingAnimations
    }

    public void PlayStatusEffectGainEffect(CreatureController creatureRecievingStatus, CreatureController.StatusContidion condition)
    {
        // TODO: make some coroutine that plays an animation for this effect and exits when the animation is over, and add it to pendingAnimations
    }

    //
    // Control functions
    //

    // force this player into the switch menu, as if they had opened it themself.
    // do not allow them to close it. once they've made a selection, call `callback(MakeMoveAction(...))`
    public void ForceSwitch(int player)
    {
        // if this player is an AI, submit their switch action
        // otherwise, open the switch menu and disable the close button

        if (player == ITurnManager.PLAYER_2 && player2AI != null)
        {
            PlayerAction action = player2AI.ForceSwitch(player, ITurnManager.Instance.CurrentGameState);
            ITurnManager.Instance.SubmitAction(action);
        }
        else
        {
            // TODO: open the switch menu and disable the close button
            // TODO: wait for all pending animations to finish
            if (player == ITurnManager.PLAYER_1) forceSwitchPendingP1 = true;
            if (player == ITurnManager.PLAYER_2) forceSwitchPendingP2 = true;
        }
    }

    // called when a turn is over and the turn manager is ready to recieve input. Also called before the first turn, once both players have been initialized
    public void TurnManagerReadyToRecieveInput()
    {
        // the turn manager has 

        // if there's an AI for either player, submit their move now
        if (player2AI != null)
        {
            PlayerAction action = player2AI.GetAction(ITurnManager.PLAYER_2, ITurnManager.Instance.CurrentGameState);
            ITurnManager.Instance.SubmitAction(action);
        }
    }

    // -1 means tie, 0 means player 1 won, 1 means player 2 won
    public void GameOver(int winningPlayer)
    {
    }

    //
    // UI Functions
    //

    public void OnBattleClick()
    {
        menuUI.SetActive(false);
        actionsUI.SetActive(true);
        AudioManager.Instance.Play("BattleTheme", true);
    }

    public void OnSwapClick()
    {
        menuUI.SetActive(false);
        swapUI.SetActive(true);
        AudioManager.Instance.Play("LossTheme", false);
    }

    public void OnStatsClick()
    {
        menuUI.SetActive(false);
        statsUI.SetActive(true);
        AudioManager.Instance.Play("VictoryTheme", false);
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
        actionsUI.SetActive(false);
        swapUI.SetActive(false);
        statsUI.SetActive(false);
    }

    //
    // Animation handling
    //

    public bool IsPlayingAnimation () { return isPlayingAnimation || pendingAnimations.Count > 0; }
    private IEnumerator PlayAnimation()
    {
        yield return currentAnimation;
        
        currentAnimation = null;
        isPlayingAnimation = false;
    }

    private void Update()
    {
        // play pending animations
        if (!isPlayingAnimation && pendingAnimations.Count > 0)
        {
            Coroutine animationToPlay = pendingAnimations[0];
            currentAnimation = animationToPlay;
            isPlayingAnimation = true;
            pendingAnimations.RemoveAt(0);
            StartCoroutine("PlayAnimation");
        }

        // not playing an animation
        if (!IsPlayingAnimation())
        {   
            if (forceSwitchPendingP1)
            {
                // TODO: open switch menu
                forceSwitchPendingP1 = false;
            }
            if (forceSwitchPendingP2)
            {
                // TODO: open switch menu
                forceSwitchPendingP2 = false;
            }
        }
    }
}
