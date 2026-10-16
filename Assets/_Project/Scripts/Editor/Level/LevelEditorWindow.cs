using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 기획자용 레벨 에디터. 메뉴: RatLoop > 레벨 에디터 (Ctrl+Shift+L)
//
// 화면 구성 (EditorUI 의 원칙을 따른다)
//   레벨 목록: 레벨 카드 목록 + 바닥 줄의 '새 레벨 만들기'
//   레벨 편집: 위 툴바(목록, 저장, 테스트 플레이) → 레벨 상태 줄 → 모드 탭 → 본문 → 바닥 줄(지금 할 수 있는 일)
// 이 파일은 창 화면(UI)이고, 씬 화면에서 동작하는 도구는 LevelEditorWindow.Scene.cs 에 있다.
public partial class LevelEditorWindow : EditorWindow
{
    private enum Mode { Select, Tiles, Props, Points, Check }
    private enum TileLayer { Ground, DecorationBack, DecorationFront }
    private enum Relocate { None, Spawn, Goal }

    private static readonly string[] TileLayerLabels = { "지형", "뒤 장식", "앞 장식" };
    private static readonly string[] TileLayerHelp =
    {
        "밟고 부딪히는 땅과 벽이에요.",
        "캐릭터 뒤에 그려지고, 부딪히지 않아요.",
        "캐릭터 앞에 그려지고, 부딪히지 않아요."
    };
    private static readonly string[] TileLayerObjectNames = { "Ground", "Decoration", "DecorationFront" };
    private static readonly string[] SnapLabels = { "0.25칸", "0.5칸", "1칸" };
    private static readonly float[] SnapValues = { 0.25f, 0.5f, 1f };
    private static readonly int[] TileCellSizes = { 8, 16, 32, 64 };

    private static readonly (PropCategory? category, string label)[] PropFilters =
    {
        (null, "전체"), (PropCategory.Physics, "물리"), (PropCategory.Static, "고정"), (PropCategory.Decoration, "장식"),
        (PropCategory.Rope, "로프"), (PropCategory.Water, "물"), (PropCategory.Barrier, "투명벽"), (PropCategory.Gimmick, "기믹")
    };

    private const string UnlockedKey = "MaxClearLevelID";

    [SerializeField] private Mode mode;
    [SerializeField] private TileLayer tileLayer;
    [SerializeField] private int snapIndex = 1;
    [SerializeField] private bool settingsOpen;
    [SerializeField] private bool progressOpen;
    [SerializeField] private bool eraser;
    [SerializeField] private bool showGrid = true;

    private readonly TilePaletteView palette = new TilePaletteView();
    private PropCatalog.Entry selectedProp;
    private int propFilterIndex;
    private string propSearch = "";
    private Relocate relocate;
    private string newLevelName = "";
    private Vector2 scroll;

    private PropValidationReport report;
    private bool reportDirty = true;
    private double nextValidateTime;

    [MenuItem("RatLoop/레벨 에디터 %#l", priority = 0)]
    public static void Open()
    {
        var window = GetWindow<LevelEditorWindow>("레벨 에디터");
        window.minSize = new Vector2(360f, 480f);
    }

    private void OnEnable()
    {
        titleContent = EditorUI.Icon("Grid.Default", "레벨 에디터");
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
        // 공통 설정(프리팹 원본)을 바꾼 것을 실행 취소한 경우 파일에도 반영한다.
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

    private void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("테스트 플레이 중이에요", EditorUI.Title);
            GUILayout.Label("플레이를 멈추면 편집 화면으로 돌아와요.", EditorUI.Caption);
            GUILayout.FlexibleSpace();
            return;
        }

        LevelRoot level = LevelEditorTools.CurrentLevel;
        if (level == null)
        {
            DrawBrowser();
            return;
        }

        DrawToolbar(level);
        DrawLevelBar(level);
        DrawModeTabs();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(2);
        switch (mode)
        {
            case Mode.Select: DrawSelectMode(level); break;
            case Mode.Tiles: DrawTilesMode(level); break;
            case Mode.Props: DrawPropsMode(); break;
            case Mode.Points: DrawPointsMode(level); break;
            case Mode.Check: DrawCheckMode(); break;
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();

        EditorUI.FooterBar(() => GUILayout.Label(CurrentHint(), EditorUI.Caption));
    }

