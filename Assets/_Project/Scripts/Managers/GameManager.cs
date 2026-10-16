using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public enum GameState { Lobby, Preview, Playing, Pause, Die, Clear }

// 다른 매니저보다 먼저 Awake 되어야 LevelLoader 가 instance 를 쓸 수 있다.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public const string GameSceneName = "Game";
    public const string LevelSelectSceneName = "LevelSelection";
    // 기존 저장 데이터와 호환되도록 키 이름은 그대로 둔다. 값은 "열린 스테이지 번호(1부터)".
    private const string UnlockedStageKey = "MaxClearLevelID";

    public static GameManager instance;

    public GameState gameState;

    [Tooltip("게임에 나오는 레벨 순서")]
    [SerializeField] private LevelList levelList;

    private int unlockedStage = 1;

    // 스테이지 내부 데이터들
    private LevelRoot currentLevel;
    private int currentLevelIndex = -1;
    private int currentTimer;
    private int currentReplayCount;

    private int lastEnteredIndex = -1;

    [FormerlySerializedAs("isCamereEffectShowed")]
    [SerializeField] private bool isPreviewShown = false;

    private float timer;
    private Coroutine clearRoutine;

    // 시간 UI 업데이트 용
    public event Action<int> OnTimeChanged;
    // UI 업데이트 용
    public event Action<int> OnReplayCountChanged;
    // 들어갈때 초기 셋팅용
    public event Action OnEnterLevel;
    // 나갈때 셋팅용
    public event Action OnExitLevel;
    public event Action<bool> OnPressPause;
    // 스테이지 클리어 시 UI 알림용 (다음 스테이지 / 다시 하기 / 스테이지 선택)
    public event Action OnLevelClear;
    // 마지막 스테이지 클리어 시 UI 알림용
    public event Action OnFinalLevelClear;

    public LevelList Levels => levelList;
    // 플레이할 수 있는 가장 높은 스테이지 번호 (1부터)
    public int UnlockedStage => unlockedStage;
    public LevelRoot CurrentLevel => currentLevel;
    public int CurrentLevelIndex => currentLevelIndex;
    // 데이터 기록용 스테이지 번호. 목록에 없는 테스트 레벨은 0.
    public int CurrentStageNumber => currentLevelIndex + 1;
    public int LastEnteredIndex => lastEnteredIndex;
    public bool IsLastLevel => currentLevelIndex >= 0 && currentLevelIndex == levelList.Count - 1;
    public bool HasNextLevel => currentLevelIndex >= 0 && currentLevelIndex + 1 < levelList.Count;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            unlockedStage = Mathf.Max(1, PlayerPrefs.GetInt(UnlockedStageKey, 1));
            GameSettings.ApplyAll();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        UpdateLevelTimer();

        if (GameInput.PausePressed)
        {
            TogglePause();
        }
    }

    public bool IsUnlocked(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex + 1 <= unlockedStage;
    }

    public void EnterNextLevel()
    {
        if (HasNextLevel)
        {
            EnterLevel(currentLevelIndex + 1);
        }
        else
        {
            ExitLevel();
        }
    }

    // 설정 메뉴의 "진행 초기화"
    public void ResetProgress()
    {
        unlockedStage = 1;
        lastEnteredIndex = -1;
        PlayerPrefs.SetInt(UnlockedStageKey, unlockedStage);
        PlayerPrefs.Save();
    }

    public void EnterLevel(int levelIndex)
    {
        LevelRoot level = levelList.Get(levelIndex);
        if (level == null)
        {
            Debug.LogError($"{levelIndex + 1}번 레벨이 레벨 목록에 없습니다.");
            return;
        }
        EnterLevel(level);
    }

    // 목록에 없는 레벨(테스트용)도 들어갈 수 있다. 이 경우 클리어해도 다음 스테이지가 열리지 않는다.
    public void EnterLevel(LevelRoot level)
    {
        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
            clearRoutine = null;
        }

        currentLevel = level;
        currentLevelIndex = levelList != null ? levelList.IndexOf(level) : -1;
        if (currentLevelIndex >= 0)
        {
            lastEnteredIndex = currentLevelIndex;
        }

        SceneManager.sceneLoaded += OnLevelLoaded;
        SceneManager.LoadScene(GameSceneName);

        Time.timeScale = 1f;
        gameState = GameState.Preview;

        currentTimer = level.TimeLimit;
        currentReplayCount = level.MaxReplayCount;
        timer = 0;
    }

    public void OnLevelLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLevelLoaded;

        SoundManager.instance.PlayStageBGM();

        OnTimeChanged?.Invoke(currentTimer);
        OnReplayCountChanged?.Invoke(currentReplayCount);
        OnEnterLevel?.Invoke();

        CameraEffect cameraEffect = Camera.main.GetComponent<CameraEffect>();

        if (cameraEffect != null && !isPreviewShown)
        {
            cameraEffect.PlayPreViewSequence(OnPreviewFinished);
            isPreviewShown = true;
        }
        else
        {
            OnPreviewFinished();
        }
    }

    private void OnPreviewFinished()
    {
        gameState = GameState.Playing;
    }

    public void ExitLevel()
    {
        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
            clearRoutine = null;
        }

        gameState = GameState.Lobby;

        Time.timeScale = 1;
        currentLevel = null;
        currentLevelIndex = -1;
        currentTimer = 0;
        currentReplayCount = 0;
        timer = 0;
        isPreviewShown = false;

        SceneManager.LoadScene(LevelSelectSceneName);
        SoundManager.instance.PlayLobbyBGM();
        OnExitLevel?.Invoke();
    }

    public void ClearLevel()
    {
        if (gameState == GameState.Clear)
        {
            return;
        }
        gameState = GameState.Clear;

        // 목록 순서로 다음 스테이지를 연다.
        bool hasNext = currentLevelIndex >= 0 && currentLevelIndex + 1 < levelList.Count;
        if (hasNext && currentLevelIndex + 2 > unlockedStage)
        {
            unlockedStage = currentLevelIndex + 2;
            PlayerPrefs.SetInt(UnlockedStageKey, unlockedStage);
        }

        SoundManager.instance.PlaySFX(SfxType.Cheese);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent(out Animator anim))
        {
            anim.SetTrigger("Eat");
        }
        DataCollector.instance.RecordClear();

        clearRoutine = StartCoroutine(ClearRoutine());
    }

    private IEnumerator ClearRoutine()
    {
        yield return new WaitForSecondsRealtime(2f);
        clearRoutine = null;

        if (IsLastLevel)
        {
            OnFinalLevelClear?.Invoke();
        }
        else if (OnLevelClear != null)
        {
            OnLevelClear.Invoke();
        }
        else
        {
            ExitLevel();
        }
    }

    public void PlayerDie()
    {
        gameState = GameState.Die;

        // 기록을 먼저 남긴 뒤 현재 레벨로 다시 들어간다.
        DataCollector.instance.RecordDeath();
        EnterLevel(currentLevel);
    }

    public void Retry()
    {
        // 기록을 먼저 남겨야 다음 시도가 시작되기 전에 이번 시도가 저장된다.
        DataCollector.instance.RecordRetry();
        EnterLevel(currentLevel);
    }

    public void Replay()
    {
        if (gameState == GameState.Playing)
        {
            if(currentReplayCount > 0)
            {
                currentReplayCount--;
                OnReplayCountChanged?.Invoke(currentReplayCount);
                SpawnManager.instance.StopRecordingAndReStart();
                DataCollector.instance.RecordReplay();
            }
        }
    }

    public void TogglePause()
    {
        if (gameState == GameState.Preview)
        {
            CameraEffect cameraEffect = Camera.main.GetComponent<CameraEffect>();
            cameraEffect.StopPreviewSequence(OnPreviewFinished);
        }
        else if (gameState == GameState.Playing)
        {
            OnPressPause?.Invoke(true);
        }
        else if (gameState == GameState.Pause)
        {
            OnPressPause?.Invoke(false);
        }
    }

    private void UpdateLevelTimer()
    {
        if (gameState == GameState.Playing)
        {
            timer += Time.deltaTime;
            if (timer >= 1)
            {
                currentTimer--;
                timer -= 1f;
                OnTimeChanged?.Invoke(currentTimer);
            }

            if (currentTimer <= 0)
            {
                PlayerDie();
            }
        }
    }
}
