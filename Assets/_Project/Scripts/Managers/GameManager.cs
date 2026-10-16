using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Lobby, Preview, Playing, Pause, Die, Clear }

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    public GameState gameState;

    private Dictionary<int, LevelData> levelData = new Dictionary<int, LevelData>();
    // 정렬된 데이터 반환하기
    public List<LevelData> SortedLevelData => levelData.Values.OrderBy(x => x.levelID).ToList();
        
    public int maxClearLevelID;
    
    // 스테이지 내부 데이터들
    public LevelData currentLevelData;
    private int currentTimer;
    private int currentReplayCount;

    private int lastEnteredId = 0;
    public int LastEnteredId => lastEnteredId;
    
    [SerializeField] private bool isCamereEffectShowed = false;
    
    private float timer;
    
    // 시간 UI 업데이트 용
    public event Action<int> OnTimeChanged;
    // UI 업데이트 용
    public event Action<int> OnReplayCountChanged;
    // 들어갈때 초기 셋팅용
    public event Action OnEnterLevel;
    // 나갈때 셋팅용
    public event Action OnExitLevel;
    public event Action<bool> OnPressPause;
    // 마지막 스테이지 클리어 시 UI 알림용
    public event Action OnFinalLevelClear;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllLevelData();
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
        maxClearLevelID = PlayerPrefs.GetInt("MaxClearLevelID", 1);
    }
    
    private void Update()
    {
        UpdateLevelTimer();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    
    // 레벨 데이터폴더 안에 있는 모든 파일 로드해서 아이디 중복되는지 검사하기
    private void LoadAllLevelData()
    {
        LevelData[] loadedData = Resources.LoadAll<LevelData>("LevelData");
        foreach (var data in loadedData)
        {
            if (!levelData.ContainsKey(data.levelID))
            {
                levelData.Add(data.levelID, data);
            }
            else
            {
                Debug.Log("중복된 아이디 있음" + data.levelID);
            }
        }
    }
    
    // ID 로 데이터 불러오기
    public LevelData GetLevelData(int id)
    {
        if (levelData.ContainsKey(id))
        {
            return levelData[id];
        }
        Debug.Log($"{id} 는 없는 데이터임");
        return null;
    }

    public void EnterLevel(LevelData levelData)
    {
        lastEnteredId = levelData.levelID;
        SceneManager.sceneLoaded += OnLevelLoaded;
        SceneManager.LoadScene("Level_"+levelData.levelID);
        
        Time.timeScale = 1f;
        gameState = GameState.Preview;
     
        currentLevelData = levelData;
        currentTimer = levelData.timeLimit;
        currentReplayCount = levelData.maxReplyCount;
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
        
        if (cameraEffect != null && !isCamereEffectShowed)
        {
            cameraEffect.PlayPreViewSequence(OnPreviewFinished);
            isCamereEffectShowed = true;
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
        gameState = GameState.Lobby;

        Time.timeScale = 1;
        currentLevelData = null;
        currentTimer = 0;
        currentReplayCount = 0;
        timer = 0;
        isCamereEffectShowed = false;
        
        SceneManager.LoadScene("LevelSelection");
        SoundManager.instance.PlayLobbyBGM();
        OnExitLevel?.Invoke();
    }

    public async void ClearLevel()
    {
        gameState = GameState.Clear;

        if (currentLevelData.levelID >= maxClearLevelID)
        {
            if (currentLevelData.nextLevelData != null)
            {
                maxClearLevelID = currentLevelData.nextLevelData.levelID;
                PlayerPrefs.SetInt("MaxClearLevelID", maxClearLevelID);
            }
        }
        SoundManager.instance.PlaySFX(SfxType.Cheese);
        if (GameObject.FindGameObjectWithTag("Player").TryGetComponent(out Animator anim))
        {
            anim.SetTrigger("Eat");
            DataCollector.instance.RecordClear();
        }

        await System.Threading.Tasks.Task.Delay(2000);

        if (currentLevelData.nextLevelData == null)
        {
            OnFinalLevelClear?.Invoke();
        }
        else
        {
            ExitLevel();
        }
    }

    public void PlayerDie()
    {
        gameState = GameState.Die;
        
        // 플레이어 죽는 애니메이션 실행한 다음 현재 스테이지 LevelData 로 EnterLevel 하기
        EnterLevel(currentLevelData);
        DataCollector.instance.RecordDeath();
    }

    public void Retry()
    {
        EnterLevel(currentLevelData);
        DataCollector.instance.RecordRetry();
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
