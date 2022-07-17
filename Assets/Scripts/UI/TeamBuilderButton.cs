using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamBuilderButton : MonoBehaviour
{
    public static int count = 0;
    public static readonly float MAX_STAT = 130;

    public Sprite MoveButton_AttackType;
    public Sprite MoveButton_DefenseType;
    public Sprite MoveButton_NeutralType;


    public Image moveBackground1;
    public Image moveBackground2;
    public Image moveBackground3;
    public Image moveBackground4;

    public Text moveName1;
    public Text moveName2;
    public Text moveName3;
    public Text moveName4;

    public Image creaturePic;

    public Slider hp;
    public Slider attk;
    public Slider def;
    public Slider spd;

    public Text nameText;


    private Creature creature;

    // Start is called before the first frame update
    void Start()
    {
        creature = Database.Instance.GetAllCreatures()[count++];
        SetStuff();
    }

    void SetStuff()
    {
        moveBackground1.sprite = GetSpriteForButton(Database.Instance.MoveFromID(creature.allowedMoves[0]).type); 
        moveBackground2.sprite = GetSpriteForButton(Database.Instance.MoveFromID(creature.allowedMoves[1]).type); 
        moveBackground3.sprite = GetSpriteForButton(Database.Instance.MoveFromID(creature.allowedMoves[2]).type); 
        moveBackground4.sprite = GetSpriteForButton(Database.Instance.MoveFromID(creature.allowedMoves[3]).type); 

        moveName1.text = (Database.Instance.MoveFromID(creature.allowedMoves[0]).name); 
        moveName2.text = (Database.Instance.MoveFromID(creature.allowedMoves[1]).name); 
        moveName3.text = (Database.Instance.MoveFromID(creature.allowedMoves[2]).name); 
        moveName4.text = (Database.Instance.MoveFromID(creature.allowedMoves[3]).name); 

        // TODO: creature pic

        hp.value = (float)creature.hp / MAX_STAT;
        attk.value = (float)creature.attack / MAX_STAT;
        def.value = (float)creature.defense / MAX_STAT;
        spd.value = (float)creature.speed / MAX_STAT;

        nameText.text = creature.name;
    }

    Sprite GetSpriteForButton(Move.Type type)
    {
        switch(type){
            case Move.Type.ATTACK: return  MoveButton_AttackType; break;
            case Move.Type.DEFEND: return  MoveButton_DefenseType; break;
            case Move.Type.NEUTRAL: return MoveButton_NeutralType; break;
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
