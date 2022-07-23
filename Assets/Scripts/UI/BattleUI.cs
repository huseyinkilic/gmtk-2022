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

    public List<Button> mainMenuButtons;

    [HideInInspector] public IAI player2AI; // if player 2 is a human, leave null
    
    private List<IEnumerator> pendingAnimations = new();
    private IEnumerator currentAnimation = null;
    private bool isPlayingAnimation = false;

    private bool forceSwitchPendingP1 = false;
    private bool forceSwitchPendingP2 = false;
    public Button switchMenuCloseButton;

    public Sprite MoveButton_AttackType;
    public Sprite MoveButton_DefenseType;
    public Sprite MoveButton_NeutralType;

    public List<UseMoveButton> moveButtons;
    public List<SwapCreatureButton> switchButtons;

    public Image creatureP1;
    public Image creatureP2;

    public Thing player1CreatureUI;
    public Thing player2CreatureUI;

    public LuckMeter luckMeter;

    // Animation clips

    public Animation luckyRollAnimation;
    public AnimationClip luckyRollAnimationClip;

    public Animation playerCreatureAnimation;
    public Animation opponentCreatureAnimation;
    public AnimationClip AttackIncreaseClip;
    public AnimationClip AttackDecreaseClip;
    public AnimationClip DefenseIncreaseClip;
    public AnimationClip DefenseDecreaseClip;
    public AnimationClip SpeedIncreaseClip;
    public AnimationClip SpeedDecreaseClip;
    public AnimationClip SlideInLeft;
    public AnimationClip SlideOutLeft;
    public AnimationClip SlideInRight;
    public AnimationClip SlideOutRight;
    public AnimationClip Hop;

    //public Animation playerCreatureEffectsAnimation;
    //public Animation opponentCreatureEffectsAnimation;
    //public AnimationClip damageClip;
    //public AnimationClip poisonClip;
    //public AnimationClip paralysisClip;
    //public AnimationClip burnClip;
    //public AnimationClip sleepClip;
    
    public UISpritesAnimation playerCreatureEffectsAnimation;
    public UISpritesAnimation opponentCreatureEffectsAnimation;
    public Sprite[] damageClip;
    public Sprite[] poisonClip;
    public Sprite[] paralysisClip;
    public Sprite[] burnClip;
    public Sprite[] sleepClip;

    private void Awake()
    {
        Instance = this;
        IUI.Instance = this;
    }

    //
    // Animation functions
    //

    private IEnumerator WaitForAnimation(Animation animation, AnimationClip clip, bool setEnabled = false, string sfxClip = null)
    {
        Debug.LogError("PLAYING ANIMATION");
        if (setEnabled) animation.gameObject.SetActive(true);

        if (sfxClip != null) SFXManager.Instance.Play(sfxClip);
        animation.clip = clip;
        animation.Play();
        yield return new WaitForSeconds(clip.length); //new WaitUntil(() => !animation.isPlaying);

        if (setEnabled) animation.gameObject.SetActive(false);
    }
    
    private int lastLuckyRollTurn = -1;
    public void LuckyRoll()
    {
        if (lastLuckyRollTurn == TurnManager.Instance.currentState.turnNumber) return;
        lastLuckyRollTurn = TurnManager.Instance.currentState.turnNumber;

        pendingAnimations.Add(WaitForAnimation(luckyRollAnimation, luckyRollAnimationClip, true));
    }

    public void CreatureHeal(CreatureController attackingCreature)
    {
        pendingAnimations.Add(WaitForAnimation((attackingCreature.state.team == 0 ? playerCreatureAnimation : opponentCreatureAnimation), Hop, false, "GMTK_VGC SFX_BUFF3DELAYEDREGEN"));
    }

    // change the sprite shown for "team" to the sprite corresponding to "switchTo"
    private CreatureController currentActiveForPlayer = null;
    private CreatureController currentActiveForOpponent = null;
    public void SwapActiveCreature(int team, CreatureController switchTo, bool isInit=false) 
    { 
        if (team == 0 ? (currentActiveForPlayer == switchTo) : (currentActiveForOpponent == switchTo)) return;

        if (team == 0) currentActiveForPlayer = switchTo;
        if (team == 1) currentActiveForOpponent = switchTo;

        pendingAnimations.Add(SwapActiveCreatureAnimation(team, switchTo, isInit)); 
    }

    IEnumerator SwapActiveCreatureAnimation(int team, CreatureController switchTo, bool isInit=false)
    {
        if (team == 0)
        {
            foreach(var moveButton in moveButtons) moveButton.HandleSwitchIn(switchTo);
            foreach(var swapButton in switchButtons) swapButton.HandleSwitchIn(switchTo);
        }

        // TODO: yield return wait for swap animation to complete
        // TODO: switch the sprite of team's creature
        
        Animation a = team == 0 ? playerCreatureAnimation : opponentCreatureAnimation;
        AnimationClip slideIn = team == 0 ? SlideInLeft : SlideInRight;
        AnimationClip slideOut = team == 0 ? SlideOutLeft : SlideOutRight;
        
        if (!isInit)
        {
            a.clip = slideOut;
            a.Play();
            yield return new WaitForSeconds(slideOut.length);
        }

        creatureP1.sprite = TurnManager.Instance.GetActiveCreature(0).state.definition.sprite;
        creatureP2.sprite = TurnManager.Instance.GetActiveCreature(1).state.definition.sprite;
        creatureP1.SetNativeSize();
        creatureP2.SetNativeSize();

        (team == 0 ? player1CreatureUI : player2CreatureUI).UpdateName();
        (team == 0 ? player1CreatureUI : player2CreatureUI).UpdateTargetHPInstantly();
        (team == 0 ? player1CreatureUI : player2CreatureUI).UpdateStatus();
    
        a.clip = slideIn;
        a.Play();
        yield return new WaitForSeconds(slideIn.length);

        yield break;
    }

    IEnumerator PrintAndWait(string print) 
    {
        Debug.LogError(print);
        yield return new WaitForSeconds(2);
    }

    // update the HP bar, play special effect, etc. No delay between calls
    public void PlayDamageEffect(CreatureController beingDamaged)
    {
        pendingAnimations.Add(AttackAnim(beingDamaged));   
    }
    IEnumerator AttackAnim(CreatureController beingDamaged)
    {
        yield return WaitForAnimation(
            (beingDamaged.state.team == 0 ? opponentCreatureAnimation : playerCreatureAnimation),
            Hop
        );

        SFXManager.Instance.Play("GMTK_VGC SFX_DEBUFF7SCARYLOOK");
        var uiImageAnimator = (beingDamaged.state.team == 0 ? playerCreatureEffectsAnimation : opponentCreatureEffectsAnimation);
        uiImageAnimator.sprites = damageClip;
        uiImageAnimator.Play();
        yield return new WaitForSeconds(uiImageAnimator.duration);

        (beingDamaged.state.team == 0 ? player1CreatureUI : player2CreatureUI).UpdateTargetHP();
    }

    public void PlayMissedEffect(CreatureController attacker)
    {
        pendingAnimations.Add(WaitForAnimation(
            (attacker.state.team == 0 ? playerCreatureAnimation : opponentCreatureAnimation),
            Hop
        ));
    }
    public void PlayNoDamageEffect(CreatureController target)
    {
        pendingAnimations.Add(WaitForAnimation(
            (target.state.team == 0 ? opponentCreatureAnimation : playerCreatureAnimation),
            Hop
        ));
    }
     
    public void PlayStatBuffEffect(CreatureController beingBuffed, string statBeingBuffed, int buffLevel)
    {
        // TODO: make some coroutine that plays an animation for this effect and exits when the animation is over, and add it to pendingAnimations
        pendingAnimations.Add(PrintAndWait("stat buff"));

        if (buffLevel == 0) return;

        Animation animation = beingBuffed.state.team == 0 ? playerCreatureAnimation : opponentCreatureAnimation;
        switch(statBeingBuffed)
        {
            case "ATTACK":  pendingAnimations.Add(WaitForAnimation(animation, buffLevel < 0 ? AttackDecreaseClip : AttackIncreaseClip  , false, buffLevel > 0 ? "GMTK_VGC GMTK_VGC SFX_BUFF4TOUGHENUP" : "GMTK_VGC SFX_DEBUFF0")); break;
            case "DEFENSE": pendingAnimations.Add(WaitForAnimation(animation, buffLevel < 0 ? DefenseDecreaseClip : DefenseIncreaseClip, false, buffLevel > 0 ? "GMTK_VGC SFX_BUFF2THICKSKIN"          : "GMTK_VGC SFX_DEBUFF5SPORESOFWEAKNESS")); break;
            case "SPEED":   pendingAnimations.Add(WaitForAnimation(animation, buffLevel < 0 ? SpeedDecreaseClip : SpeedIncreaseClip    , false, buffLevel > 0 ? "GMTK_VGC SFX_BUFF1"                   : "GMTK_VGC SFX_DEBUFF8SLOWBAITPOSE")); break;
        }
    }

    public void PlayStatusEffectGainEffect(CreatureController creatureRecievingStatus, CreatureController.StatusContidion condition)
    {
        pendingAnimations.Add(StatusEffectGainEffect(creatureRecievingStatus, condition));
    }

    IEnumerator StatusEffectGainEffect(CreatureController creatureRecievingStatus, CreatureController.StatusContidion condition)
    {
        Debug.LogError("Playing status anim");
        UISpritesAnimation a = (creatureRecievingStatus.state.team == 0 ? playerCreatureEffectsAnimation : opponentCreatureEffectsAnimation);
        Sprite[] p = null;
        switch (condition)
        {
            case CreatureController.StatusContidion.BURN:      p = burnClip;      SFXManager.Instance.Play("GMTK_VGC SFX_DEBUFF0");          break;
            case CreatureController.StatusContidion.SLEEP:     p = sleepClip;     SFXManager.Instance.Play("GMTK_VGC SFX_DEBUFF4ASLEEP");    break;
            case CreatureController.StatusContidion.POISONED:  p = poisonClip;    SFXManager.Instance.Play("GMTK_VGC SFX_DEBUFF3POISONED");  break;
            case CreatureController.StatusContidion.PARALYZED: p = paralysisClip; SFXManager.Instance.Play("GMTK_VGC SFX_DEBUFF1PARALYZED"); break;
        }

        if (p == null) yield break;

        a.sprites = p;
        a.Play();
        yield return new WaitForSeconds(a.duration);
        (creatureRecievingStatus.state.team == 0 ? player1CreatureUI : player2CreatureUI).UpdateStatus();
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
            if (player == ITurnManager.PLAYER_1) forceSwitchPendingP1 = true;
            if (player == ITurnManager.PLAYER_2) forceSwitchPendingP2 = true;
        }
    }

    // called when a turn is over and the turn manager is ready to recieve input. Also called before the first turn, once both players have been initialized
    public void TurnManagerReadyToRecieveInput()
    {
        Debug.LogWarning("Turn manager is ready");
        // the turn manager has completed calculations and it's ready to recieve player input

        if (TurnManager.Instance.currentState.turnNumber == 0)
        {
            // initialize
            creatureP1.sprite = TurnManager.Instance.GetActiveCreature(0).state.definition.sprite;
            creatureP2.sprite = TurnManager.Instance.GetActiveCreature(1).state.definition.sprite;
            creatureP1.SetNativeSize();
            creatureP2.SetNativeSize();
        
            player2CreatureUI.UpdateName();
            player2CreatureUI.UpdateTargetHPInstantly();
            player2CreatureUI.UpdateStatus();
            player1CreatureUI.UpdateName();
            player1CreatureUI.UpdateTargetHPInstantly();
            player1CreatureUI.UpdateStatus();
        }

    
        mainMenuButtons.ForEach(b => b.interactable = true);

        for(int q = 0; q < 3; q++)
        {
            switchButtons[q].UpdateMe(TurnManager.Instance.players[0].team[q]);
        }

        creatureP1.sprite = TurnManager.Instance.GetActiveCreature(0).state.definition.sprite;
        creatureP2.sprite = TurnManager.Instance.GetActiveCreature(1).state.definition.sprite;
        creatureP1.SetNativeSize();
        creatureP2.SetNativeSize();

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
        pendingAnimations.Add(ShowEndScreen(winningPlayer));
    }

    IEnumerator ShowEndScreen(int winningPlayer)
    {
        yield return new WaitForSeconds(1);
    
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

        switchMenuCloseButton.interactable = !forceSwitchPendingP1; // this button should be disabled if forceSwitchPendingP1 is true
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

    private IEnumerator currentTurn;

    public void SetCurrentTurn(IEnumerator cTurn)
    {
        currentTurn = cTurn;
        mainMenuButtons.ForEach(b => b.interactable = false);
    }

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
                forceSwitchPendingP1 = false;
            } 
            else if (forceSwitchPendingP2)
            {
                DisplaySwapMenu();
                forceSwitchPendingP2 = false;
            } 
            else if (currentTurn != null)
            {
                var nextActionExists = currentTurn.MoveNext();
                if (!nextActionExists) currentTurn = null;
            }
        }
    }

    public void UpdateLuckBar(float luckBalance)
    {
        luckMeter.targetLuck = luckBalance;
    }
}
