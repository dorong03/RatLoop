using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 기획자용 레벨 에디터. 메뉴: RatLoop > 레벨 에디터 (Ctrl+Shift+L)
//
// 사용 흐름
//   1. 레벨 목록에서 레벨을 열거나 새로 만든다.
//   2. 위쪽 모드 버튼(선택 / 타일 / 프롭 / 시작·치즈 / 검사)을 고르면 그 모드의 씬 도구가 바로 켜진다.
//   3. 저장하거나 테스트 플레이를 누른다.
// 이 파일은 창 화면(UI)이고, 씬 화면에서 동작하는 도구는 LevelEditorWindow.Scene.cs 에 있다.
public partial class LevelEditorWindow : EditorWindow
{
    private enum Mode { Select, Tiles, Props, Points, Check }
    private enum TileLayer { Ground, DecorationBack, DecorationFront }
    private enum Relocate { None, Spawn, Goal }

    private static readonly string[] ModeLabels = { "선택·이동", "타일", "프롭", "시작·치즈", "검사" };
    private static readonly string[] TileLayerLabels = { "지형", "뒤 장식", "앞 장식" };
    private static readonly string[] TileLayerHelp =
    {
        "밟고 부딪히는 땅과 벽입니다.",
        "캐릭터 뒤에 그려지는 장식입니다. 지나갈 수 있습니다.",
        "캐릭터 앞에 그려지는 장식입니다. 지나갈 수 있습니다."
    };
    private static readonly string[] TileLayerObjectNames = { "Ground", "Decoration", "DecorationFront" };
    private static readonly string[] SnapLabels = { "0.25칸", "0.5칸", "1칸" };
    private static readonly float[] SnapValues = { 0.25f, 0.5f, 1f };

    private static readonly (PropCategory? category, string label)[] PropFilters =
    {
        (null, "전체"), (PropCategory.Physics, "물리"), (PropCategory.Static, "고정"), (PropCategory.Decoration, "장식"),
        (PropCategory.Rope, "로프"), (PropCategory.Water, "물"), (PropCategory.Barrier, "투명벽"), (PropCategory.Gimmick, "기믹")
    };

    private static readonly Color ErrorColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color WarningColor = new Color(1f, 0.85f, 0.35f);
    private static readonly Color OkColor = new Color(0.55f, 0.9f, 0.55f);
    private static readonly Color SelectedTint = new Color(0.35f, 0.65f, 1f);

    [SerializeField] private Mode mode;
    [SerializeField] private TileLayer tileLayer;
    [SerializeField] private int snapIndex = 1;
    [SerializeField] private bool settingsOpen;
    [SerializeField] private bool eraser;
    [SerializeField] private bool showGrid = true;
    [SerializeField] private int tileCellSize = TileSheetImporter.DefaultCellSize;

    private readonly TilePaletteView palette = new TilePaletteView();
    private PropCatalog.Entry selectedProp;
    private int propFilterIndex;
    private string propSearch = "";
    private Relocate relocate;
    private string newLevelName = "";
    private Vector2 scroll;
    private Vector2 propScroll;

    private PropValidationReport report;
    private bool reportDirty = true;
    private double nextValidateTime;

    private GUIStyle titleStyle;
    private GUIStyle bannerStyle;
    private GUIStyle hintStyle;
    private GUIStyle cardTitleStyle;

    [MenuItem("RatLoop/레벨 에디터 %#l", priority = 0)]
    public static void Open()
    {
        var window = GetWindow<LevelEditorWindow>("레벨 에디터");
        window.minSize = new Vector2(340f, 460f);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += Repaint;
        EditorApplication.hierarchyChanged += MarkReportDirty;
        Undo.undoRedoPerformed += OnUndoRedo;
        PrefabStage.prefabStageOpened += OnStageChanged;
        PrefabStage.prefabStageClosing += OnStageChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= Repaint;
        EditorApplication.hierarchyChanged -= MarkReportDirty;
        Undo.undoRedoPerformed -= OnUndoRedo;
        PrefabStage.prefabStageOpened -= OnStageChanged;
        PrefabStage.prefabStageClosing -= OnStageChanged;
    }

    // 프로젝트에 타일이나 프롭이 추가되면 팔레트와 미리보기를 다시 만든다.
    private void OnProjectChange()
    {
        palette.Refresh();
        ghostCache.Clear();
        Repaint();
    }

    private void OnStageChanged(PrefabStage stage)
    {
        mode = Mode.Select;
        relocate = Relocate.None;
        selectedProp = null;
        reportDirty = true;
        Repaint();
    }

