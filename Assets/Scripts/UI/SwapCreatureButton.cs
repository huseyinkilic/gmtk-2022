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
    public Button button;

    public Image hp;
    public Text name;
    public Image sprite;


    public void Awake()
    {
        ui = Resources.FindObjectsOfTypeAll<BattleUI>()[0];
        //button = GetComponent<Button>();
    }

    public void MakeAndSubmitAction()
    {
        
        var action = TurnManager.Instance.MakeSwitchAction(team, creatureIndex);
        TurnManager.Instance.SubmitAction(action);
    
        // return to the actions UI
        ui.DisplayMainMenu();
    }

    public void UpdateMe(CreatureController creature)
    {
        if (!creature.CanStillFight()) button.interactable = false;
        if (creature.state == null || creature.state.definition == null) return;

        hp.fillAmount = 1f - ((float)creature.state.currentDamage) / ((float)creature.state.definition.hp);
        sprite.sprite = creature.state.definition.sprite;
        name.text = creature.state.definition.name;
    }

    public void HandleSwitchIn(CreatureController switchTo)
    {
        button.interactable = true;
        if (TurnManager.Instance.GetPlayerCreatures(0).Count <= creatureIndex) 
        {
            
            button.interactable = false;
            return;
        }   

        Debug.LogWarning($"SWITCHING TO {switchTo.state.definition.name}");
        
        if (switchTo.state.indexOnTeam == creatureIndex) button.interactable = false;
        else button.interactable = TurnManager.Instance.GetPlayerCreatures(0)[creatureIndex].CanStillFight();
    }
}
