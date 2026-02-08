using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;

public class LevelUI : MonoBehaviour
{
    [Header("유틸리티")]
    public Text timer;
    public Text livesCount;

    [Header("일시정지 설정")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject resumeButton;

    private bool isPaused = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnPressPauseButton();
        }
    }
    
    private void Start()
    {
        GameManager.instance.levelUI = this;
        InitLevelUI();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void InitLevelUI()
    {
        timer.text = $"{Mathf.RoundToInt(GameManager.instance.timeForLive)}";
        livesCount.text = $"X {GameManager.instance.currentLives.ToString()}";
    }

    private void OnPressPauseButton()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(resumeButton);
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        GameManager.instance.RetryLevel();
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        GameManager.instance.ExitLevel();
    }
}