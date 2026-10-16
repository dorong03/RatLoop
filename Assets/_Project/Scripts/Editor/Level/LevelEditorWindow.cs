using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// 기획자용 레벨 에디터. 메뉴: RatLoop > 레벨 에디터 (Ctrl+Shift+L)
// 하이어라키/인스펙터를 몰라도 이 창 하나로 레벨을 만들고, 타일을 칠하고, 프롭을 배치하고, 테스트할 수 있다.
public class LevelEditorWindow : EditorWindow
{
    private enum Tab { Tiles, Props, Selection, Check }
    private enum TileLayer { Ground, DecorationBack, DecorationFront }
    private enum Tool { None, PaintTile, EraseTile, PlaceProp }

    private static readonly string[] TabLabels = { "타일", "프롭", "선택한 것", "세이프티 체크" };
    private static readonly string[] TileLayerLabels = { "지형 (밟고 부딪힘)", "뒤 장식 (통과)", "앞 장식 (통과)" };
    private static readonly string[] TileLayerObjectNames = { "Ground", "Decoration", "DecorationFront" };
    private static readonly string[] SnapLabels = { "0.25칸", "0.5칸", "1칸" };
    private static readonly float[] SnapValues = { 0.25f, 0.5f, 1f };

    private static readonly (PropCategory? category, string label)[] PropFilters =
    {
        (null, "전체"), (PropCategory.Physics, "물리"), (PropCategory.Static, "고정"), (PropCategory.Decoration, "장식"),
        (PropCategory.Rope, "로프"), (PropCategory.Water, "물"), (PropCategory.Gimmick, "기믹")
    };

    private const string TilesRoot = "Assets/_Project/Art/Tiles";
    private const float IconSize = 40f;

    private Tab tab;
    private Tool tool;
    private TileLayer tileLayer;
    private TileBase selectedTile;
    private int tileGroupIndex;
    private PropCatalog.Entry selectedProp;
    private int propFilterIndex;
    private int snapIndex = 1;
    private string newLevelName = "";
    private Vector2 scroll;
    private PropValidationReport lastReport;

    private List<string> tileGroupNames;
    private Dictionary<string, List<TileBase>> tileGroups;

    [MenuItem("RatLoop/레벨 에디터 %#l", priority = 0)]
    public static void Open()
    {
        var window = GetWindow<LevelEditorWindow>("레벨 에디터");
        window.minSize = new Vector2(320f, 420f);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += Repaint;
        EditorApplication.hierarchyChanged += Repaint;
        PrefabStage.prefabStageOpened += OnStageChanged;
        PrefabStage.prefabStageClosing += OnStageChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= Repaint;
        EditorApplication.hierarchyChanged -= Repaint;
        PrefabStage.prefabStageOpened -= OnStageChanged;
        PrefabStage.prefabStageClosing -= OnStageChanged;
    }

    private void OnStageChanged(PrefabStage stage)
    {
        tool = Tool.None;
        lastReport = null;
        Repaint();
    }

    // ================================================================ 창