    // ---------------------------------------------------------------- 레벨 목록 화면

    private void DrawBrowser()
    {
        LevelList list = LevelEditorTools.LevelList;
        if (list == null)
        {
            EditorGUILayout.HelpBox("LevelList 에셋을 찾을 수 없어요.", MessageType.Error);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(8);
        GUILayout.Label("레벨", EditorUI.Title);
        GUILayout.Label("게임에 나오는 순서예요. 화살표로 순서를 바꾸면 스테이지 번호도 바뀌어요.", EditorUI.Caption);
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
            EditorUI.Header("게임 목록에 없는 레벨", "스테이지 선택 화면에 나오지 않아요.");
            foreach (LevelRoot lv in others)
            {
                DrawLevelCard(lv, -1, list.Count);
            }
        }

        EditorGUILayout.Space(8);
        DrawTestProgress(list);
        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();

        // 새 레벨 만들기는 목록이 길어져도 항상 보이도록 바닥 줄에 둔다.
        EditorUI.FooterBar(() =>
        {
            GUILayout.Label("새 레벨", EditorUI.Section);
            using (new EditorGUILayout.HorizontalScope())
            {
                newLevelName = EditorUI.TextField(newLevelName, $"스테이지 {list.Count + 1}", GUILayout.Height(22));
                if (EditorUI.Button("만들기", EditorUI.ButtonKind.Primary, GUILayout.Width(72), GUILayout.Height(22)))
                {
                    string levelName = string.IsNullOrWhiteSpace(newLevelName) ? $"스테이지 {list.Count + 1}" : newLevelName.Trim();
                    LevelEditorTools.CreateLevel(levelName, addToList: true);
                    newLevelName = "";
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.Label("바닥, 시작 위치, 치즈가 놓인 상태로 만들어지고 목록 맨 뒤에 추가돼요.", EditorUI.Caption);
        });
    }

    private void DrawLevelCard(LevelRoot lv, int index, int count)
    {
        using (new EditorGUILayout.HorizontalScope(EditorUI.Card))
        {
            GUILayout.Label(index >= 0 ? (index + 1).ToString() : "–", EditorUI.Title, GUILayout.Width(26), GUILayout.Height(34));
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label(lv != null ? lv.DisplayName : "(비어 있음)", EditorUI.Section);
                if (lv != null)
                {
                    GUILayout.Label($"{lv.name} · 제한 {lv.TimeLimit}초 · 리플레이 {lv.MaxReplayCount}회", EditorUI.Caption);
                }
            }
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (index >= 0)
                    {
                        using (new EditorGUI.DisabledScope(index == 0))
                        {
                            if (GUILayout.Button(new GUIContent("▲", "앞으로"), EditorStyles.miniButtonLeft, GUILayout.Width(24), GUILayout.Height(22)))
                            {
                                LevelEditorTools.MoveInList(index, -1);
                                GUIUtility.ExitGUI();
                            }
                        }
                        using (new EditorGUI.DisabledScope(index == count - 1))
                        {
                            if (GUILayout.Button(new GUIContent("▼", "뒤로"), EditorStyles.miniButtonRight, GUILayout.Width(24), GUILayout.Height(22)))
                            {
                                LevelEditorTools.MoveInList(index, 1);
                                GUIUtility.ExitGUI();
                            }
                        }
                        GUILayout.Space(4);
                    }
                    else if (lv != null && GUILayout.Button("목록에 추가", GUILayout.Width(80), GUILayout.Height(22)))
                    {
                        LevelEditorTools.AddToList(lv);
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledScope(lv == null))
                    {
                        if (EditorUI.Button("열기", EditorUI.ButtonKind.Primary, GUILayout.Width(52), GUILayout.Height(22)))
                        {
                            LevelEditorTools.OpenLevel(lv);
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorUI.MenuButton(menu => BuildLevelMenu(menu, lv, index), GUILayout.Width(26), GUILayout.Height(22));
                }
            }
        }
    }

    // 목록에서 빼기, 삭제처럼 자주 쓰지 않고 되돌리기 어려운 동작은 메뉴 안에 둔다.
    private void BuildLevelMenu(GenericMenu menu, LevelRoot lv, int index)
    {
        if (index >= 0)
        {
            menu.AddItem(new GUIContent("게임 목록에서 빼기"), false, () =>
            {
                string name = lv != null ? lv.DisplayName : $"{index + 1}번";
                if (EditorUtility.DisplayDialog("게임 목록에서 빼기",
                        $"'{name}' 레벨을 게임 목록에서 뺄까요?\n\n스테이지 선택 화면에서만 사라지고 파일은 남아 있어요. 나중에 다시 추가할 수 있어요.",
                        "빼기", "취소"))
                {
                    LevelEditorTools.RemoveFromList(index);
                    Repaint();
                }
            });
        }
        else if (lv != null)
        {
            menu.AddItem(new GUIContent("게임 목록에 추가"), false, () =>
            {
                LevelEditorTools.AddToList(lv);
                Repaint();
            });
        }

        if (lv == null)
        {
            return;
        }
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("레벨 삭제…"), false, () =>
        {
            if (EditorUtility.DisplayDialog("레벨 삭제",
                    $"'{lv.DisplayName}' 레벨을 삭제할까요?\n\n게임 목록에서 빠지고 파일({lv.name})은 휴지통으로 옮겨져요. 실수로 지웠다면 휴지통이나 Git에서 되살릴 수 있어요.",
                    "삭제", "취소"))
            {
                LevelEditorTools.DeleteLevel(lv);
                Repaint();
            }
        });
    }

