
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UISpritesAnimation : MonoBehaviour
{
    public float duration;
 
    public Sprite[] sprites;
     
    private Image image;
    private int index = 0;
    private float timer = 0;

    public bool loop = false;
    public bool play = false;
    public bool disableOnEnd = true;
    private bool nextFrameAnimationEnds = false;
 
    void Start()
    {
        image = GetComponent<Image>();
    }

    public void Play()
    {
        play = true;
        index = 0;
        timer = 0;
    }

    private void Update()
    {
        if (sprites == null || sprites.Length <= 0) return;
        if (!play) return;

        if((timer+=Time.deltaTime) >= (duration / sprites.Length))
        {
            // end animation (if it's ready to end)
            if (nextFrameAnimationEnds) 
            { 
                nextFrameAnimationEnds = false;
                play = false; 
                if (disableOnEnd)
                {
                    image.enabled = false; 
                }
                return; 
            }

            image.enabled = true;

            // advance to next frame
            timer = 0;
            image.sprite = sprites[index];
            index = (index + 1) % sprites.Length;
            
            // check for end of animation
            if (!loop && index == 0) { nextFrameAnimationEnds = true; }
        }
    }
}