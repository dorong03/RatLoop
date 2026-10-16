using UnityEngine;

// Game 씬에 하나 있다. GameManager 가 고른 레벨 프리팹을 생성하고 플레이어를 스폰한다.
// SpawnManager 보다 먼저 실행되어야 CameraEffect.Start 가 플레이어를 찾을 수 있다.
[DefaultExecutionOrder(-50)]
public class LevelLoader : MonoBehaviour
{
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private CameraEffect cameraEffect;

    [Tooltip("Game 씬을 바로 실행했을 때 불러올 레벨이에요. 비워 두면 목록의 첫 레벨을 불러와요.")]
    [SerializeField] private LevelRoot editorTestLevel;

#if UNITY_EDITOR
    // 레벨 에디터의 "테스트 플레이" 가 플레이 직전에 레벨 프리팹 경로를 넣어둔다. (도메인 리로드 후에도 유지)
    public const string TestLevelSessionKey = "RatLoop.TestLevelPath";
#endif

    public LevelRoot Current { get; private set; }

    private bool enterOnStart;

    private void Awake()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            Debug.LogError("GameManager가 없어요. Game 씬에 GameManager 프리팹을 넣어 주세요.");
            return;
        }

        LevelRoot level = gm.CurrentLevel;
        if (level == null)
        {
            // Game 씬을 에디터에서 바로 실행한 경우: Start 에서 정식 경로(EnterLevel)로 다시 들어온다.
            // Awake 에서 바로 들어가면 이 씬의 sceneLoaded 이벤트가 먼저 처리되어 프리뷰가 시작되지 않는다.
            enterOnStart = true;
            return;
        }

        Current = Instantiate(level, transform);
        Current.name = level.name;

        if (cameraEffect != null)
        {
            cameraEffect.SetPixelPerfectReference(Current.PixelPerfectReference);
        }

        SpawnPoint spawn = Current.SpawnPoint;
        if (spawn == null)
        {
            Debug.LogError($"{level.name}에 시작 위치(SpawnPoint)가 없어요.");
            return;
        }
        spawnManager.Initialize(spawn.transform);
    }

    private void Start()
    {
        if (enterOnStart)
        {
            GameManager gm = GameManager.instance;
            LevelRoot fallback = editorTestLevel != null ? editorTestLevel : gm.Levels.Get(0);
#if UNITY_EDITOR
            string testPath = UnityEditor.SessionState.GetString(TestLevelSessionKey, "");
            if (!string.IsNullOrEmpty(testPath))
            {
                UnityEditor.SessionState.EraseString(TestLevelSessionKey);
                GameObject testPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(testPath);
                if (testPrefab != null && testPrefab.TryGetComponent(out LevelRoot testLevel))
                {
                    fallback = testLevel;
                }
            }
#endif
            gm.EnterLevel(fallback);
        }
    }
}
