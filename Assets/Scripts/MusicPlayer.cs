using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public string themeName;
    public bool loop;

    // Start is called before the first frame update
    void Start()
    {
        AudioManager.Instance.Play(themeName, loop);   
    }
}
