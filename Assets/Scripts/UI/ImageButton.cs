using UnityEngine;
using UnityEngine.UI;

public class ImageButton : MonoBehaviour
{
    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}
