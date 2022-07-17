using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionLogUI : MonoBehaviour
{
    public static ActionLogUI Instance;

    public GameObject textPrefab;

    private float currentY = -30;

    private Transform parentTo;

    // Start is called before the first frame update
    void Awake()
    {
        parentTo = this.transform.GetChild(0);   
        Instance = this;
    }

    public void AddLog(string msg)
    {
        GameObject logText = Instantiate(textPrefab);
        logText.GetComponent<Text>().text = msg;
        logText.transform.parent = parentTo;
        var deltaY = -30 - 155.8726f; // 155.8726 is the height of the log text
        logText.transform.localPosition = new Vector3(30, currentY+deltaY);
        currentY = currentY+deltaY;
    }
}
