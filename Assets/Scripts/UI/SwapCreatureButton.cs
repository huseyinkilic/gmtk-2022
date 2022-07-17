using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapCreatureButton : MonoBehaviour
{
    public int team;
    public int creatureIndex;

    private BattleUI ui;
    public void Start()
    {
        ui = Resources.FindObjectsOfTypeAll<BattleUI>()[0];
    }

    public void MakeAndSubmitAction()
    {
        var action = TurnManager.Instance.MakeSwitchAction(team, creatureIndex);
        TurnManager.Instance.SubmitAction(action);
    
        // return to the menu "Menu"
        ui.OnBackClick();
    }
}
