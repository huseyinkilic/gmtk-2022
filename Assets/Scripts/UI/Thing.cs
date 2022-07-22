using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Thing : MonoBehaviour
{
    public Image hp;
    public Text name;

    public GameObject slp;
    public GameObject par;
    public GameObject poi;
    public GameObject bur;

    public static float hpBarSpeed = 0.005f;
    private float targetHP = 1;
    public int player;

    //public void Start()
    //{
    //    UpdateName();
    //    UpdateStatus();
    //    UpdateTargetHPInstantly();
    //}

    public void Update()
    {
        float delta = targetHP - hp.fillAmount;
        hp.fillAmount += Mathf.Sign(delta)*Mathf.Min(hpBarSpeed, Mathf.Abs(delta));   
    }

    public bool HPBarAnimationIsDone()
    {
        return Mathf.Abs(targetHP - hp.fillAmount) <= Mathf.Epsilon;
    }

    public void UpdateName()
    {
        var creature = TurnManager.Instance.GetActiveCreature(player);
        name.text = creature.state.definition.name;
    }

    public void UpdateStatus()
    {
        var creature = TurnManager.Instance.GetActiveCreature(player);
        slp.SetActive(creature.state.status == CreatureController.StatusContidion.SLEEP);
        par.SetActive(creature.state.status == CreatureController.StatusContidion.PARALYZED);
        poi.SetActive(creature.state.status == CreatureController.StatusContidion.POISONED);
        bur.SetActive(creature.state.status == CreatureController.StatusContidion.BURN);
    }

    public void UpdateTargetHP()
    {
        var creature = TurnManager.Instance.GetActiveCreature(player);
        var targetFillAmount = 1f - ((float)creature.state.currentDamage) / ((float)creature.state.definition.hp);
        targetHP = targetFillAmount;
    }

    public void UpdateTargetHPInstantly()
    {
        UpdateTargetHP();
        hp.fillAmount = targetHP;
    }
}
