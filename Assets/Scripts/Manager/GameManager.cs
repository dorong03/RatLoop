using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Lobby, Preview, Playing, Die, GameOver, Clear
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private LevelData[] levelData;
    
    public static GameManager instance;
    
    public GameState currentState;

    public LevelUI levelUI;

    public int clearLevelIndex { get; private set; }
    
    private int currentStageLevel;
    public int currentLives { get; private set; }
    public float timeForLive { get; private set; }
    private float stageTimer;

    private void Awake()
    {
        PlayerPrefs.SetInt("ClearLevelIndex", 1);
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
            levelUI.timer.text = $"{Mathf.RoundToInt(stageTimer)}";
            if (stageTimer <= 0)
            {
                currentLives = 0;
                EndGame();
            }
        }
    }

    public void PlayerDie()
    {
        if (currentLives > 0)
        {
            currentLives--;
            levelUI.livesCount.text = $"X {currentLives}";
            ChangeState(GameState.Die);
            RecordingManager.instance.StopPlayingRecord();
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
            return;
        }
        currentStageLevel = data.levelIndex;
        currentLives = data.maxLives;
        timeForLive = data.timeLimit;
        stageTimer = timeForLive;
        ChangeState(GameState.Preview);
        
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayStageBGM();
        }
    }

    public void LevelClear()
    {
        ChangeState(GameState.Clear);

        if (Camera.main != null)
        {
            CameraEffect camEffect = Camera.main.GetComponent<CameraEffect>();
            if (camEffect != null)
            {
                camEffect.SetLookDown(false);
            }
        }

        if (currentStageLevel >= clearLevelIndex)
        {
            clearLevelIndex = currentStageLevel + 1;
            PlayerPrefs.SetInt("ClearLevelIndex", clearLevelIndex);
            
        }
        Invoke("NextLevel", 3f);
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

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayLobbyBGM();
        }
    }

    public void EndGame()
    {
        ChangeState(GameState.GameOver);
        Invoke("RetryLevel", 2f);
    }
    
    private void ChangeState(GameState newState)
    {
        currentState = newState;
    }
    
    public void OpenLevel(int level)
    {
        string sceneName = "Level_" + level.ToString();

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            EnterLevel(level);
        }
    }
}