    private void OnUndoRedo()
    {
        // 실행 취소로 타일 데이터가 바뀌면 화면과 충돌 모양이 따라오지 않을 수 있어서 다시 그린다.
        LevelRoot level = LevelEditorTools.CurrentLevel;
        if (level != null)
        {
            foreach (var tilemap in level.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true))
            {
                tilemap.RefreshAllTiles();
            }
        }
        // 종류 설정(프리팹 원본)을 바꾼 것을 실행 취소한 경우 파일에도 반영한다.
        AssetDatabase.SaveAssets();
        MarkReportDirty();
        Repaint();
        SceneView.RepaintAll();
    }

    private void MarkReportDirty()
    {
        reportDirty = true;
    }

    // 문제 검사는 바뀐 뒤 잠깐 기다렸다가 한 번만 실행한다. (타일을 칠하는 동안 매 프레임 돌지 않게)
    private void OnInspectorUpdate()
    {
        if (reportDirty && EditorApplication.timeSinceStartup >= nextValidateTime && LevelEditorTools.CurrentLevel != null)
        {
            report = LevelEditorTools.Validate();
            reportDirty = false;
            nextValidateTime = EditorApplication.timeSinceStartup + 0.6;
            Repaint();
        }
    }

    // ================================================================ 화면

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
        cardTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        hintStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 11, wordWrap = true, alignment = TextAnchor.MiddleLeft };
        bannerStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 10, 4, 4)
        };
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("테스트 플레이 중입니다. 플레이를 멈추면 레벨 편집 화면으로 돌아옵니다.", MessageType.Info);
            return;
        }

        LevelRoot level = LevelEditorTools.CurrentLevel;
        if (level == null)
        {
            DrawBrowser();
            return;
        }

        DrawHeader(level);
        DrawNotInListBanner(level);
        DrawStatusAndSettings(level);
        DrawModeToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(4);
        switch (mode)
        {
            case Mode.Select: DrawSelectMode(level); break;
            case Mode.Tiles: DrawTilesMode(level); break;
            case Mode.Props: DrawPropsMode(); break;
            case Mode.Points: DrawPointsMode(level); break;
            case Mode.Check: DrawCheckMode(); break;
        }
        EditorGUILayout.Space(6);
        EditorGUILayout.EndScrollView();

        DrawHintBar();
    }

    // ---------------------------------------------------------------- 레벨 목록 화면

    private void DrawBrowser()
    {
        LevelList list = LevelEditorTools.LevelList;
        if (list == null)
        {
            EditorGUILayout.HelpBox("LevelList 에셋을 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("레벨 목록", titleStyle);
        EditorGUILayout.LabelField("게임에 나오는 순서입니다. ▲▼ 로 순서를 바꾸면 스테이지 번호가 바뀝니다.", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        var inList = new HashSet<LevelRoot>();
        for (int i = 0; i < list.Count; i++)
        {
            LevelRoot lv = list.Get(i);
            inList.Add(lv);
            DrawLevelCard(lv, i, list.Count);
        }

        var others = LevelEditorTools.FindAllLevels().FindAll(l => !inList.Contains(l));
        if (others.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("목록에 없는 레벨", cardTitleStyle);
            foreach (LevelRoot lv in others)
            {
                DrawLevelCard(lv, -1, list.Count);
            }
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("새 레벨 만들기", cardTitleStyle);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            newLevelName = EditorGUILayout.TextField("레벨 이름", newLevelName);
            if (GUILayout.Button("만들고 열기", GUILayout.Height(28)))
            {
                string levelName = string.IsNullOrWhiteSpace(newLevelName) ? $"스테이지 {list.Count + 1}" : newLevelName.Trim();
                LevelEditorTools.CreateLevel(levelName, addToList: true);
                newLevelName = "";
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField("바닥, 시작 위치, 치즈가 들어 있는 상태로 시작하고 목록 맨 뒤에 추가됩니다.", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("테스트용 진행도 (이 컴퓨터에만 저장)", cardTitleStyle);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            int unlocked = PlayerPrefs.GetInt("MaxClearLevelID", 1);
            EditorGUILayout.LabelField($"지금 열린 스테이지: 1 ~ {Mathf.Min(unlocked, list.Count)}  (전체 {list.Count}개)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("스테이지는 앞 스테이지를 클리어해야 열립니다. 새 레벨을 게임에서 바로 확인하려면 모두 열어 두세요.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("모든 스테이지 열기"))
                {
                    PlayerPrefs.SetInt("MaxClearLevelID", Mathf.Max(1, list.Count));
                    PlayerPrefs.Save();
                    ShowNotification(new GUIContent("모든 스테이지를 열었습니다"));
                }
                if (GUILayout.Button("진행 초기화"))
                {
                    PlayerPrefs.SetInt("MaxClearLevelID", 1);
                    PlayerPrefs.Save();
                    ShowNotification(new GUIContent("1스테이지만 열린 상태로 돌렸습니다"));
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelCard(LevelRoot lv, int index, int count)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(index >= 0 ? (index + 1).ToString() : "-", titleStyle, GUILayout.Width(26));
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label(lv != null ? lv.DisplayName : "(비어 있음)", cardTitleStyle);
                if (lv != null)
                {
                    GUILayout.Label($"{lv.name}  ·  제한 {lv.TimeLimit}초  ·  리플레이 {lv.MaxReplayCount}회", EditorStyles.miniLabel);
                }
            }
            GUILayout.FlexibleSpace();

            if (index >= 0)
            {
                using (new EditorGUI.DisabledScope(index == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(26), GUILayout.Height(26))) { LevelEditorTools.MoveInList(index, -1); GUIUtility.ExitGUI(); }
                }
                using (new EditorGUI.DisabledScope(index == count - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(26), GUILayout.Height(26))) { LevelEditorTools.MoveInList(index, 1); GUIUtility.ExitGUI(); }
                }
            }
            else if (lv != null && GUILayout.Button("목록에 추가", GUILayout.Width(80), GUILayout.Height(26)))
            {
                LevelEditorTools.AddToList(lv);
                GUIUtility.ExitGUI();
            }

            using (new EditorGUI.DisabledScope(lv == null))
            {
                if (GUILayout.Button("열기", GUILayout.Width(48), GUILayout.Height(26))) { LevelEditorTools.OpenLevel(lv); GUIUtility.ExitGUI(); }
            }
            if (index >= 0 && GUILayout.Button("빼기", GUILayout.Width(48), GUILayout.Height(26)) &&
                EditorUtility.DisplayDialog("레벨 빼기", $"{index + 1}번 레벨을 목록에서 뺄까요?\n레벨 파일은 지워지지 않고 '목록에 없는 레벨'로 남습니다.", "빼기", "취소"))
            {
                LevelEditorTools.RemoveFromList(index);
                GUIUtility.ExitGUI();
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (lv != null && GUILayout.Button("삭제", GUILayout.Width(48), GUILayout.Height(26)) &&
                EditorUtility.DisplayDialog("레벨 삭제",
                    $"'{lv.DisplayName}' ({lv.name}) 레벨을 삭제할까요?\n\n" +
                    "레벨 목록에서도 빠지고 파일은 컴퓨터의 휴지통으로 옮겨집니다.\n" +
                    "실수로 지웠다면 휴지통에서 되살리거나 Git 에서 복구할 수 있습니다.",
                    "삭제", "취소"))
            {
                GUI.backgroundColor = Color.white;
                LevelEditorTools.DeleteLevel(lv);
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    // ---------------------------------------------------------------- 편집 화면 공통

    private void DrawHeader(LevelRoot level)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool dirty = stage != null && stage.scene.isDirty;
        LevelList list = LevelEditorTools.LevelList;
        int index = list != null ? list.IndexOf(AssetDatabase.LoadAssetAtPath<LevelRoot>(LevelEditorTools.CurrentLevelPath)) : -1;
        string order = index >= 0 ? $"스테이지 {index + 1}" : "목록에 없음";

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("‹ 목록", EditorStyles.toolbarButton, GUILayout.Width(56)))
            {
                SetMode(Mode.Select);
                StageUtility.GoToMainStage();
                GUIUtility.ExitGUI();
            }
            GUILayout.Label($"{level.DisplayName}  ({order})", titleStyle);
            GUILayout.FlexibleSpace();
            if (dirty)
            {
                GUILayout.Label("● 저장 안 됨", EditorStyles.miniBoldLabel);
            }

            GUI.backgroundColor = dirty ? new Color(1f, 0.9f, 0.5f) : Color.white;
            if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                report = LevelEditorTools.SaveCurrentLevel();
                reportDirty = false;
                ShowNotification(new GUIContent(report.HasErrors ? "저장했지만 오류가 있습니다" : "저장했습니다"));
                if (report.HasErrors)
                {
                    SetMode(Mode.Check);
                }
            }
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("▶ 테스트 플레이", EditorStyles.toolbarButton, GUILayout.Width(104)))
            {
                RequestTestPlay();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    // 게임 목록에 없는 레벨은 스테이지 선택 화면에 나오지 않으므로 알려주고 바로 추가할 수 있게 한다.
    private void DrawNotInListBanner(LevelRoot level)
    {
        LevelList list = LevelEditorTools.LevelList;
        var asset = AssetDatabase.LoadAssetAtPath<LevelRoot>(LevelEditorTools.CurrentLevelPath);
        if (list == null || asset == null || list.IndexOf(asset) >= 0)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("이 레벨은 게임 목록에 없어서 스테이지 선택 화면에 나오지 않습니다.", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("게임 목록에 추가", GUILayout.Width(110), GUILayout.Height(22)))
            {
                LevelEditorTools.AddToList(asset);
                ShowNotification(new GUIContent($"스테이지 {list.Count}(으)로 추가했습니다"));
            }
        }
    }

    private void RequestTestPlay()
    {
        report = LevelEditorTools.Validate();
        reportDirty = false;
        if (report.HasErrors)
        {
            SetMode(Mode.Check);
            ShowNotification(new GUIContent("오류를 먼저 고쳐주세요"));
            return;
        }

        SetMode(Mode.Select);
        LevelEditorTools.TestPlay();
        GUIUtility.ExitGUI();
    }

    private void DrawStatusAndSettings(LevelRoot level)
    {
        int errors = 0, warnings = 0;
        if (report != null)
        {
            foreach (var e in report.Entries)
            {
                if (e.severity == PropValidationReport.Severity.Error) errors++;
                else warnings++;
            }
        }

        string text;
        Color color;
        if (report == null) { text = "검사 중..."; color = Color.white; }
        else if (errors > 0) { text = $"오류 {errors}개" + (warnings > 0 ? $", 경고 {warnings}개" : "") + "  (눌러서 보기)"; color = ErrorColor; }
        else if (warnings > 0) { text = $"경고 {warnings}개  (눌러서 보기)"; color = WarningColor; }
        else { text = "문제 없음"; color = OkColor; }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = color;
            if (GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Height(20)))
            {
                SetMode(Mode.Check);
            }
            GUI.backgroundColor = Color.white;

            bool grid = GUILayout.Toggle(showGrid, "격자", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(20));
            if (grid != showGrid)
            {
                showGrid = grid;
                SceneView.RepaintAll();
            }
        }

        string summary = $"레벨 설정   ·   제한 {level.TimeLimit}초   ·   리플레이 {level.MaxReplayCount}회   ·   카메라 {(level.Zoom == LevelRoot.CameraZoom.Wide ? "넓게" : "보통")}";
        settingsOpen = EditorGUILayout.Foldout(settingsOpen, summary, true);
        if (settingsOpen)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var so = new SerializedObject(level);
                so.Update();
                if (PlannerFieldUtility.DrawPlannerFields(so))
                {
                    MarkReportDirty();
                }
                so.ApplyModifiedProperties();
            }
        }
    }

    private void DrawModeToolbar()
    {
        EditorGUILayout.Space(2);
        Mode newMode = (Mode)GUILayout.Toolbar((int)mode, ModeLabels, GUILayout.Height(28));
        if (newMode != mode)
        {
            SetMode(newMode);
        }
    }

    private void SetMode(Mode newMode)
    {
        mode = newMode;
        if (mode != Mode.Points)
        {
            relocate = Relocate.None;
        }
        if (mode != Mode.Props)
        {
            selectedProp = null;
        }
        if (mode == Mode.Check)
        {
            report = LevelEditorTools.Validate();
            reportDirty = false;
        }
        GUIUtility.keyboardControl = 0;
        Repaint();
        SceneView.RepaintAll();
    }

    private void DrawHintBar()
    {
        GUILayout.Label(CurrentHint(), hintStyle, GUILayout.MinHeight(34));
    }

    private string CurrentHint()
    {
        switch (mode)
        {
            case Mode.Tiles:
                if (eraser) return "지우개: 씬 화면에서 클릭하거나 끌어서 지웁니다.   Esc: 끝내기";
                return palette.HasStamp
                    ? "씬 화면에서 클릭하거나 끌어서 칠합니다.   Shift: 지우기   Esc: 끝내기"
                    : "위 팔레트에서 타일이나 영역을 고르세요.";
            case Mode.Props:
                return selectedProp != null
                    ? $"'{selectedProp.displayName}' : 씬 화면을 클릭하면 놓입니다.   Esc: 끝내기"
                    : "위에서 놓을 프롭을 고르세요.";
            case Mode.Points:
                return relocate != Relocate.None
                    ? $"씬 화면을 클릭하거나 끌면 {(relocate == Relocate.Spawn ? "시작 위치" : "치즈")}가 그 자리로 옮겨집니다.   Esc: 끝내기"
                    : "버튼을 눌러 옮기거나, 씬 화면의 원을 끌어서 옮깁니다.";
            case Mode.Check:
                return "빨간 오류가 있으면 테스트 플레이를 할 수 없습니다. 노란 경고는 확인만 하면 됩니다.";
            default:
                return "씬 화면에서 프롭을 클릭해 고르고 끌어서 옮깁니다.   (W: 이동, E: 회전)";
        }
    }

    // ---------------------------------------------------------------- 선택·이동

    private void DrawSelectMode(LevelRoot level)
    {
        GameObject go = Selection.activeGameObject;
        LevelProp prop = go != null ? go.GetComponentInParent<LevelProp>() : null;
        if (prop != null && prop.transform.IsChildOf(level.transform))
        {
            DrawSelectedProp(level, prop);
            EditorGUILayout.Space(8);
        }
        else
        {
            EditorGUILayout.HelpBox("씬 화면에서 프롭을 클릭하면 여기서 값을 바꿀 수 있습니다.", MessageType.None);
        }

        EditorGUILayout.LabelField("이 레벨에 있는 프롭", cardTitleStyle);
        var props = new List<LevelProp>(level.GetComponentsInChildren<LevelProp>(true));
        props.Sort((a, b) => a.Category != b.Category ? a.Category.CompareTo(b.Category) : string.CompareOrdinal(a.name, b.name));
        if (props.Count == 0)
        {
            EditorGUILayout.LabelField("아직 없습니다. '프롭' 모드에서 놓아보세요.", EditorStyles.miniLabel);
        }
        PropCategory? current = null;
        foreach (LevelProp p in props)
        {
            if (current != p.Category)
            {
                current = p.Category;
                EditorGUILayout.LabelField(CategoryLabel(p.Category), EditorStyles.miniBoldLabel);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isSelected = p == prop;
                GUI.backgroundColor = isSelected ? SelectedTint : Color.white;
                if (GUILayout.Button(p.name, EditorStyles.miniButtonLeft))
                {
                    SelectAndFrame(p.gameObject);
                }
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("찾기", EditorStyles.miniButtonRight, GUILayout.Width(40)))
                {
                    SelectAndFrame(p.gameObject);
                }
            }
        }
    }

    private void DrawSelectedProp(LevelRoot level, LevelProp prop)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(prop.name, titleStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(CategoryLabel(prop.Category), EditorStyles.miniLabel, GUILayout.Width(60));
            }

            if (PlannerFieldUtility.HasFields(prop.GetType(), PlannerFieldUtility.Scope.Shared))
            {
                DrawTypeSettings(prop, PlannerFieldUtility.IsEditingType(prop)
                    ? "같은 종류 전체에 적용 (모든 레벨)"
                    : "설정");
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("이 자리에만 적용", EditorStyles.miniBoldLabel);
            var so = new SerializedObject(prop);
            so.Update();
            if (PlannerFieldUtility.HasFields(prop.GetType(), PlannerFieldUtility.Scope.PerInstance))
            {
                if (PlannerFieldUtility.DrawPlannerFields(so, PlannerFieldUtility.Scope.PerInstance))
                {
                    MarkReportDirty();
                }
            }
            so.ApplyModifiedProperties();

            DrawPositionFields(prop.transform);

            if (!(prop is IAreaProp) && !(prop is SpawnPoint))
            {
                DrawAngleFields(prop.transform);
            }
            else if (prop is IAreaProp)
            {
                EditorGUILayout.HelpBox("크기: 위 '가로/세로 칸 수'를 바꾸거나, 씬 화면에서 오른쪽 점(가로), 위쪽 점(세로), 오른쪽 위 네모(둘 다)를 끌어서 바꿉니다.", MessageType.None);
            }

            var r = new PropValidationReport();
            prop.Validate(r);
            foreach (PropValidationReport.Entry e in r.Entries)
            {
                EditorGUILayout.HelpBox(e.message, e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
            }

            bool unique = prop is SpawnPoint || prop is GoalProp;
            using (new EditorGUI.DisabledScope(unique))
            {
                if (GUILayout.Button(unique ? "시작 위치와 치즈는 지울 수 없습니다" : "이 프롭 지우기"))
                {
                    Undo.DestroyObjectImmediate(prop.gameObject);
                    MarkReportDirty();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    // 종류 공통 설정: 프리팹 원본을 고치므로 이 프롭을 쓰는 모든 레벨에 함께 적용된다.
    private void DrawTypeSettings(Component instanceOrPrefab, string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (PlannerFieldUtility.DrawSharedFields(instanceOrPrefab))
            {
                // 크기처럼 오브젝트에 직접 반영되는 값은 미리보기와 이미 놓은 같은 프롭에도 바로 맞춘다.
                ghostCache.Clear();
                LevelRoot level = LevelEditorTools.CurrentLevel;
                if (level != null)
                {
                    foreach (LevelProp prop in level.GetComponentsInChildren<LevelProp>(true))
                    {
                        prop.ApplySettings();
                    }
                }
                MarkReportDirty();
            }

            List<string> overrides = PlannerFieldUtility.GetSharedOverrides(instanceOrPrefab);
            if (overrides.Count > 0)
            {
                EditorGUILayout.HelpBox($"이 자리의 프롭만 값이 다릅니다: {string.Join(", ", overrides)}\n같은 종류는 같은 값을 쓰도록 맞추는 것을 권장합니다.", MessageType.Warning);
                if (GUILayout.Button("종류 설정으로 맞추기"))
                {
                    PlannerFieldUtility.RevertSharedOverrides(instanceOrPrefab);
                    MarkReportDirty();
                }
            }
        }
    }

    private static void DrawPositionFields(Transform t)
    {
        EditorGUI.BeginChangeCheck();
        Vector2 pos = EditorGUILayout.Vector2Field("위치 (X, Y)", new Vector2(t.position.x, t.position.y));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(t, "위치 변경");
            t.position = new Vector3(pos.x, pos.y, t.position.z);
        }
    }

    private static void DrawAngleFields(Transform t)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("각도", GUILayout.Width(40));
            EditorGUI.BeginChangeCheck();
            float angle = EditorGUILayout.FloatField(Mathf.Round(t.localEulerAngles.z * 10f) / 10f, GUILayout.Width(52));
            if (EditorGUI.EndChangeCheck())
            {
                SetAngle(t, angle);
            }
            foreach (float step in new[] { -90f, -15f, 15f, 90f })
            {
                if (GUILayout.Button(step > 0 ? $"+{step}" : step.ToString(), EditorStyles.miniButton))
                {
                    SetAngle(t, t.localEulerAngles.z + step);
                }
            }
            if (GUILayout.Button("좌우 뒤집기", EditorStyles.miniButton))
            {
                if (t.TryGetComponent(out SizedProp sized))
                {
                    // 크기 프롭은 크기를 직접 계산하므로 뒤집기 값을 바꾼다.
                    Undo.RecordObjects(new UnityEngine.Object[] { sized, t }, "좌우 뒤집기");
                    sized.FlipX = !sized.FlipX;
                    sized.ApplySettings();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(sized);
                }
                else
                {
                    Undo.RecordObject(t, "좌우 뒤집기");
                    Vector3 s = t.localScale;
                    t.localScale = new Vector3(-s.x, s.y, s.z);
                }
            }
        }
    }

    private static void SetAngle(Transform t, float angle)
    {
        Undo.RecordObject(t, "각도 변경");
        Vector3 e = t.localEulerAngles;
        t.localEulerAngles = new Vector3(e.x, e.y, Mathf.Repeat(angle, 360f));
    }

    private static string CategoryLabel(PropCategory c)
    {
        switch (c)
        {
            case PropCategory.Physics: return "물리 프롭";
            case PropCategory.Static: return "고정 프롭";
            case PropCategory.Decoration: return "장식";
            case PropCategory.Rope: return "로프";
            case PropCategory.Water: return "물";
            case PropCategory.Gimmick: return "기믹";
            case PropCategory.Goal: return "치즈";
            case PropCategory.Spawn: return "시작 위치";
            case PropCategory.Barrier: return "투명벽";
            default: return c.ToString();
        }
    }

    // ---------------------------------------------------------------- 타일

    private void DrawTilesMode(LevelRoot level)
    {
        EditorGUILayout.LabelField("어디에 칠할까요?", cardTitleStyle);
        tileLayer = (TileLayer)GUILayout.Toolbar((int)tileLayer, TileLayerLabels, GUILayout.Height(24));
        EditorGUILayout.LabelField(TileLayerHelp[(int)tileLayer], EditorStyles.miniLabel);
        if (FindTargetTilemap(level) == null)
        {
            EditorGUILayout.HelpBox($"이 레벨에 '{TileLayerObjectNames[(int)tileLayer]}' 타일맵이 없습니다.", MessageType.Warning);
        }

        EditorGUILayout.Space(4);
        eraser = GUILayout.Toolbar(eraser ? 1 : 0, new[] { "칠하기", "지우개" }, GUILayout.Height(24)) == 1;
        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(eraser))
        {
            float height = Mathf.Clamp(position.height - 440f, 180f, 640f);
            if (palette.Draw(height))
            {
                eraser = false;
                SceneView.RepaintAll();
            }
            if (palette.HasStamp && GUILayout.Button("선택 해제", EditorStyles.miniButton))
            {
                palette.ClearSelection();
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("새 타일 이미지", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            GUILayout.Label("칸 크기", EditorStyles.miniLabel, GUILayout.Width(40));
            tileCellSize = Mathf.Clamp(EditorGUILayout.IntField(tileCellSize, GUILayout.Width(36)), 4, 256);
            GUILayout.Label("px", EditorStyles.miniLabel, GUILayout.Width(18));
            if (GUILayout.Button("PNG 추가..."))
            {
                string path = EditorUtility.OpenFilePanel("타일 이미지 (PNG)", "", "png");
                if (!string.IsNullOrEmpty(path))
                {
                    int count = TileSheetImporter.ImportFromDisk(path, tileCellSize);
                    palette.Refresh();
                    ShowNotification(new GUIContent($"타일 {count}개를 추가했습니다"));
                }
                GUIUtility.ExitGUI();
            }
        }
    }

    // ---------------------------------------------------------------- 프롭

    private void DrawPropsMode()
    {
        PropCatalog catalog = LevelEditorTools.Catalog;
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("PropCatalog 에셋을 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            propSearch = GUILayout.TextField(propSearch, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("+ 새 프롭 만들기", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                NewPropWindow.Open(SelectCatalogEntry);
            }
        }
        int columnsForChips = Mathf.Max(2, Mathf.FloorToInt((position.width - 20f) / 64f));
        var labels = new string[PropFilters.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = PropFilters[i].label;
        }
        propFilterIndex = GUILayout.SelectionGrid(propFilterIndex, labels, columnsForChips, EditorStyles.miniButton);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("놓는 간격", EditorStyles.miniLabel, GUILayout.Width(52));
            snapIndex = EditorGUILayout.Popup(snapIndex, SnapLabels, GUILayout.Width(70));
            GUILayout.Label("(물, 투명벽은 항상 1칸)", EditorStyles.miniLabel);
        }

        PropCategory? filter = PropFilters[propFilterIndex].category;
        string search = propSearch.Trim();
        var entries = new List<PropCatalog.Entry>();
        foreach (PropCatalog.Entry e in catalog.Entries)
        {
            if (e.hiddenInPalette || e.prefab == null)
            {
                continue;
            }
            if (filter != null && e.Category != filter)
            {
                continue;
            }
            if (search.Length > 0 && e.displayName.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }
            entries.Add(e);
        }

        EditorGUILayout.Space(2);
        if (entries.Count == 0)
        {
            EditorGUILayout.LabelField("해당하는 프롭이 없습니다.", EditorStyles.miniLabel);
        }

        const float cell = 76f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / (cell + 4f)));
        for (int i = 0; i < entries.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = i; j < Mathf.Min(i + columns, entries.Count); j++)
                {
                    DrawPropCell(entries[j], cell);
                }
            }
        }

        if (selectedProp != null)
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(selectedProp.displayName, cardTitleStyle);
                if (!string.IsNullOrEmpty(selectedProp.description))
                {
                    EditorGUILayout.LabelField(selectedProp.description, EditorStyles.wordWrappedMiniLabel);
                }
                LevelProp typeProp = selectedProp.Prop;
                if (typeProp != null && PlannerFieldUtility.HasFields(typeProp.GetType(), PlannerFieldUtility.Scope.Shared))
                {
                    EditorGUILayout.Space(2);
                    DrawTypeSettings(typeProp, "이 종류의 설정 (이미 놓은 것과 모든 레벨에 함께 적용)");
                }
            }
        }
    }

    // 새로 만든 프롭을 팔레트에서 바로 고른 상태로 만든다.
    private void SelectCatalogEntry(string id)
    {
        PropCatalog catalog = LevelEditorTools.Catalog;
        selectedProp = catalog != null ? catalog.Find(id) : null;
        propFilterIndex = 0;
        propSearch = "";
        ghostCache.Clear();
        if (LevelEditorTools.CurrentLevel != null)
        {
            mode = Mode.Props;
        }
        ShowNotification(new GUIContent("프롭을 만들었습니다. 씬 화면을 클릭해 놓아보세요"));
        Repaint();
    }

    private void DrawPropCell(PropCatalog.Entry e, float cell)
    {
        Rect r = GUILayoutUtility.GetRect(cell, cell + 16f, GUILayout.Width(cell), GUILayout.Height(cell + 16f));
        bool selected = selectedProp != null && selectedProp.id == e.id;
        EditorGUI.DrawRect(r, selected ? new Color(0.25f, 0.55f, 1f, 0.55f) : new Color(0f, 0f, 0f, 0.15f));
        Sprite icon = e.GetIcon();
        var iconRect = new Rect(r.x + 6, r.y + 4, cell - 12, cell - 12);
        if (icon != null)
        {
            TilePaletteView.DrawSprite(FitRect(iconRect, icon), icon);
        }
        else
        {
            GUI.Label(iconRect, CategoryLabel(e.Category), EditorStyles.centeredGreyMiniLabel);
        }
        GUI.Label(new Rect(r.x, r.yMax - 18, r.width, 18), e.displayName, EditorStyles.centeredGreyMiniLabel);
        if (GUI.Button(r, new GUIContent("", e.displayName + (string.IsNullOrEmpty(e.description) ? "" : "\n" + e.description)), GUIStyle.none))
        {
            selectedProp = selected ? null : e;
            SceneView.RepaintAll();
        }
    }

    private static Rect FitRect(Rect box, Sprite sprite)
    {
        Rect tr = sprite.textureRect;
        float aspect = tr.width / tr.height;
        Rect fit = box;
        if (aspect > box.width / box.height)
        {
            fit.height = box.width / aspect;
            fit.y += (box.height - fit.height) * 0.5f;
        }
        else
        {
            fit.width = box.height * aspect;
            fit.x += (box.width - fit.width) * 0.5f;
        }
        return fit;
    }

    // ---------------------------------------------------------------- 시작·치즈

    private void DrawPointsMode(LevelRoot level)
    {
        DrawPointCard("시작 위치", level.SpawnPoint != null ? level.SpawnPoint.transform : null, Relocate.Spawn,
            "쥐가 처음 나타나는 곳입니다. 아래로 10칸 안에 땅이 있어야 합니다.");
        EditorGUILayout.Space(6);
        GoalProp goal = level.GetComponentInChildren<GoalProp>(true);
        DrawPointCard("치즈", goal != null ? goal.transform : null, Relocate.Goal,
            "쥐가 먹으면 레벨이 클리어됩니다.");
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("놓는 간격", EditorStyles.miniLabel);
        snapIndex = GUILayout.Toolbar(snapIndex, SnapLabels);
    }

    private void DrawPointCard(string title, Transform target, Relocate which, string description)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, cardTitleStyle);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            if (target == null)
            {
                EditorGUILayout.HelpBox($"{title}가 레벨에 없습니다.", MessageType.Error);
                return;
            }

            DrawPositionFields(target);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool active = relocate == which;
                GUI.backgroundColor = active ? SelectedTint : Color.white;
                if (GUILayout.Button(active ? "옮기는 중... (Esc: 끝내기)" : "씬 화면을 클릭해서 옮기기", GUILayout.Height(24)))
                {
                    relocate = active ? Relocate.None : which;
                    SceneView.RepaintAll();
                }
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("찾기", GUILayout.Width(50), GUILayout.Height(24)))
                {
                    SelectAndFrame(target.gameObject);
                }
            }
        }
    }

    // ---------------------------------------------------------------- 검사

    private void DrawCheckMode()
    {
        if (GUILayout.Button("다시 검사", GUILayout.Height(24)))
        {
            report = LevelEditorTools.Validate();
            reportDirty = false;
        }

        if (report == null)
        {
            report = LevelEditorTools.Validate();
            reportDirty = false;
        }

        if (report.Entries.Count == 0)
        {
            EditorGUILayout.HelpBox("문제가 없습니다. 저장하고 테스트 플레이를 해보세요.", MessageType.Info);
            return;
        }

        foreach (PropValidationReport.Entry e in report.Entries)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(e.message, e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
                using (new EditorGUI.DisabledScope(e.context == null))
                {
                    if (GUILayout.Button("찾기", GUILayout.Width(44), GUILayout.Height(38)))
                    {
                        var c = e.context as Component;
                        SelectAndFrame(c != null ? c.gameObject : e.context as GameObject);
                    }
                }
            }
        }
    }

    // ================================================================ 공통

    private UnityEngine.Tilemaps.Tilemap FindTargetTilemap(LevelRoot level)
    {
        string objectName = TileLayerObjectNames[(int)tileLayer];
        foreach (var tm in level.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true))
        {
            if (tm.name == objectName && tm.GetComponentInParent<WaterVolume>() == null)
            {
                return tm;
            }
        }
        return null;
    }

    private static void SelectAndFrame(GameObject go)
    {
        if (go == null)
        {
            return;
        }
        Selection.activeGameObject = go;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }
}
