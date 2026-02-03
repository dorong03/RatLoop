using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    
    public enum GameState { Ready, Playing, GameOver}
    
    public GameState CurrentGameState { get; private set; }
    public Transform CameraTarget { get; private set; }

    [SerializeField] private float timeLimit;
    [SerializeField] private int replayCounts;

    [SerializeField] private Transform cheesePosition;
    [SerializeField] private Transform playerPosition;

    private float currentTimer;

    

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        StartCoroutine(LevelStart());
    }

    private void Update()
    {
        if (CurrentGameState == GameState.Playing)
        {
            UpdateTimer();
        }
    }

    private void UpdateTimer()
    {
        currentTimer += Time.deltaTime;
        if(currentTimer >  timeLimit)
        {
            replayCounts--;
            if (replayCounts > 0)
            {
                RecordingManager.instance.StopPlayingRecord();
            }
            else
            {
                CurrentGameState = GameState.GameOver;
            }
        }
    }

    private void InitTimer()
    {
        currentTimer = 0;
    }

    private IEnumerator LevelStart()
    {
        CurrentGameState = GameState.Ready;
        yield return new WaitForSeconds(1f);
        
        CameraTarget = cheesePosition;
        yield return new WaitForSeconds(2f);

        CameraTarget = playerPosition;
        yield return new WaitForSeconds(1f);
        CurrentGameState = GameState.Playing;
    }

    public void OnTakeCheese()
    {
        
    }
    
    private void LoadNextLevel()
    {
        
    }
}
