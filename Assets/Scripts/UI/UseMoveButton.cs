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

    public Text accText;
    public Text powerText;

    public Text descriptionText;

    public Image i;

    public void Awake()
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

        if (ui == null) ui = BattleUI.Instance;

        switch(creature.state.knownMoves[moveIndex].type){
            case Move.Type.ATTACK: s = ui.MoveButton_AttackType; break;
            case Move.Type.DEFEND: s = ui.MoveButton_DefenseType; break;
            case Move.Type.NEUTRAL: s = ui.MoveButton_NeutralType; break;
        }

        i.sprite = s;

        nameText.text = creature.state.knownMoves[moveIndex].name;
        accText.text = creature.state.knownMoves[moveIndex].accuracy + "";
        powerText.text = creature.state.knownMoves[moveIndex].basePower + "";
        descriptionText.text = creature.state.knownMoves[moveIndex].description;
    }
}
