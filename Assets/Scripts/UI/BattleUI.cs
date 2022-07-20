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
    
    private List<IEnumerator> pendingAnimations = new();
    private IEnumerator currentAnimation = null;
    private bool isPlayingAnimation = false;

    private bool forceSwitchPendingP1 = false;
    private bool forceSwitchPendingP2 = false;

    public Sprite MoveButton_AttackType;
    public Sprite MoveButton_DefenseType;
    public Sprite MoveButton_NeutralType;

    public List<UseMoveButton> moveButtons;
    public List<SwapCreatureButton> switchButtons;

    public Image creatureP1;
    public Image creatureP2;

    private void Awake()
    {
        Instance = this;
        IUI.Instance = this;
    }

    //
    // Animation functions
    //

    // change the sprite shown for "team" to the sprite corresponding to "switchTo"
    public void SwapActiveCreature(int team, CreatureController switchTo)
    {
        
        if (team == 0)
        {
            foreach(var moveButton in moveButtons) moveButton.HandleSwitchIn(switchTo);
            foreach(var swapButton in switchButtons) swapButton.HandleSwitchIn(switchTo);
        }

        creatureP1.sprite = TurnManager.Instance.GetActiveCreature(0).state.definition.sprite;
        creatureP2.sprite = TurnManager.Instance.GetActiveCreature(1).state.definition.sprite;

        // TODO: move this stuff to SwapActiveCreatureAnimation, and queue it up in pendingAnimations
    }

    IEnumerator SwapActiveCreatureAnimation(int team, CreatureController switchTo)
    {
        if (team == 0)
        {
            foreach(var moveButton in moveButtons) moveButton.HandleSwitchIn(switchTo);
            foreach(var swapButton in switchButtons) swapButton.HandleSwitchIn(switchTo);
        }

        // TODO: yield return wait for swap animation to complete
        // TODO: switch the sprite of team's creature


        yield break;
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

    IEnumerator StatusEffectGainEffect(CreatureController creatureRecievingStatus, CreatureController.StatusContidion condition)
    {
        // enable the correct status icon

        // start the status effect animation
        // yeild return wait until the animation is done

        yield break;
    }

    //
    // Control functions
    //

    // force this player into the switch menu, as if they had opened it themself.
    // do not allow them to close it. once they've made a selection, call `callback(MakeMoveAction(...))`
    public void ForceSwitch(int player)
    {
        Debug.LogWarning($"Forcing player {player+1} to switch creatures");
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
        Debug.LogWarning("Turn manager is ready");
        // the turn manager has 

        for(int q = 0; q < 3; q++)
        {
            switchButtons[q].UpdateMe(TurnManager.Instance.players[0].team[q]);
        }

        creatureP1.sprite = TurnManager.Instance.GetActiveCreature(0).state.definition.sprite;
        creatureP2.sprite = TurnManager.Instance.GetActiveCreature(1).state.definition.sprite;

        // if there's an AI for either player, submit their move now
        if (player2AI != null)
        {
            PlayerAction action = player2AI.GetAction(ITurnManager.PLAYER_2, ITurnManager.Instance.CurrentGameState);
            ITurnManager.Instance.SubmitAction(action);
        }
    }


    public GameObject winCanvas;
    public GameObject loseCanvas;
    // -1 means tie, 0 means player 1 won, 1 means player 2 won
    public void GameOver(int winningPlayer)
    {
        ActionLogger.LogMessage($"Game over! Player {winningPlayer+1} won!");
        winCanvas.SetActive(winningPlayer == 0);
        loseCanvas.SetActive(winningPlayer != 0);
    }

    //
    // UI Functions
    //

    public void DisplayMainMenu()
    {
        menuUI.SetActive(true);
        actionsUI.SetActive(false);
        swapUI.SetActive(false);
        statsUI.SetActive(false);
    }

    public void DisplayActionsMenu()
    {
        menuUI.SetActive(false);
        actionsUI.SetActive(true);
        swapUI.SetActive(false);
        statsUI.SetActive(false);
    }

    public void DisplaySwapMenu()
    {
        menuUI.SetActive(false);
        actionsUI.SetActive(false);
        swapUI.SetActive(true);
        statsUI.SetActive(false);
    }

    public void DisplayStats()
    {
        menuUI.SetActive(false);
        actionsUI.SetActive(false);
        swapUI.SetActive(false);
        statsUI.SetActive(true);
    }

    private void Start()
    {
        DisplayMainMenu();
        AudioManager.Instance.Play("BattleTheme", true);
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

    public IEnumerator currentTurn;

    private void Update()
    {
        // play pending animations
        if (!isPlayingAnimation && pendingAnimations.Count > 0)
        {
            IEnumerator animationToPlay = pendingAnimations[0];
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
                DisplaySwapMenu();
                // TODO: disable the exit button
                forceSwitchPendingP1 = false;
            } 
            else if (forceSwitchPendingP2)
            {
                DisplaySwapMenu();
                // TODO: disable the exit button
                forceSwitchPendingP2 = false;
            } 
            else if (currentTurn != null)
            {
                var nextActionExists = currentTurn.MoveNext();
                if (!nextActionExists) currentTurn = null;
            }
        }
    }
}
