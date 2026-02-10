using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    [SerializeField] private Text livesText;
    [SerializeField] private Text timeText;
    [SerializeField] private GameObject pausePanel;

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
        GameManager.instance.OnReplayCountChanged += UpdateLivesText;
        GameManager.instance.OnTimeChanged += UpdateTimeText;
        GameManager.instance.OnEnterLevel += ShowLevelUI;
        GameManager.instance.OnExitLevel += HideLevelUI;
        GameManager.instance.OnPressPause += OnPressPause;
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
