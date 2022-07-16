using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapCreatureButton : MonoBehaviour
{
    public int team;
    public int creatureIndex;
    public void MakeAndSubmitAction()
    {
        var action = TurnManager.Instance.MakeSwitchAction(team, creatureIndex);
        TurnManager.Instance.SubmitAction(action);
    
        // TODO: return to the menu "Menu"
    }
}
