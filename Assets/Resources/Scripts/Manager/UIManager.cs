using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    [SerializeField] private Text livesText;
    [SerializeField] private Text timeText;
    
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ReTryButton;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
            
        GameManager.instance.OnReplayCountChanged += UpdateLivesText;
        GameManager.instance.OnTimeChanged += UpdateTimeText;
        GameManager.instance.OnEnterLevel += ShowLevelUI;
        GameManager.instance.OnExitLevel += HideLevelUI;
        GameManager.instance.OnPressPause += OnPressPause;
        
        ResumeButton.onClick.AddListener(GameManager.instance.TogglePause);
        ExitButton.onClick.AddListener(GameManager.instance.ExitLevel);
        ReTryButton.onClick.AddListener(GameManager.instance.Retry);
    }

    private void OnDestroy()
    {
        GameManager.instance.OnReplayCountChanged -= UpdateLivesText;
        GameManager.instance.OnTimeChanged -= UpdateTimeText;
        GameManager.instance.OnEnterLevel -= ShowLevelUI;
        GameManager.instance.OnExitLevel -= HideLevelUI;
        GameManager.instance.OnPressPause -= OnPressPause;
    }

    private void UpdateLivesText(int lives)
    {
        livesText.text = "X "+ lives;
    }

    private void UpdateTimeText(int time)
    {
        timeText.text = time.ToString();
    }

    private void OnPressPause(bool pause)
    {
        pausePanel.SetActive(pause);
        EventSystem.current.SetSelectedGameObject(ResumeButton.gameObject);
    }

    private void ShowLevelUI()
    {
        timeText.gameObject.SetActive(true);
        livesText.gameObject.SetActive(true);
        pausePanel.SetActive(false);
    }

    private void HideLevelUI()
    {
        timeText.gameObject.SetActive(false);
        livesText.gameObject.SetActive(false);
        pausePanel.SetActive(false);
    }
}
