using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    [Header("레벨 종료 UI")]
    [SerializeField] private GameObject GameOverUI;
    [SerializeField] private GameObject GameClearUI;

    [Header("레벨 종료 UI 버튼들")]
    [SerializeField] private Button s_levelSelectButton;
    [SerializeField] private Button f_levelSelectButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;
    
    [Header("유틸리티")]
     public Text timer;
     public Text livesCount;
    
    private void Start()
    {
        GameManager.instance.levelUI = this;
        s_levelSelectButton.onClick.AddListener(GameManager.instance.ExitLevel);
        f_levelSelectButton.onClick.AddListener(GameManager.instance.ExitLevel);
        retryButton.onClick.AddListener(GameManager.instance.RetryLevel);
        continueButton.onClick.AddListener(GameManager.instance.NextLevel);
        InitLevelUI();
    }
    
    public void EnableGameOverUI()
    {
        GameOverUI.SetActive(true);
    }
    
    public void EnableClearUI()
    {
        GameClearUI.SetActive(true);
    }

    public void InitLevelUI()
    {
        timer.text = $"시간 : {Mathf.RoundToInt(GameManager.instance.timeForLive)}";
        livesCount.text = $"목숨 : {GameManager.instance.currentLives.ToString()}";
    }
}
