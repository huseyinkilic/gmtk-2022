using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public List<AudioClip> audioClips = new List<AudioClip>();

    private AudioSource cameraAudioSource;

    public void Play(string name, bool isLooped)
    {
        AudioClip clip = audioClips.Find(p => p.name == name);

        if (clip)
        {
            cameraAudioSource.clip = clip;
            cameraAudioSource.loop = isLooped;
            cameraAudioSource.Play();
        }
    }

    private void Awake()
    {
        Instance = this;
        cameraAudioSource = Camera.main.GetComponent<AudioSource>();
    }
}
