using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Lobby,Preview,Playing,Die,Paused,GameOver
}
public class GameManager : MonoBehaviour
{
    [SerializeField] private LevelData[] levelData;
    
    public static GameManager instance;
    
    public GameState currentState;

    public LevelUI levelUI;

    public int clearLevelIndex { get; private set; }
    
    // 스테이지 내부 정보
    private int currentStageLevel;
    public int currentLives { get; private set; }
    public float timeForLive { get; private set; }
    private float stageTimer;

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
        }
        
        clearLevelIndex = PlayerPrefs.GetInt("ClearLevelIndex", 1);
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            stageTimer -= Time.deltaTime;
            levelUI.timer.text = $"타이머 : {Mathf.RoundToInt(stageTimer)}";
            if (stageTimer <= 0)
            {
                currentLives = 0;
                PlayerDie();
            }
        }
    }

    public void PlayerDie()
    {
        currentLives--;
        levelUI.livesCount.text = $"목숨 : {currentLives}";
        if (currentLives > 0)
        {
            ChangeState(GameState.Die);
            RecordingManager.instance.StopPlayingRecord();
            stageTimer = timeForLive;
        }
        else
        {
            EndGame();
            // levelUI.EnableGameOverUI();
        }
    }

    public void PlayerAlive()
    {
        ChangeState(GameState.Playing);
    }
    
    public void EnterLevel(int level)
    {
        LevelData data = levelData[level - 1];
        if (data == null)
        {
            Debug.LogError("레벨 데이터가 없습니다");
            return;
        }
        currentStageLevel = data.levelIndex;
        currentLives = data.maxLives;
        timeForLive = data.timeLimit;
        stageTimer = timeForLive;
        ChangeState(GameState.Preview);
    }

    public void LevelClear()
    {
        if (currentStageLevel > clearLevelIndex)
        {
            clearLevelIndex = currentStageLevel + 1;
            PlayerPrefs.SetInt("ClearLevelIndex", clearLevelIndex);
        }
        levelUI.EnableClearUI();
        ChangeState(GameState.GameOver);
    }

    public void RetryLevel()
    {
        OpenLevel(currentStageLevel);
    }

    public void NextLevel()
    {
        if (levelData.Length + 1 > currentStageLevel)
        {
            OpenLevel(levelData[currentStageLevel].levelIndex);
        }
        else
        {
            ExitLevel();
        }
    }
    
    public void ExitLevel()
    {
        SceneManager.LoadScene("LevelSelction");
        ChangeState(GameState.Lobby);
    }

    private void EndGame()
    {
        ChangeState(GameState.GameOver);
    }
    
    private void ChangeState(GameState newState)
    {
        currentState = newState;
        Time.timeScale = currentState == GameState.Paused ? 0 : 1;
    }
    
    public void OpenLevel(int level)
    {
        string sceneName = "Level_" + level.ToString();

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            EnterLevel(level);
        }
        else
        {
            Debug.Log($"{level} 레벨 씬이 존재하지 않음");
        }
    }
}
