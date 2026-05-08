using System;
using UnityEngine;

public class DataCollector : MonoBehaviour
{
    public static DataCollector instance;

    private SessionData currentSession;
    private AttemptData currentAttempt;

    private float attemptTimer = 0f;
    private bool isTracking = false;
    private bool hasFirstInput = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            currentSession = new SessionData();
            String deviceName = SystemInfo.deviceName;
            currentSession.Init(deviceName);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameManager.instance.OnEnterLevel += OnEnterLevel;
        GameManager.instance.OnExitLevel += OnExitLevel;
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnEnterLevel -= OnEnterLevel;
            GameManager.instance.OnExitLevel -= OnExitLevel;
        }
    }

    private void Update()
    {
        if (isTracking)
            attemptTimer += Time.deltaTime;
    }

    
    // 강제 종료시 저장
    private void OnApplicationQuit()
    {
        if (currentAttempt != null)
        {
            currentAttempt.isClear = false;
            currentAttempt.deathTime = attemptTimer;
            CommitAttempt();
        }
    }

    // 레벨 진입 및 퇴장때 실행

    private void OnEnterLevel()
    {
        if (GameManager.instance.currentLevelData == null) return;

        currentAttempt = new AttemptData(GameManager.instance.currentLevelData.levelID);
        attemptTimer = 0f;
        isTracking = true;
        hasFirstInput = false;
    }

    private void OnExitLevel()
    {
        isTracking = false;
        CommitAttempt();
    }

    // 점프 저장
    public void RecordJump()
    {
        if (currentAttempt == null) return;

        if (!hasFirstInput)
        {
            hasFirstInput = true;
        }
        currentAttempt.jumpCount++;
    }

    // 죽을때 저장
    public void RecordDeath()
    {
        if (currentAttempt == null) return;
        currentAttempt.deathCount++;
        currentAttempt.deathTime = attemptTimer;
        currentAttempt.isClear = false;
        CommitAttempt();
    }

    // 클리어 기록 저장
    public void RecordClear()
    {
        if (currentAttempt == null) return;
        currentAttempt.isClear = true;
        currentAttempt.clearTime = attemptTimer;
        CommitAttempt();
    }

    // ㅍ리플레이 기능 저장
    public void RecordReplay()
    {
        if (currentAttempt == null) return;
        currentAttempt.replayCount++;
    }

    // 재시작할때 저장
    public void RecordRetry()
    {
        if (currentAttempt == null) return;
        currentAttempt.retryCount++;
    }

    // 혹시 기믹도 저장해야하면 사용할 듯
    public void RecordGimmickInteract()
    {
        if (currentAttempt == null) return;
        currentAttempt.gimmickInteractCount++;
    }

    // 모든 데이터를 DataSaver 로 보내기

    private void CommitAttempt()
    {
        if (currentAttempt == null) return;
        currentSession.attempts.Add(currentAttempt);
        DataSaver.instance.Save(currentSession);
        currentAttempt = null;
    }
}
