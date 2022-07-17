using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UseMoveButton : MonoBehaviour
{
    public int team;
    public int moveIndex;
    
    private BattleUI ui;
    private Button button;
    public Text nameText;

    public void Start()
    {
        ui = Resources.FindObjectsOfTypeAll<BattleUI>()[0];
        button = GetComponent<Button>();
    }

    public void MakeAndSubmitAction()
    {
        int opposingTeam = 1-team;
        var action = TurnManager.Instance.MakeMoveAction(team, moveIndex, TurnManager.Instance.GetActiveCreature(opposingTeam));
        TurnManager.Instance.SubmitAction(action);

        // return to the menu "Menu"
        ui.DisplayMainMenu();
    }

    public void HandleSwitchIn(CreatureController creature)
    {
        Sprite s = null;

        switch(creature.state.knownMoves[moveIndex].type){
            case Move.Type.ATTACK: s = ui.MoveButton_AttackType; break;
            case Move.Type.DEFEND: s = ui.MoveButton_DefenseType; break;
            case Move.Type.NEUTRAL: s = ui.MoveButton_NeutralType; break;
        }

        button.GetComponent<Image>().sprite = s;

        // TODO: and now set the text
        nameText.text = creature.state.knownMoves[moveIndex].name;
        // descriptionText.text = ...
        // accuracyText
        // powerText
    }
}
