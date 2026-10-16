using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 레벨 에디터 창이 쓰는 기능 모음: 레벨 생성/열기/저장, 레벨 목록 관리, 테스트 플레이.
public static class LevelEditorTools
{
    public const string LevelsFolder = "Assets/_Project/Levels";
    public const string TemplatePath = "Assets/_Project/Prefabs/Level/LevelTemplate.prefab";
    public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string ReturnToLevelKey = "RatLoop.ReturnToLevelPath";

    // ------------------------------------------------------------ 에셋 찾기

    public static T FindAsset<T>() where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null)
            {
                return asset;
            }
        }
        return null;
    }

    public static PropCatalog Catalog => FindAsset<PropCatalog>();
    public static LevelList LevelList => FindAsset<LevelList>();

    // 레벨 폴더의 모든 레벨 프리팹 (목록에 없는 것 포함)
    public static List<LevelRoot> FindAllLevels()
    {
        var result = new List<LevelRoot>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LevelsFolder }))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go != null && go.TryGetComponent(out LevelRoot level))
            {
                result.Add(level);
            }
        }
        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    // ------------------------------------------------------------ 열기 / 만들기 / 저장

    // 지금 열려 있는 레벨 (프리팹 편집 화면의 루트)
    public static LevelRoot CurrentLevel
    {
        get
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && stage.prefabContentsRoot != null
                ? stage.prefabContentsRoot.GetComponent<LevelRoot>()
                : null;
        }
    }

    public static string CurrentLevelPath
    {
        get
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null ? stage.assetPath : null;
        }
    }

    public static void OpenLevel(LevelRoot levelPrefab)
    {
        if (levelPrefab == null)
        {
            return;
        }
        OpenLevel(AssetDatabase.GetAssetPath(levelPrefab));
    }

    public static void OpenLevel(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        PrefabStageUtility.OpenPrefab(path);
        FrameLevel();
    }

    public static void FrameLevel()
    {
        LevelRoot level = CurrentLevel;
        SceneView view = SceneView.lastActiveSceneView;
        if (level == null || view == null)
        {
            return;
        }

        var bounds = new Bounds(level.transform.position, Vector3.one);
        foreach (Renderer r in level.GetComponentsInChildren<Renderer>())
        {
            if (!(r is UnityEngine.Tilemaps.TilemapRenderer) && r.transform.parent != null && r.transform.parent.name == "Background")
            {
                continue;
            }
            bounds.Encapsulate(r.bounds);
        }
        view.in2DMode = true;
        view.Frame(bounds, false);
    }

    // 템플릿을 복사해 새 레벨을 만든다. 레벨 목록 맨 뒤에 추가하고 바로 연다.
    public static string CreateLevel(string displayName, bool addToList)
    {
        if (!AssetDatabase.IsValidFolder(LevelsFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Levels");
        }

        string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelsFolder}/Level_{NextLevelNumber():00}.prefab");
        if (!AssetDatabase.CopyAsset(TemplatePath, path))
        {
            Debug.LogError("레벨 템플릿을 복사하지 못했습니다: " + TemplatePath);
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        root.name = Path.GetFileNameWithoutExtension(path);
        var so = new SerializedObject(root.GetComponent<LevelRoot>());
        so.FindProperty("displayName").stringValue = string.IsNullOrEmpty(displayName) ? "새 레벨" : displayName;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        if (addToList)
        {
            AddToList(AssetDatabase.LoadAssetAtPath<LevelRoot>(path));
        }

        OpenLevel(path);
        return path;
    }

    private static int NextLevelNumber()
    {
        int max = 0;
        foreach (LevelRoot level in FindAllLevels())
        {
            string n = level.name.Replace("Level_", "");
            if (int.TryParse(n, out int value))
            {
                max = Mathf.Max(max, value);
            }
        }
        return max + 1;
    }

    // 세이프티 체크 후 저장한다. 오류가 있어도 저장은 하되(작업 손실 방지) 결과를 돌려준다.
    public static PropValidationReport SaveCurrentLevel()
    {
        var report = Validate();
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || CurrentLevel == null)
        {
            return report;
        }

        PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
        stage.ClearDirtiness();
        return report;
    }

    public static PropValidationReport Validate()
    {
        var report = new PropValidationReport();
        LevelRoot level = CurrentLevel;
        if (level == null)
        {
            report.Error("열려 있는 레벨이 없습니다.");
            return report;
        }

        level.Validate(report, Catalog);
        return report;
    }

    // ------------------------------------------------------------ 레벨 목록

    public static void AddToList(LevelRoot levelPrefab)
    {
        LevelList list = LevelList;
        if (list == null || levelPrefab == null || list.IndexOf(levelPrefab) >= 0)
        {
            return;
        }

        var so = new SerializedObject(list);
        SerializedProperty levels = so.FindProperty("levels");
        levels.InsertArrayElementAtIndex(levels.arraySize);
        levels.GetArrayElementAtIndex(levels.arraySize - 1).objectReferenceValue = levelPrefab;
        so.ApplyModifiedProperties();
        SaveList(list);
    }

    public static void RemoveFromList(int index)
    {
        LevelList list = LevelList;
        var so = new SerializedObject(list);
        SerializedProperty levels = so.FindProperty("levels");
        if (index < 0 || index >= levels.arraySize)
        {
            return;
        }
        // 오브젝트 참조 원소는 한 번 지우면 null 이 되고, 두 번째에 실제로 빠진다.
        levels.GetArrayElementAtIndex(index).objectReferenceValue = null;
        levels.DeleteArrayElementAtIndex(index);
        so.ApplyModifiedProperties();
        SaveList(list);
    }

    public static void MoveInList(int index, int direction)
    {
        LevelList list = LevelList;
        var so = new SerializedObject(list);
        SerializedProperty levels = so.FindProperty("levels");
        int target = index + direction;
        if (index < 0 || target < 0 || index >= levels.arraySize || target >= levels.arraySize)
        {
            return;
        }
        levels.MoveArrayElement(index, target);
        so.ApplyModifiedProperties();
        SaveList(list);
    }

    private static void SaveList(LevelList list)
    {
        EditorUtility.SetDirty(list);
        AssetDatabase.SaveAssetIfDirty(list);
    }

    // ------------------------------------------------------------ 테스트 플레이

    public static void TestPlay()
    {
        string path = CurrentLevelPath;
        if (CurrentLevel == null || string.IsNullOrEmpty(path))
        {
            return;
        }

        SaveCurrentLevel();
        SessionState.SetString(LevelLoader.TestLevelSessionKey, path);
        SessionState.SetString(ReturnToLevelKey, path);

        StageUtility.GoToMainStage();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            SessionState.EraseString(LevelLoader.TestLevelSessionKey);
            SessionState.EraseString(ReturnToLevelKey);
            return;
        }
        EditorSceneManager.OpenScene(GameScenePath);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void RegisterReturnToLevel()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            string path = SessionState.GetString(ReturnToLevelKey, "");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            SessionState.EraseString(ReturnToLevelKey);
            OpenLevelWhenIdle(path);
        };
    }

    // 플레이 종료 직후에는 Unity 가 씬 구성을 복원하면서 프리팹 편집 화면을 닫을 수 있어서,
    // 정지가 끝나고 몇 번 갱신된 뒤에 연다.
    private static void OpenLevelWhenIdle(string path)
    {
        int framesToWait = 10;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || --framesToWait > 0)
            {
                return;
            }
            EditorApplication.update -= tick;
            OpenLevel(path);
        };
        EditorApplication.update += tick;
    }
}
