using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<int> OnLevelChange;

    public enum State { Init, Start, Play, Loss, Win };
    public State state;

    public int currentLevel;
    public GameObject currentLevelGameObject;

    public GameObject canvasHUD;
    public GameObject canvasStart;
    public GameObject canvasLoss;
    public GameObject canvasWin;
    public GameObject canvasCreatures;
    public GameObject canvasBattle;

    public GameObject[] levels;

    private GameObject lastCanvasObject;

    void Awake()
    {
        state = State.Init;

        Instantiate(canvasHUD);

        //currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        currentLevel = 0;
        LoadLevel(currentLevel);
        SetState(State.Start);
    }

    public void SetState(State newState)
    {
        if (state != newState)
        {
            state = newState;

            switch (state)
            {
                case State.Start:
                    DisplayScreen(canvasStart);
                    break;
                case State.Play:
                    if (lastCanvasObject)
                    {
                        Destroy(lastCanvasObject);
                    }
                    break;
                case State.Loss:
                    DisplayScreen(canvasLoss);
                    break;
                case State.Win:
                    DisplayScreen(canvasWin);
                    break;
            }
        }
    }

    public void RetryLevel()
    {
        LoadLevel(currentLevel);
        SetState(State.Start);
    }

    public void NextLevel()
    {
        currentLevel = (currentLevel + 1) % levels.Length;

        //PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        //PlayerPrefs.Save();

        LoadLevel(currentLevel);
        SetState(State.Start);
    }

    private void LoadLevel(int level)
    {
        if (currentLevelGameObject)
        {
            Destroy(currentLevelGameObject);
        }

        currentLevel = level;
        currentLevelGameObject = Instantiate(levels[currentLevel], levels[currentLevel].transform.position, levels[currentLevel].transform.rotation);

        OnLevelChange?.Invoke(currentLevel);
    }

    private void DisplayScreen(GameObject canvasPrefab)
    {
        if (lastCanvasObject)
        {
            Destroy(lastCanvasObject);
        }

        GameObject spawned = Instantiate(canvasPrefab);
        if (spawned)
        {
            lastCanvasObject = spawned;
        }
    }
}
