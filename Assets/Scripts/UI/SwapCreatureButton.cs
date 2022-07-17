using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapCreatureButton : MonoBehaviour
{
    public int team;
    public int creatureIndex;

    private BattleUI ui;
    private Button button;
    public void Start()
    {
        ui = Resources.FindObjectsOfTypeAll<BattleUI>()[0];
        button = GetComponent<Button>();
    }

    public void MakeAndSubmitAction()
    {
        var action = TurnManager.Instance.MakeSwitchAction(team, creatureIndex);
        TurnManager.Instance.SubmitAction(action);
    
        // return to the actions UI
        ui.OnBackClick();
    }

    public void UpdateMe(CreatureController creature)
    {
        if (!creature.CanStillFight()) button.interactable = false;

        // TODO: update the hp bar
    }

    public void HandleSwitchIn(CreatureController switchTo)
    {
        if (switchTo.state.indexOnTeam == creatureIndex) button.interactable = false;
        else button.interactable = TurnManager.Instance.GetPlayerCreatures(0)[creatureIndex].CanStillFight();
    }
}
