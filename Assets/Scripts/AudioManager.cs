using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip battleTheme;
    public AudioClip lossTheme;
    public AudioClip victoryTheme;

    private Camera mainCamera;
    private AudioSource cameraAudioSource;

    public void PlayBattleTheme()
    {
        cameraAudioSource.clip = battleTheme;
        cameraAudioSource.loop = true;
        cameraAudioSource.Play();
    }

    public void PlayLossTheme()
    {
        cameraAudioSource.clip = lossTheme;
        cameraAudioSource.loop = false;
        cameraAudioSource.Play();
    }

    public void PlayVictoryTheme()
    {
        cameraAudioSource.clip = victoryTheme;
        cameraAudioSource.loop = false;
        cameraAudioSource.Play();
    }

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        cameraAudioSource = mainCamera.GetComponent<AudioSource>();
    }
}
