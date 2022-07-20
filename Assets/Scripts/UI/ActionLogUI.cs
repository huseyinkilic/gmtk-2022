using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionLogUI : MonoBehaviour
{
    public static ActionLogUI Instance;

    public GameObject textPrefab;
    public Transform parentTo;

    private float currentY = -30;

    private ScrollRect scrollRect;

    // Start is called before the first frame update
    void Awake()
    {
        //parentTo = this.transform.GetChild(0);   
        Instance = this;
        scrollRect = GetComponent<ScrollRect>();
    }

    public void AddLog(string msg)
    {
        bool isAtBottom = scrollRect.normalizedPosition.y <= 0.00001; // basically "is equal to 0"
        
        GameObject logText = Instantiate(textPrefab);
        logText.GetComponent<Text>().text = msg;
        logText.transform.parent = parentTo;
        var deltaY = -30 - 155.8726f; // 155.8726 is the height of the log text
        logText.transform.localPosition = new Vector3(30, currentY+deltaY);
        currentY = currentY+deltaY;

        if (isAtBottom)
        {
            // https://stackoverflow.com/a/47613689/9643841
            Canvas.ForceUpdateCanvases();

            parentTo.GetComponent<VerticalLayoutGroup>().CalculateLayoutInputVertical() ;
            parentTo.GetComponent<ContentSizeFitter>().SetLayoutVertical() ;

            scrollRect.content.GetComponent<VerticalLayoutGroup>().CalculateLayoutInputVertical() ;
            scrollRect.content.GetComponent<ContentSizeFitter>().SetLayoutVertical() ;

            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}