    // 스테이지는 앞 스테이지를 깨야 열리므로, 새 레벨을 게임에서 바로 확인할 수 있게 진행도를 바꾸는 곳.
    private void DrawTestProgress(LevelList list)
    {
        int unlocked = Mathf.Min(PlayerPrefs.GetInt(UnlockedKey, 1), list.Count);
        progressOpen = EditorGUILayout.Foldout(progressOpen, $"테스트용 진행도 · 스테이지 1~{unlocked} 열림", true);
        if (!progressOpen)
        {
            return;
        }

        using (EditorUI.CardScope())
        {
            GUILayout.Label("스테이지는 앞 스테이지를 클리어해야 열려요. 새 레벨을 게임에서 바로 해 보려면 모든 스테이지를 열어 두세요. 이 컴퓨터에만 저장돼요.", EditorUI.Caption);
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("모든 스테이지 열기", GUILayout.Height(22)))
                {
                    PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(1, list.Count));
                    PlayerPrefs.Save();
                    ShowNotification(new GUIContent("모든 스테이지를 열었어요"));
                }
                if (GUILayout.Button("처음 상태로", GUILayout.Height(22)))
                {
                    PlayerPrefs.SetInt(UnlockedKey, 1);
                    PlayerPrefs.Save();
                    ShowNotification(new GUIContent("1스테이지만 열린 상태로 되돌렸어요"));
                }
            }
        }
    }

    // ---------------------------------------------------------------- 편집 화면: 위쪽

    // 창 전체에 해당하는 동작: 목록으로, 저장, 테스트 플레이
    private void DrawToolbar(LevelRoot level)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool dirty = stage != null && stage.scene.isDirty;
        int index = CurrentListIndex();

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button(new GUIContent("‹ 목록", "레벨 목록으로 돌아가요"), EditorStyles.toolbarButton, GUILayout.Width(52)))
            {
                SetMode(Mode.Select);
                StageUtility.GoToMainStage();
                GUIUtility.ExitGUI();
            }
            GUILayout.Label(index >= 0 ? $"{index + 1}. {level.DisplayName}" : level.DisplayName, EditorStyles.boldLabel, GUILayout.MinWidth(40));
            GUILayout.FlexibleSpace();

            if (dirty)
            {
                GUILayout.Label(new GUIContent("저장 안 됨", "바뀐 내용이 아직 저장되지 않았어요"), EditorStyles.miniLabel);
            }
            if (EditorUI.ToolbarButton(EditorUI.Icon("SaveActive", "저장", "Ctrl+S"), dirty ? EditorUI.ButtonKind.Primary : EditorUI.ButtonKind.Normal, 64))
            {
                report = LevelEditorTools.SaveCurrentLevel();
                reportDirty = false;
                ShowNotification(new GUIContent(report.HasErrors ? "저장했어요. 오류를 확인해 주세요" : "저장했어요"));
                if (report.HasErrors)
                {
                    SetMode(Mode.Check);
                }
            }
            if (EditorUI.ToolbarButton(EditorUI.Icon("PlayButton", "테스트 플레이"), EditorUI.ButtonKind.Play, 104))
            {
                RequestTestPlay();
            }
        }
    }

    private int CurrentListIndex()
    {
        LevelList list = LevelEditorTools.LevelList;
        var asset = AssetDatabase.LoadAssetAtPath<LevelRoot>(LevelEditorTools.CurrentLevelPath);
        return list != null && asset != null ? list.IndexOf(asset) : -1;
    }

    // 레벨 하나에 대한 정보를 한곳에: 게임 목록 여부, 검사 결과, 레벨 설정
    private void DrawLevelBar(LevelRoot level)
    {
        EditorGUILayout.Space(4);
        DrawNotInListNotice();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(4);
            DrawStatus();
            GUILayout.FlexibleSpace();
            string zoom = level.Zoom == LevelRoot.CameraZoom.Wide ? "넓게" : "보통";
            var settingsLabel = new GUIContent($"레벨 설정 {(settingsOpen ? "▴" : "▾")}",
                $"제한 시간 {level.TimeLimit}초 · 리플레이 {level.MaxReplayCount}회 · 카메라 {zoom}");
            settingsOpen = GUILayout.Toggle(settingsOpen, settingsLabel, EditorStyles.miniButton, GUILayout.Width(80));
        }

        if (settingsOpen)
        {
            using (EditorUI.CardScope())
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
        EditorGUILayout.Space(2);
    }

    // 게임 목록에 없는 레벨은 스테이지 선택 화면에 나오지 않으므로 알려주고 바로 추가할 수 있게 한다.
    private void DrawNotInListNotice()
    {
        LevelList list = LevelEditorTools.LevelList;
        var asset = AssetDatabase.LoadAssetAtPath<LevelRoot>(LevelEditorTools.CurrentLevelPath);
        if (list == null || asset == null || list.IndexOf(asset) >= 0)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope(EditorUI.Card))
        {
            GUILayout.Label(EditorUI.Icon("console.warnicon.sml", ""), GUILayout.Width(20));
            GUILayout.Label("게임 목록에 없어서 스테이지 선택 화면에 나오지 않아요.", EditorUI.Caption);
            if (GUILayout.Button("게임 목록에 추가", GUILayout.Width(104), GUILayout.Height(20)))
            {
                LevelEditorTools.AddToList(asset);
                ShowNotification(new GUIContent($"{list.Count}번 스테이지로 추가했어요"));
            }
        }
    }

    private void DrawStatus()
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

        GUIContent content;
        if (report == null)
        {
            content = new GUIContent("검사 중");
        }
        else if (errors > 0)
        {
            content = EditorUI.Icon("console.erroricon.sml", warnings > 0 ? $"오류 {errors} · 경고 {warnings}" : $"오류 {errors}");
        }
        else if (warnings > 0)
        {
            content = EditorUI.Icon("console.warnicon.sml", $"경고 {warnings}");
        }
        else
        {
            content = EditorUI.Icon("TestPassed", "문제 없음");
        }
        content.tooltip = "눌러서 검사 결과를 봐요";

        if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.Height(20)))
        {
            SetMode(Mode.Check);
        }
    }

    private void RequestTestPlay()
    {
        report = LevelEditorTools.Validate();
        reportDirty = false;
        if (report.HasErrors)
        {
            SetMode(Mode.Check);
            ShowNotification(new GUIContent("오류를 먼저 고쳐 주세요"));
            return;
        }

        SetMode(Mode.Select);
        LevelEditorTools.TestPlay();
        GUIUtility.ExitGUI();
    }

    private void DrawModeTabs()
    {
        var tabs = new[]
        {
            EditorUI.Icon("MoveTool", "선택", "프롭을 골라 옮기고 설정을 바꿔요"),
            EditorUI.Icon("Grid.PaintTool", "타일", "땅과 장식 타일을 칠해요"),
            EditorUI.Icon("Prefab Icon", "프롭", "물체, 물, 로프, 투명벽을 놓아요"),
            EditorUI.Icon("Favorite", "시작·치즈", "시작 위치와 치즈를 옮겨요"),
            EditorUI.Icon("console.infoicon.sml", "검사", "레벨에 문제가 없는지 확인해요")
        };
        Mode newMode = (Mode)GUILayout.Toolbar((int)mode, tabs, GUILayout.Height(28));
        if (newMode != mode)
        {
            SetMode(newMode);
        }
        EditorGUILayout.Space(2);
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

    // 바닥 줄 안내. 씬 화면 위쪽 안내와 같은 글을 쓴다.
    private string CurrentHint()
    {
        switch (mode)
        {
            case Mode.Tiles:
                if (eraser) return "씬 화면을 클릭하거나 끌어서 지워요 · Esc 끝내기";
                return palette.HasStamp
                    ? "씬 화면을 클릭하거나 끌어서 칠해요 · Shift 누른 채 지우기 · Esc 끝내기"
                    : "팔레트에서 칠할 타일을 골라 주세요.";
            case Mode.Props:
                return selectedProp != null
                    ? "씬 화면을 클릭해서 놓아요 · Esc 끝내기"
                    : "놓을 프롭을 골라 주세요.";
            case Mode.Points:
                return relocate != Relocate.None
                    ? "씬 화면을 클릭하거나 끌어서 옮겨요 · Esc 끝내기"
                    : "'클릭해서 옮기기'를 누르거나 씬 화면의 원을 끌어서 옮겨요.";
            case Mode.Check:
                return "빨간 오류가 있으면 테스트 플레이를 할 수 없어요. 노란 경고는 확인만 하면 돼요.";
            default:
                return "씬 화면에서 프롭을 클릭해서 고르고 끌어서 옮겨요 · W 이동 · E 회전";
        }
    }

    // 격자와 놓는 간격. 놓는 도구(타일, 프롭, 시작·치즈)에서 같은 모양으로 쓴다.
    private void DrawPlacementOptions(bool withSnap, string note = null)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            bool grid = GUILayout.Toggle(showGrid, EditorUI.Icon("Grid.Default", "격자", "씬 화면에 타일 칸을 보여줘요"), EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(20));
            if (grid != showGrid)
            {
                showGrid = grid;
                SceneView.RepaintAll();
            }
            if (withSnap)
            {
                GUILayout.Space(8);
                GUILayout.Label("놓는 간격", EditorStyles.miniLabel, GUILayout.Width(50));
                snapIndex = GUILayout.Toolbar(snapIndex, SnapLabels, EditorStyles.miniButton, GUILayout.Height(20));
            }
            else
            {
                GUILayout.FlexibleSpace();
            }
        }
        if (!string.IsNullOrEmpty(note))
        {
            GUILayout.Label(note, EditorUI.Caption);
        }
    }

    // ---------------------------------------------------------------- 선택

    private void DrawSelectMode(LevelRoot level)
    {
        GameObject go = Selection.activeGameObject;
        LevelProp prop = go != null ? go.GetComponentInParent<LevelProp>() : null;
        if (prop != null && prop.transform.IsChildOf(level.transform))
        {
            DrawSelectedProp(prop);
        }
        else
        {
            using (EditorUI.CardScope())
            {
                GUILayout.Label("씬 화면에서 프롭을 클릭하면 여기에서 설정을 바꿀 수 있어요.", EditorUI.Caption);
            }
        }

        var props = new List<LevelProp>(level.GetComponentsInChildren<LevelProp>(true));
        props.Sort((a, b) => a.Category != b.Category ? a.Category.CompareTo(b.Category) : string.CompareOrdinal(a.name, b.name));
        EditorUI.Header($"이 레벨의 프롭 {props.Count}개", "이름을 누르면 씬 화면에서 그 프롭으로 이동해요.");
        if (props.Count == 0)
        {
            GUILayout.Label("아직 놓은 프롭이 없어요. '프롭' 탭에서 골라 놓아 보세요.", EditorUI.Caption);
            return;
        }

        using (EditorUI.CardScope())
        {
            PropCategory? current = null;
            foreach (LevelProp p in props)
            {
                if (current != p.Category)
                {
                    if (current != null)
                    {
                        EditorGUILayout.Space(3);
                    }
                    current = p.Category;
                    GUILayout.Label(CategoryLabel(p.Category), EditorStyles.miniBoldLabel);
                }
                bool isSelected = p == prop;
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = isSelected ? EditorUI.SelectedColor : old;
                if (GUILayout.Button(p.name, EditorStyles.miniButton))
                {
                    SelectAndFrame(p.gameObject);
                }
                GUI.backgroundColor = old;
            }
        }
    }

    private void DrawSelectedProp(LevelProp prop)
    {
        using (EditorUI.CardScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(prop.name, EditorUI.Title);
                GUILayout.FlexibleSpace();
                GUILayout.Label(CategoryLabel(prop.Category), EditorUI.Badge);
            }

            if (PlannerFieldUtility.HasFields(prop.GetType(), PlannerFieldUtility.Scope.Shared))
            {
                DrawSharedSettings(prop);
            }

            DrawInstanceSettings(prop);

            var r = new PropValidationReport();
            prop.Validate(r);
            foreach (PropValidationReport.Entry e in r.Entries)
            {
                EditorGUILayout.HelpBox(e.message, e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            bool unique = prop is SpawnPoint || prop is GoalProp;
            if (unique)
            {
                GUILayout.Label("시작 위치와 치즈는 지울 수 없어요. '시작·치즈' 탭에서 옮길 수 있어요.", EditorUI.Caption);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (EditorUI.Button(EditorUI.Icon("TreeEditor.Trash", "프롭 지우기"), EditorUI.ButtonKind.Danger, GUILayout.Width(110)))
                    {
                        Undo.DestroyObjectImmediate(prop.gameObject);
                        MarkReportDirty();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }

    // 공통 설정: 프리팹 원본을 고치므로 같은 프롭 모두(다른 레벨에 놓인 것 포함)에 적용된다.
    // 선택 탭과 프롭 탭이 같은 모양으로 쓴다.
    private void DrawSharedSettings(Component instanceOrPrefab)
    {
        bool shared = PlannerFieldUtility.IsEditingType(instanceOrPrefab) || EditorUtility.IsPersistent(instanceOrPrefab);
        EditorUI.Header(shared ? "공통 설정" : "설정",
            shared ? "같은 프롭 모두에 적용돼요. 다른 레벨에 놓인 것도 함께 바뀌어요." : null);

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
            EditorGUILayout.HelpBox($"이 프롭만 공통 설정과 값이 달라요: {string.Join(", ", overrides)}", MessageType.Warning);
            if (GUILayout.Button("공통 설정으로 되돌리기"))
            {
                PlannerFieldUtility.RevertSharedOverrides(instanceOrPrefab);
                MarkReportDirty();
            }
        }
    }

    // 이 프롭만: 놓은 자리마다 다른 값 (칸 수, 마디 수, 위치, 각도)
    private void DrawInstanceSettings(LevelProp prop)
    {
        EditorUI.Header("이 프롭만");

        var so = new SerializedObject(prop);
        so.Update();
        if (PlannerFieldUtility.HasFields(prop.GetType(), PlannerFieldUtility.Scope.PerInstance)
            && PlannerFieldUtility.DrawPlannerFields(so, PlannerFieldUtility.Scope.PerInstance))
        {
            MarkReportDirty();
        }
        so.ApplyModifiedProperties();

        DrawPositionFields(prop.transform);
        if (prop is IAreaProp)
        {
            GUILayout.Label("씬 화면의 손잡이로도 크기를 바꿀 수 있어요. 오른쪽 점은 가로, 위쪽 점은 세로, 오른쪽 위 네모는 둘 다예요.", EditorUI.Caption);
        }
        else if (!(prop is SpawnPoint))
        {
            DrawAngleFields(prop.transform);
        }
    }

    private static void DrawPositionFields(Transform t)
    {
        EditorGUI.BeginChangeCheck();
        Vector2 pos = EditorGUILayout.Vector2Field("위치", new Vector2(t.position.x, t.position.y));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(t, "위치 바꾸기");
            t.position = new Vector3(pos.x, pos.y, t.position.z);
        }
    }

    private static void DrawAngleFields(Transform t)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("각도");
            EditorGUI.BeginChangeCheck();
            float angle = EditorGUILayout.FloatField(Mathf.Round(t.localEulerAngles.z * 10f) / 10f, GUILayout.Width(48));
            if (EditorGUI.EndChangeCheck())
            {
                SetAngle(t, angle);
            }
            float[] steps = { -90f, -15f, 15f, 90f };
            for (int i = 0; i < steps.Length; i++)
            {
                GUIStyle style = i == 0 ? EditorStyles.miniButtonLeft : i == steps.Length - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
                if (GUILayout.Button(steps[i] > 0 ? $"+{steps[i]}" : steps[i].ToString(), style))
                {
                    SetAngle(t, t.localEulerAngles.z + steps[i]);
                }
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("방향");
            if (GUILayout.Button("좌우 뒤집기", EditorStyles.miniButton))
            {
                Flip(t);
            }
        }
    }

    private static void Flip(Transform t)
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

    private static void SetAngle(Transform t, float angle)
    {
        Undo.RecordObject(t, "각도 바꾸기");
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
        // 무엇을, 어디에 칠할지 (도구와 칠할 곳)
        using (EditorUI.CardScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("칠할 곳", EditorUI.Section, GUILayout.Width(52));
                tileLayer = (TileLayer)GUILayout.Toolbar((int)tileLayer, TileLayerLabels, GUILayout.Height(22));
            }
            GUILayout.Label(TileLayerHelp[(int)tileLayer], EditorUI.Caption);
            if (FindTargetTilemap(level) == null)
            {
                EditorGUILayout.HelpBox($"이 레벨에 '{TileLayerLabels[(int)tileLayer]}' 타일맵이 없어요.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("도구", EditorUI.Section, GUILayout.Width(52));
                var tools = new[] { EditorUI.Icon("Grid.PaintTool", "칠하기"), EditorUI.Icon("Grid.EraserTool", "지우개") };
                eraser = GUILayout.Toolbar(eraser ? 1 : 0, tools, GUILayout.Height(22)) == 1;
            }
            EditorGUILayout.Space(2);
            DrawPlacementOptions(withSnap: false);
        }

        // 팔레트와 새 타일 이미지 추가는 같은 곳에 둔다.
        if (EditorUI.HeaderWithButton("팔레트", EditorUI.Icon("Toolbar Plus", "타일 이미지 추가", "PNG 이미지를 칸 크기대로 잘라 타일로 만들어요"), 120))
        {
            ShowAddTileMenu();
        }
        using (new EditorGUI.DisabledScope(eraser))
        {
            float height = Mathf.Clamp(position.height - 470f, 160f, 640f);
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
    }

    private void ShowAddTileMenu()
    {
        var menu = new GenericMenu();
        foreach (int size in TileCellSizes)
        {
            int cell = size;
            string label = cell == TileSheetImporter.DefaultCellSize ? $"한 칸이 {cell}픽셀인 이미지 (기본)" : $"한 칸이 {cell}픽셀인 이미지";
            menu.AddItem(new GUIContent(label), false, () => ImportTiles(cell));
        }
        menu.ShowAsContext();
    }

    private void ImportTiles(int cellSize)
    {
        string path = EditorUtility.OpenFilePanel("타일 이미지 고르기 (PNG)", "", "png");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        int count = TileSheetImporter.ImportFromDisk(path, cellSize);
        palette.Refresh();
        ShowNotification(new GUIContent($"타일 {count}개를 추가했어요"));
        Repaint();
    }

    // ---------------------------------------------------------------- 프롭

    private void DrawPropsMode()
    {
        PropCatalog catalog = LevelEditorTools.Catalog;
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("PropCatalog 에셋을 찾을 수 없어요.", MessageType.Error);
            return;
        }

        // 찾기와 새로 만들기를 한 줄에
        using (new EditorGUILayout.HorizontalScope())
        {
            propSearch = GUILayout.TextField(propSearch, EditorStyles.toolbarSearchField, GUILayout.Height(20));
            if (EditorUI.Button(EditorUI.Icon("Toolbar Plus", "새 프롭", "그림으로 새 프롭을 만들어요"), EditorUI.ButtonKind.Normal, GUILayout.Width(76), GUILayout.Height(20)))
            {
                NewPropWindow.Open(SelectCatalogEntry);
            }
        }
        int chipColumns = Mathf.Max(2, Mathf.FloorToInt((position.width - 20f) / 60f));
        var labels = new string[PropFilters.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = PropFilters[i].label;
        }
        propFilterIndex = GUILayout.SelectionGrid(propFilterIndex, labels, chipColumns, EditorStyles.miniButton);
        EditorGUILayout.Space(2);
        DrawPlacementOptions(withSnap: true, note: "물과 투명벽은 항상 1칸 단위로 놓여요.");

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

        EditorGUILayout.Space(4);
        if (entries.Count == 0)
        {
            GUILayout.Label("조건에 맞는 프롭이 없어요.", EditorUI.Caption);
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

        if (selectedProp == null)
        {
            return;
        }

        EditorGUILayout.Space(6);
        using (EditorUI.CardScope())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(selectedProp.displayName, EditorUI.Title);
                GUILayout.FlexibleSpace();
                GUILayout.Label(CategoryLabel(selectedProp.Category), EditorUI.Badge);
            }
            if (!string.IsNullOrEmpty(selectedProp.description))
            {
                GUILayout.Label(selectedProp.description, EditorUI.Caption);
            }
            LevelProp typeProp = selectedProp.Prop;
            if (typeProp != null && PlannerFieldUtility.HasFields(typeProp.GetType(), PlannerFieldUtility.Scope.Shared))
            {
                DrawSharedSettings(typeProp);
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
        ShowNotification(new GUIContent("프롭을 만들었어요. 씬 화면을 클릭해서 놓아 보세요"));
        Repaint();
    }

    private void DrawPropCell(PropCatalog.Entry e, float cell)
    {
        Rect r = GUILayoutUtility.GetRect(cell, cell + 16f, GUILayout.Width(cell), GUILayout.Height(cell + 16f));
        bool selected = selectedProp != null && selectedProp.id == e.id;
        bool hover = r.Contains(Event.current.mousePosition);
        Color bg = selected ? new Color(0.25f, 0.55f, 1f, 0.55f) : hover ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.15f);
        EditorGUI.DrawRect(r, bg);
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
        DrawPlacementOptions(withSnap: true);
        EditorGUILayout.Space(2);
        DrawPointCard("시작 위치", level.SpawnPoint != null ? level.SpawnPoint.transform : null, Relocate.Spawn,
            "쥐가 처음 나타나는 곳이에요. 아래로 10칸 안에 땅이 있어야 해요.",
            "레벨에 시작 위치가 없어요.");
        GoalProp goal = level.GetComponentInChildren<GoalProp>(true);
        DrawPointCard("치즈", goal != null ? goal.transform : null, Relocate.Goal,
            "쥐가 치즈를 먹으면 클리어예요.",
            "레벨에 치즈가 없어요.");
    }

    private void DrawPointCard(string title, Transform target, Relocate which, string description, string missing)
    {
        using (EditorUI.CardScope())
        {
            GUILayout.Label(title, EditorUI.Section);
            GUILayout.Label(description, EditorUI.Caption);
            if (target == null)
            {
                EditorGUILayout.HelpBox(missing, MessageType.Error);
                return;
            }

            DrawPositionFields(target);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool active = relocate == which;
                if (EditorUI.Button(active ? "옮기는 중 · Esc로 끝내기" : "클릭해서 옮기기",
                        active ? EditorUI.ButtonKind.Primary : EditorUI.ButtonKind.Normal, GUILayout.Height(22)))
                {
                    relocate = active ? Relocate.None : which;
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button(new GUIContent("찾기", "씬 화면에서 보여줘요"), GUILayout.Width(48), GUILayout.Height(22)))
                {
                    SelectAndFrame(target.gameObject);
                }
            }
        }
    }

    // ---------------------------------------------------------------- 검사

    private void DrawCheckMode()
    {
        if (report == null)
        {
            report = LevelEditorTools.Validate();
            reportDirty = false;
        }

        if (EditorUI.HeaderWithButton("검사 결과", EditorUI.Icon("Refresh", "다시 검사"), 84))
        {
            report = LevelEditorTools.Validate();
            reportDirty = false;
        }

        if (report.Entries.Count == 0)
        {
            using (EditorUI.CardScope())
            {
                GUILayout.Label(EditorUI.Icon("TestPassed", "문제가 없어요. 저장하고 테스트 플레이해 보세요."), EditorStyles.wordWrappedLabel);
            }
            return;
        }

        foreach (PropValidationReport.Entry e in report.Entries)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(e.message, e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
                using (new EditorGUI.DisabledScope(e.context == null))
                {
                    if (GUILayout.Button(new GUIContent("찾기", "씬 화면에서 보여줘요"), GUILayout.Width(44), GUILayout.Height(38)))
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