    private void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("테스트 플레이 중입니다. 플레이를 멈추면 레벨로 돌아옵니다.", MessageType.Info);
            return;
        }

        LevelRoot level = LevelEditorTools.CurrentLevel;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (level == null)
        {
            DrawLevelBrowser();
        }
        else
        {
            DrawEditingHeader(level);
            DrawLevelSettings(level);
            EditorGUILayout.Space(6);
            Tab newTab = (Tab)GUILayout.Toolbar((int)tab, TabLabels, GUILayout.Height(24));
            if (newTab != tab)
            {
                tab = newTab;
                if (tab == Tab.Check)
                {
                    lastReport = LevelEditorTools.Validate();
                }
            }
            EditorGUILayout.Space(4);

            switch (tab)
            {
                case Tab.Tiles: DrawTilesTab(level); break;
                case Tab.Props: DrawPropsTab(); break;
                case Tab.Selection: DrawSelectionTab(level); break;
                case Tab.Check: DrawCheckTab(); break;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    // ---------------------------------------------------------------- 레벨 목록 화면

    private void DrawLevelBrowser()
    {
        EditorGUILayout.LabelField("레벨 목록 (게임에 나오는 순서)", EditorStyles.boldLabel);
        LevelList list = LevelEditorTools.LevelList;
        if (list == null)
        {
            EditorGUILayout.HelpBox("LevelList 에셋을 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        var inList = new HashSet<LevelRoot>();
        for (int i = 0; i < list.Count; i++)
        {
            LevelRoot lv = list.Get(i);
            inList.Add(lv);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"{i + 1}.", GUILayout.Width(24));
                GUILayout.Label(lv != null ? $"{lv.DisplayName}  ({lv.name})" : "(비어 있음)");
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(24))) { LevelEditorTools.MoveInList(i, -1); GUIUtility.ExitGUI(); }
                }
                using (new EditorGUI.DisabledScope(i == list.Count - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(24))) { LevelEditorTools.MoveInList(i, 1); GUIUtility.ExitGUI(); }
                }
                using (new EditorGUI.DisabledScope(lv == null))
                {
                    if (GUILayout.Button("열기", GUILayout.Width(44))) { LevelEditorTools.OpenLevel(lv); GUIUtility.ExitGUI(); }
                }
                if (GUILayout.Button("빼기", GUILayout.Width(44)) &&
                    EditorUtility.DisplayDialog("레벨 빼기", $"{i + 1}번 레벨을 목록에서 뺄까요?\n레벨 파일은 지워지지 않습니다.", "빼기", "취소"))
                {
                    LevelEditorTools.RemoveFromList(i);
                    GUIUtility.ExitGUI();
                }
            }
        }

        var notInList = LevelEditorTools.FindAllLevels().FindAll(l => !inList.Contains(l));
        if (notInList.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("목록에 없는 레벨", EditorStyles.boldLabel);
            foreach (LevelRoot lv in notInList)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label($"{lv.DisplayName}  ({lv.name})");
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("목록에 추가", GUILayout.Width(80))) { LevelEditorTools.AddToList(lv); GUIUtility.ExitGUI(); }
                    if (GUILayout.Button("열기", GUILayout.Width(44))) { LevelEditorTools.OpenLevel(lv); GUIUtility.ExitGUI(); }
                }
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("새 레벨 만들기", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            newLevelName = EditorGUILayout.TextField("레벨 이름", newLevelName);
            if (GUILayout.Button("만들기", GUILayout.Width(60)))
            {
                LevelEditorTools.CreateLevel(newLevelName, addToList: true);
                newLevelName = "";
                GUIUtility.ExitGUI();
            }
        }
        EditorGUILayout.HelpBox("새 레벨은 바닥, 시작 위치, 치즈가 들어 있는 템플릿에서 시작하고 목록 맨 뒤에 추가됩니다.", MessageType.None);
    }

    // ---------------------------------------------------------------- 편집 화면

    private void DrawEditingHeader(LevelRoot level)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool dirty = stage != null && stage.scene.isDirty;
        LevelList list = LevelEditorTools.LevelList;
        int index = list != null ? list.IndexOf(AssetDatabase.LoadAssetAtPath<LevelRoot>(LevelEditorTools.CurrentLevelPath)) : -1;
        string order = index >= 0 ? $"{index + 1}번 스테이지" : "목록에 없음";

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label($"편집 중: {level.DisplayName}  ({order}){(dirty ? "  *저장 안 됨" : "")}", EditorStyles.boldLabel);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("저장", GUILayout.Height(26)))
            {
                lastReport = LevelEditorTools.SaveCurrentLevel();
                ShowNotification(new GUIContent(lastReport.HasErrors ? "저장했지만 오류가 있습니다" : "저장했습니다"));
                if (lastReport.HasErrors)
                {
                    tab = Tab.Check;
                }
            }
            if (GUILayout.Button("테스트 플레이", GUILayout.Height(26)))
            {
                var report = LevelEditorTools.Validate();
                if (report.HasErrors)
                {
                    lastReport = report;
                    tab = Tab.Check;
                    ShowNotification(new GUIContent("오류를 먼저 고쳐주세요"));
                }
                else
                {
                    tool = Tool.None;
                    LevelEditorTools.TestPlay();
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("화면 맞추기", GUILayout.Height(26)))
            {
                LevelEditorTools.FrameLevel();
            }
            if (GUILayout.Button("레벨 목록", GUILayout.Height(26)))
            {
                tool = Tool.None;
                StageUtility.GoToMainStage();
                GUIUtility.ExitGUI();
            }
        }
    }

    private void DrawLevelSettings(LevelRoot level)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("레벨 설정", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            var so = new SerializedObject(level);
            so.Update();
            PlannerFieldUtility.DrawPlannerFields(so);
            so.ApplyModifiedProperties();
        }
    }

    // ---------------------------------------------------------------- 타일

    private void DrawTilesTab(LevelRoot level)
    {
        EnsureTileGroups();
        tileLayer = (TileLayer)GUILayout.SelectionGrid((int)tileLayer, TileLayerLabels, 3);
        if (FindTargetTilemap(level) == null)
        {
            EditorGUILayout.HelpBox($"이 레벨에 '{TileLayerObjectNames[(int)tileLayer]}' 타일맵이 없습니다.", MessageType.Warning);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawToolToggle(Tool.PaintTile, "칠하기", selectedTile != null);
            DrawToolToggle(Tool.EraseTile, "지우기", true);
            if (GUILayout.Button("멈추기")) { tool = Tool.None; }
        }
        EditorGUILayout.HelpBox("씬 화면에서 왼쪽 버튼으로 칠하고, Shift 를 누른 채 칠하면 지웁니다. Esc 로 멈춥니다.", MessageType.None);

        if (tileGroupNames.Count == 0)
        {
            EditorGUILayout.HelpBox("타일이 없습니다: " + TilesRoot, MessageType.Warning);
            return;
        }
        tileGroupIndex = EditorGUILayout.Popup("타일 묶음", Mathf.Clamp(tileGroupIndex, 0, tileGroupNames.Count - 1), tileGroupNames.ToArray());
        List<TileBase> tiles = tileGroups[tileGroupNames[tileGroupIndex]];

        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 30f) / (IconSize + 4f)));
        for (int i = 0; i < tiles.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = i; j < Mathf.Min(i + columns, tiles.Count); j++)
                {
                    TileBase tile = tiles[j];
                    Rect r = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
                    bool selected = tile == selectedTile;
                    if (selected)
                    {
                        EditorGUI.DrawRect(r, new Color(0.25f, 0.55f, 1f, 0.6f));
                    }
                    DrawSprite(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), (tile as Tile)?.sprite);
                    if (GUI.Button(r, new GUIContent("", tile.name), GUIStyle.none))
                    {
                        selectedTile = tile;
                        tool = Tool.PaintTile;
                        FocusSceneView();
                    }
                }
            }
        }
    }

    private void EnsureTileGroups()
    {
        if (tileGroups != null)
        {
            return;
        }

        tileGroups = new Dictionary<string, List<TileBase>>();
        foreach (string guid in AssetDatabase.FindAssets("t:TileBase", new[] { TilesRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
            {
                continue;
            }
            string group = Path.GetFileName(Path.GetDirectoryName(path));
            if (!tileGroups.TryGetValue(group, out var list))
            {
                tileGroups[group] = list = new List<TileBase>();
            }
            list.Add(tile);
        }
        foreach (var list in tileGroups.Values)
        {
            list.Sort((a, b) => TileNumber(a).CompareTo(TileNumber(b)));
        }
        tileGroupNames = new List<string>(tileGroups.Keys);
        tileGroupNames.Sort();
    }

    private static int TileNumber(TileBase tile)
    {
        int underscore = tile.name.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(tile.name.Substring(underscore + 1), out int n) ? n : int.MaxValue;
    }

    private Tilemap FindTargetTilemap(LevelRoot level)
    {
        string objectName = TileLayerObjectNames[(int)tileLayer];
        foreach (Tilemap tm in level.GetComponentsInChildren<Tilemap>(true))
        {
            if (tm.name == objectName && tm.GetComponentInParent<WaterVolume>() == null)
            {
                return tm;
            }
        }
        return null;
    }

    // ---------------------------------------------------------------- 프롭

    private void DrawPropsTab()
    {
        PropCatalog catalog = LevelEditorTools.Catalog;
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("PropCatalog 에셋을 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        var labels = new string[PropFilters.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = PropFilters[i].label;
        }
        propFilterIndex = GUILayout.SelectionGrid(propFilterIndex, labels, 4);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawToolToggle(Tool.PlaceProp, "배치하기", selectedProp != null);
            if (GUILayout.Button("멈추기")) { tool = Tool.None; }
            GUILayout.Label("칸 맞춤", GUILayout.Width(44));
            snapIndex = EditorGUILayout.Popup(snapIndex, SnapLabels, GUILayout.Width(70));
        }
        EditorGUILayout.HelpBox("프롭을 고른 뒤 씬 화면을 클릭하면 놓입니다. 놓은 프롭은 '선택한 것' 탭에서 값을 바꿉니다. Esc 로 멈춥니다.", MessageType.None);

        PropCategory? filter = PropFilters[propFilterIndex].category;
        var entries = new List<PropCatalog.Entry>();
        foreach (PropCatalog.Entry e in catalog.Entries)
        {
            if (!e.hiddenInPalette && e.prefab != null && (filter == null || e.Category == filter))
            {
                entries.Add(e);
            }
        }

        const float cell = 72f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 30f) / (cell + 4f)));
        for (int i = 0; i < entries.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = i; j < Mathf.Min(i + columns, entries.Count); j++)
                {
                    PropCatalog.Entry e = entries[j];
                    Rect r = GUILayoutUtility.GetRect(cell, cell + 16f, GUILayout.Width(cell), GUILayout.Height(cell + 16f));
                    bool selected = selectedProp != null && selectedProp.id == e.id;
                    EditorGUI.DrawRect(r, selected ? new Color(0.25f, 0.55f, 1f, 0.6f) : new Color(0f, 0f, 0f, 0.15f));
                    Sprite icon = e.GetIcon();
                    Rect iconRect = new Rect(r.x + 6, r.y + 4, cell - 12, cell - 12);
                    if (icon != null)
                    {
                        DrawSprite(iconRect, icon);
                    }
                    else
                    {
                        GUI.Label(iconRect, e.Category == PropCategory.Water ? "물" : "?", EditorStyles.centeredGreyMiniLabel);
                    }
                    GUI.Label(new Rect(r.x, r.yMax - 18, r.width, 18), e.displayName, EditorStyles.centeredGreyMiniLabel);
                    if (GUI.Button(r, new GUIContent("", e.description), GUIStyle.none))
                    {
                        selectedProp = e;
                        tool = Tool.PlaceProp;
                        FocusSceneView();
                    }
                }
            }
        }

        if (selectedProp != null && !string.IsNullOrEmpty(selectedProp.description))
        {
            EditorGUILayout.HelpBox($"{selectedProp.displayName}: {selectedProp.description}", MessageType.None);
        }
    }

    // ---------------------------------------------------------------- 선택한 것

    private void DrawSelectionTab(LevelRoot level)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("시작 위치 선택"))
            {
                SelectAndFrame(level.SpawnPoint != null ? level.SpawnPoint.gameObject : null);
            }
            if (GUILayout.Button("치즈 선택"))
            {
                GoalProp goal = level.GetComponentInChildren<GoalProp>(true);
                SelectAndFrame(goal != null ? goal.gameObject : null);
            }
        }

        GameObject go = Selection.activeGameObject;
        LevelProp prop = go != null ? go.GetComponentInParent<LevelProp>() : null;
        if (prop == null || !prop.transform.IsChildOf(level.transform))
        {
            EditorGUILayout.HelpBox("씬 화면에서 프롭을 클릭하면 여기서 값을 바꿀 수 있습니다.\n옮길 때는 씬 화면에서 끌어서 옮깁니다. (W 키: 이동 도구)", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(prop.name, EditorStyles.boldLabel);
        var so = new SerializedObject(prop);
        so.Update();
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (PlannerFieldUtility.GetPlannerProperties(so).Count == 0)
            {
                EditorGUILayout.LabelField("조절할 값이 없습니다.", EditorStyles.miniLabel);
            }
            else
            {
                PlannerFieldUtility.DrawPlannerFields(so);
            }
            so.ApplyModifiedProperties();

            if (PrefabUtility.IsPartOfPrefabInstance(prop) && PlannerFieldUtility.HasPrefabOverrides(so))
            {
                if (GUILayout.Button("기본값으로 되돌리기"))
                {
                    PlannerFieldUtility.RevertToPrefabDefaults(so);
                }
            }
        }

        if (!(prop is WaterVolume) && !(prop is SpawnPoint))
        {
            EditorGUILayout.LabelField("각도", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                Transform t = prop.transform;
                EditorGUI.BeginChangeCheck();
                float angle = EditorGUILayout.FloatField(Mathf.Round(t.localEulerAngles.z * 10f) / 10f, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck())
                {
                    SetAngle(t, angle);
                }
                foreach (float step in new[] { -90f, -15f, 15f, 90f })
                {
                    if (GUILayout.Button(step > 0 ? $"+{step}" : step.ToString()))
                    {
                        SetAngle(t, t.localEulerAngles.z + step);
                    }
                }
                if (GUILayout.Button("좌우 뒤집기"))
                {
                    Undo.RecordObject(t, "좌우 뒤집기");
                    Vector3 s = t.localScale;
                    t.localScale = new Vector3(-s.x, s.y, s.z);
                }
            }
        }
        else if (prop is WaterVolume)
        {
            EditorGUILayout.HelpBox("물 크기는 위 칸 수를 바꾸거나, 씬 화면의 파란 손잡이를 끌어서 바꿉니다.", MessageType.None);
        }

        var report = new PropValidationReport();
        prop.Validate(report);
        foreach (PropValidationReport.Entry e in report.Entries)
        {
            EditorGUILayout.HelpBox(e.message, e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
        }

        EditorGUILayout.Space(6);
        bool unique = prop is SpawnPoint || prop is GoalProp;
        using (new EditorGUI.DisabledScope(unique))
        {
            if (GUILayout.Button(unique ? "시작 위치와 치즈는 지울 수 없습니다" : "이 프롭 지우기"))
            {
                Undo.DestroyObjectImmediate(prop.gameObject);
                GUIUtility.ExitGUI();
            }
        }
    }

    private static void SetAngle(Transform t, float angle)
    {
        Undo.RecordObject(t, "각도 변경");
        Vector3 e = t.localEulerAngles;
        t.localEulerAngles = new Vector3(e.x, e.y, Mathf.Repeat(angle, 360f));
    }

    // ---------------------------------------------------------------- 세이프티 체크

    private void DrawCheckTab()
    {
        if (GUILayout.Button("다시 검사", GUILayout.Height(24)))
        {
            lastReport = LevelEditorTools.Validate();
        }

        if (lastReport == null)
        {
            lastReport = LevelEditorTools.Validate();
        }

        if (lastReport.Entries.Count == 0)
        {
            EditorGUILayout.HelpBox("문제가 없습니다.", MessageType.Info);
            return;
        }

        foreach (PropValidationReport.Entry e in lastReport.Entries)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(e.message, e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
                using (new EditorGUI.DisabledScope(e.context == null))
                {
                    if (GUILayout.Button("보기", GUILayout.Width(44), GUILayout.Height(38)))
                    {
                        var c = e.context as Component;
                        SelectAndFrame(c != null ? c.gameObject : e.context as GameObject);
                    }
                }
            }
        }
        EditorGUILayout.HelpBox("빨간 오류가 있으면 테스트 플레이를 할 수 없습니다. 노란 경고는 확인만 하면 됩니다.", MessageType.None);
    }

    // ================================================================ 씬 화면

    private void OnSceneGUI(SceneView view)
    {
        if (tool == Tool.None)
        {
            return;
        }

        LevelRoot level = LevelEditorTools.CurrentLevel;
        if (level == null)
        {
            tool = Tool.None;
            return;
        }

        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlId);
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            tool = Tool.None;
            e.Use();
            Repaint();
            return;
        }

        Vector3 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        world.z = 0f;

        if (tool == Tool.PaintTile || tool == Tool.EraseTile)
        {
            HandleTileTool(level, world, e);
        }
        else if (tool == Tool.PlaceProp)
        {
            HandlePropTool(level, world, e);
        }

        if (e.type == EventType.MouseMove)
        {
            view.Repaint();
        }
    }

    private void HandleTileTool(LevelRoot level, Vector3 world, Event e)
    {
        Tilemap tm = FindTargetTilemap(level);
        if (tm == null)
        {
            return;
        }

        bool erase = tool == Tool.EraseTile || e.shift;
        Vector3Int cell = tm.WorldToCell(world);
        Vector3 min = tm.CellToWorld(cell);
        Vector3 size = tm.layoutGrid.cellSize;
        var rect = new Rect(min.x, min.y, size.x, size.y);
        Handles.DrawSolidRectangleWithOutline(rect,
            erase ? new Color(1f, 0.2f, 0.2f, 0.25f) : new Color(0.2f, 0.6f, 1f, 0.25f),
            erase ? Color.red : Color.cyan);

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
        {
            // 한 번 누르고 끄는 동안 칠한 것이 실행 취소 한 번이 된다.
            if (e.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName(erase ? "타일 지우기" : "타일 칠하기");
            }
            TileBase newTile = erase ? null : selectedTile;
            if (tm.GetTile(cell) != newTile)
            {
                Undo.RecordObject(tm, erase ? "타일 지우기" : "타일 칠하기");
                tm.SetTile(cell, newTile);
                EditorUtility.SetDirty(tm);
            }
            e.Use();
        }
    }

    private void HandlePropTool(LevelRoot level, Vector3 world, Event e)
    {
        if (selectedProp == null || selectedProp.prefab == null)
        {
            return;
        }

        bool isWater = selectedProp.Category == PropCategory.Water;
        float snap = isWater ? 1f : SnapValues[snapIndex];
        Vector3 pos = new Vector3(Snap(world.x, snap), Snap(world.y, snap), 0f);
        if (isWater)
        {
            pos = new Vector3(Mathf.Floor(world.x), Mathf.Floor(world.y), 0f);
        }

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(pos, Vector3.forward, 0.3f);
        Handles.Label(pos + Vector3.up * 0.6f, selectedProp.displayName, EditorStyles.boldLabel);

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            PlaceProp(level, selectedProp, pos);
            e.Use();
        }
    }

    private static float Snap(float v, float step) => Mathf.Round(v / step) * step;

    private static void PlaceProp(LevelRoot level, PropCatalog.Entry entry, Vector3 position)
    {
        // 배치 한 번이 실행 취소 한 번이 되도록 그룹을 나눈다.
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(entry.displayName + " 배치");

        string containerName = entry.Category == PropCategory.Water ? "Water" : "Props";
        Transform parent = level.transform.Find(containerName);
        if (parent == null)
        {
            parent = new GameObject(containerName).transform;
            parent.SetParent(level.transform, false);
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "컨테이너 추가");
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, parent);
        go.transform.position = position;
        Undo.RegisterCreatedObjectUndo(go, entry.displayName + " 배치");
        if (go.TryGetComponent(out LevelProp prop))
        {
            prop.ApplySettings();
        }
        Selection.activeGameObject = go;
    }

    // ================================================================ 공통

    private void DrawToolToggle(Tool target, string label, bool enabled)
    {
        using (new EditorGUI.DisabledScope(!enabled))
        {
            bool on = tool == target;
            bool now = GUILayout.Toggle(on, label, "Button");
            if (now != on)
            {
                tool = now ? target : Tool.None;
                if (now)
                {
                    FocusSceneView();
                }
            }
        }
    }

    private static void FocusSceneView()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.Repaint();
        }
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

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        float aspect = tr.width / tr.height;
        Rect fit = rect;
        if (aspect > rect.width / rect.height)
        {
            fit.height = rect.width / aspect;
            fit.y += (rect.height - fit.height) * 0.5f;
        }
        else
        {
            fit.width = rect.height * aspect;
            fit.x += (rect.width - fit.width) * 0.5f;
        }
        GUI.DrawTextureWithTexCoords(fit, tex, uv);
    }
}
