using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LuckMeter : MonoBehaviour
{
    public float currentLuckDisplayed = 0;
    public Slider slider;

    public float targetLuck = 0;
    public float maxUpdateSpeed = 0.005f;

    public static float Sigmoid(float x)
    {
        return 1 / (1 + Mathf.Exp(-x));
    }

    void Start()
    {
        slider.value = 0.5f;   
    }

    void Update()
    {
        float delta = targetLuck - currentLuckDisplayed;
        currentLuckDisplayed += Mathf.Sign(delta)*Mathf.Min(maxUpdateSpeed, Mathf.Abs(delta)); 
        slider.value = Sigmoid(currentLuckDisplayed);
    }
}
