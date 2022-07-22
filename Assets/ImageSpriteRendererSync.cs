using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(SpriteRenderer))]
public class ImageSpriteRendererSync : MonoBehaviour
{
    private Image image;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        image = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        image.sprite = spriteRenderer.sprite;
        image.enabled = spriteRenderer.sprite != null;
    }
}
