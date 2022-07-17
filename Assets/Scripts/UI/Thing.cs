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


    public int player;

    public void Update()
    {
        var creature = TurnManager.Instance.GetActiveCreature(player);
        hp.fillAmount = 1f - ((float)creature.state.currentDamage) / ((float)creature.state.definition.hp);
        name.text = creature.state.definition.name;
        
        slp.SetActive(creature.state.status == CreatureController.StatusContidion.SLEEP);
        par.SetActive(creature.state.status == CreatureController.StatusContidion.PARALYZED);
        poi.SetActive(creature.state.status == CreatureController.StatusContidion.POISONED);
        bur.SetActive(creature.state.status == CreatureController.StatusContidion.BURN);
    }
}
