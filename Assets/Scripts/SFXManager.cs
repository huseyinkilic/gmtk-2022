using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    public List<AudioClip> audioClips = new List<AudioClip>();

    private AudioSource audioSource;

    public void Play(string name)
    {
        AudioClip clip = audioClips.Find(p => p.name == name);

        if (clip)
        {
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.Play();
        }
    }

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }
}
