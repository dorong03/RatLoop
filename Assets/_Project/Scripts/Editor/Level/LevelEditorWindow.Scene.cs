using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 레벨 에디터 창의 씬 화면 도구: 타일 칠하기, 프롭 놓기, 시작 위치/치즈 옮기기.
public partial class LevelEditorWindow
{
    private enum SceneTool { None, Paint, Erase, Place, Relocate }

    // 한 번 누르고 끄는 동안(스트로크)의 실행 취소 묶음
    private int strokeGroup = -1;
    private readonly HashSet<Object> strokeRecorded = new HashSet<Object>();
    private Vector3Int lastPaintedCell = new Vector3Int(int.MinValue, 0, 0);

    private float SnapValue => SnapValues[Mathf.Clamp(snapIndex, 0, SnapValues.Length - 1)];

    private SceneTool CurrentTool
    {
        get
        {
            switch (mode)
            {
                case Mode.Tiles: return eraser ? SceneTool.Erase : (palette.HasStamp ? SceneTool.Paint : SceneTool.None);
                case Mode.Props: return selectedProp != null ? SceneTool.Place : SceneTool.None;
                case Mode.Points: return relocate != Relocate.None ? SceneTool.Relocate : SceneTool.None;
                default: return SceneTool.None;
            }
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        LevelRoot level = LevelEditorTools.CurrentLevel;
        if (level == null || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        SceneTool tool = CurrentTool;
        DrawGrid(view, level);
        DrawBanner(view, tool);
        DrawPointMarkers(level, tool == SceneTool.None);

        if (tool == SceneTool.None)
        {
            DrawSelectedAreaHandles(level);
            return;
        }

        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        EventType type = e.GetTypeForControl(id);

        if (type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(id);
        }

        if (type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            SetMode(Mode.Select);
            e.Use();
            return;
        }

        Vector3 world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        world.z = 0f;

        DrawPreview(level, tool, world, e);

        bool left = e.button == 0 && !e.alt && !e.control && !e.command;
        if (type == EventType.MouseDown && left)
        {
            GUIUtility.hotControl = id;
            BeginStroke(tool);
            ApplyTool(level, tool, world, e, true);
            e.Use();
        }
        else if (type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            ApplyTool(level, tool, world, e, false);
            e.Use();
        }
        else if (type == EventType.MouseUp && GUIUtility.hotControl == id)
        {
            GUIUtility.hotControl = 0;
            EndStroke();
            e.Use();
        }

        if (type == EventType.MouseMove)
        {
            view.Repaint();
        }
    }

    // ---------------------------------------------------------------- 실행 취소 묶음

    // 클릭 한 번(끌기 포함)이 실행 취소 한 번이 되도록 묶는다.
    private void BeginStroke(SceneTool tool)
    {
        Undo.IncrementCurrentGroup();
        strokeGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ToolUndoName(tool));
        strokeRecorded.Clear();
        lastPaintedCell = new Vector3Int(int.MinValue, 0, 0);
    }

    private void EndStroke()
    {
        if (strokeGroup >= 0)
        {
            Undo.CollapseUndoOperations(strokeGroup);
            strokeGroup = -1;
        }
        strokeRecorded.Clear();
        MarkReportDirty();
        Repaint();
    }

    private static string ToolUndoName(SceneTool tool)
    {
        switch (tool)
        {
            case SceneTool.Paint: return "타일 칠하기";
            case SceneTool.Erase: return "타일 지우기";
            case SceneTool.Place: return "프롭 놓기";
            default: return "위치 옮기기";
        }
    }

    private void RecordOnce(Object target, bool complete)
    {
        if (!strokeRecorded.Add(target))
        {
            return;
        }
        if (complete)
        {
            Undo.RegisterCompleteObjectUndo(target, ToolUndoName(CurrentTool));
        }
        else
        {
            Undo.RecordObject(target, ToolUndoName(CurrentTool));
        }
    }

    // ---------------------------------------------------------------- 도구 동작

    private void ApplyTool(LevelRoot level, SceneTool tool, Vector3 world, Event e, bool isClick)
    {
        switch (tool)
        {
            case SceneTool.Paint:
            case SceneTool.Erase:
                ApplyTiles(level, tool, world, e);
                break;
            case SceneTool.Place:
                if (isClick)
                {
                    PlaceProp(level, selectedProp, PlacementPosition(selectedProp, world));
                    // 놓은 프롭이 선택되며 생기는 선택 변경도 같은 실행 취소에 포함된다.
                }
                break;
            case SceneTool.Relocate:
                Relocated(level, world);
                break;
        }
    }

    private void ApplyTiles(LevelRoot level, SceneTool tool, Vector3 world, Event e)
    {
        Tilemap tm = FindTargetTilemap(level);
        if (tm == null)
        {
            return;
        }

        Vector3Int cell = tm.WorldToCell(world);
        if (cell == lastPaintedCell)
        {
            return;
        }
        lastPaintedCell = cell;

        bool erase = tool == SceneTool.Erase || e.shift;
        bool changed = false;
        if (erase)
        {
            if (tm.GetTile(cell) != null)
            {
                RecordOnce(tm, true);
                tm.SetTile(cell, null);
                changed = true;
            }
        }
        else
        {
            foreach (TilePaletteView.StampTile s in palette.Stamp)
            {
                Vector3Int target = cell + new Vector3Int(s.dx, s.dy, 0);
                if (tm.GetTile(target) != s.tile)
                {
                    RecordOnce(tm, true);
                    tm.SetTile(target, s.tile);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(tm);
        }
    }

    private void Relocated(LevelRoot level, Vector3 world)
    {
        Transform target = null;
        if (relocate == Relocate.Spawn && level.SpawnPoint != null)
        {
            target = level.SpawnPoint.transform;
        }
        else if (relocate == Relocate.Goal)
        {
            GoalProp goal = level.GetComponentInChildren<GoalProp>(true);
            target = goal != null ? goal.transform : null;
        }
        if (target == null)
        {
            return;
        }

        RecordOnce(target, false);
        target.position = new Vector3(Snap(world.x, SnapValue), Snap(world.y, SnapValue), target.position.z);
        EditorUtility.SetDirty(target);
    }

    private Vector3 PlacementPosition(PropCatalog.Entry entry, Vector3 world)
    {
        // 물, 투명벽은 칸 단위 영역이라 항상 칸의 왼쪽 아래 모서리에 놓는다.
        if (entry.prefab != null && entry.prefab.GetComponent<IAreaProp>() != null)
        {
            return new Vector3(Mathf.Floor(world.x), Mathf.Floor(world.y), 0f);
        }
        return SnapPosition(world);
    }

    private Vector3 SnapPosition(Vector3 world) => new Vector3(Snap(world.x, SnapValue), Snap(world.y, SnapValue), 0f);

    private static float Snap(float v, float step) => Mathf.Round(v / step) * step;

    private static string ContainerFor(PropCatalog.Entry entry)
    {
        if (entry.Category == PropCategory.Water) return "Water";
        if (entry.Category == PropCategory.Barrier) return "Bounds";
        return "Props";
    }

    // 프롭을 놓는다. 놓기 + 선택 변경이 실행 취소 한 번이 된다.
    private static void PlaceProp(LevelRoot level, PropCatalog.Entry entry, Vector3 position)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(entry.displayName + " 놓기");

        string containerName = ContainerFor(entry);
        Transform parent = level.transform.Find(containerName);
        if (parent == null)
        {
            parent = new GameObject(containerName).transform;
            parent.SetParent(level.transform, false);
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "프롭 묶음 추가");
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, parent);
        Undo.RegisterCreatedObjectUndo(go, entry.displayName + " 놓기");
        go.transform.position = position;
        if (go.TryGetComponent(out LevelProp prop))
        {
            prop.ApplySettings();
        }
        Selection.activeGameObject = go;
        Undo.CollapseUndoOperations(group);
    }

    // ---------------------------------------------------------------- 씬 화면 표시

    private void DrawBanner(SceneView view, SceneTool tool)
    {
        string text = BannerText(tool);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Handles.BeginGUI();
        var content = new GUIContent(text);
        float width = Mathf.Min(EditorUI.Banner.CalcSize(content).x + 4f, view.position.width - 20f);
        GUI.Box(new Rect(10f, 10f, width, 26f), content, EditorUI.Banner);
        Handles.EndGUI();
    }

    // 지금 켜진 도구 이름(굵게)과 조작법. 창 바닥 줄의 안내와 같은 글을 쓴다.
    private string BannerText(SceneTool tool)
    {
        switch (tool)
        {
            case SceneTool.Paint:
                return $"<b>타일 칠하기 · {TileLayerLabels[(int)tileLayer]}</b>   {CurrentHint()}";
            case SceneTool.Erase:
                return $"<b>타일 지우기 · {TileLayerLabels[(int)tileLayer]}</b>   {CurrentHint()}";
            case SceneTool.Place:
                return $"<b>놓기 · {selectedProp.displayName}</b>   {CurrentHint()}";
            case SceneTool.Relocate:
                return $"<b>{(relocate == Relocate.Spawn ? "시작 위치" : "치즈")} 옮기기</b>   {CurrentHint()}";
            default:
                if (mode == Mode.Select) return $"<b>선택</b>   {CurrentHint()}";
                if (mode == Mode.Points) return "<b>시작·치즈</b>   씬 화면의 원을 끌어서 옮겨요";
                return null;
        }
    }

    // 물, 투명벽을 고르면 씬 화면에 크기 손잡이를 보여준다. (인스펙터 창이 없어도 동작)
    // 오른쪽 점: 가로, 위쪽 점: 세로, 오른쪽 위 네모: 둘 다
    private void DrawSelectedAreaHandles(LevelRoot level)
    {
        GameObject go = Selection.activeGameObject;
        LevelProp prop = go != null ? go.GetComponentInParent<LevelProp>() : null;
        if (prop == null || !(prop is IAreaProp area) || !prop.transform.IsChildOf(level.transform))
        {
            return;
        }

        Color color = prop is WaterVolume ? new Color(0.3f, 0.8f, 1f)
            : (prop is InvisibleWall wall && wall.BlocksProps ? new Color(1f, 0.4f, 0.4f) : new Color(0.8f, 0.5f, 1f));
        string label = prop is WaterVolume ? "물" : "투명벽";
        AreaHandles.Draw(prop, area, color, label);
    }

    // 시작 위치와 치즈를 항상 표시하고, 다른 도구가 꺼져 있을 때는 끌어서 옮길 수 있게 한다.
    private void DrawPointMarkers(LevelRoot level, bool draggable)
    {
        DrawMarker(level.SpawnPoint != null ? level.SpawnPoint.transform : null, "시작", new Color(0.35f, 1f, 0.4f), draggable);
        GoalProp goal = level.GetComponentInChildren<GoalProp>(true);
        DrawMarker(goal != null ? goal.transform : null, "치즈", new Color(1f, 0.85f, 0.2f), draggable);
    }

    private void DrawMarker(Transform target, string label, Color color, bool draggable)
    {
        if (target == null)
        {
            return;
        }

        Vector3 pos = target.position;
        float size = HandleUtility.GetHandleSize(pos) * 0.12f;
        Handles.color = color;
        Handles.Label(pos + Vector3.up * (size * 4f + 0.3f), label, EditorStyles.boldLabel);

        if (!draggable)
        {
            Handles.DrawWireDisc(pos, Vector3.forward, size * 1.5f);
            return;
        }

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(pos, size * 1.5f, Vector3.zero, Handles.CircleHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, label + " 옮기기");
            target.position = new Vector3(Snap(moved.x, SnapValue), Snap(moved.y, SnapValue), pos.z);
            MarkReportDirty();
            Repaint();
        }
    }

    private void DrawPreview(LevelRoot level, SceneTool tool, Vector3 world, Event e)
    {
        if (e.type != EventType.Repaint)
        {
            return;
        }

        switch (tool)
        {
            case SceneTool.Paint:
            case SceneTool.Erase:
                DrawTilePreview(level, tool, world, e);
                break;
            case SceneTool.Place:
                DrawPropPreview(world);
                break;
            case SceneTool.Relocate:
                {
                    Vector3 p = SnapPosition(world);
                    Handles.color = relocate == Relocate.Spawn ? new Color(0.35f, 1f, 0.4f) : new Color(1f, 0.85f, 0.2f);
                    Handles.DrawWireDisc(p, Vector3.forward, 0.35f);
                    break;
                }
        }
    }

    private void DrawTilePreview(LevelRoot level, SceneTool tool, Vector3 world, Event e)
    {
        Tilemap tm = FindTargetTilemap(level);
        if (tm == null)
        {
            return;
        }

        Vector3Int cell = tm.WorldToCell(world);
        Vector3 size = tm.layoutGrid.cellSize;
        bool erase = tool == SceneTool.Erase || e.shift;

        if (erase)
        {
            DrawCellOutline(tm, cell, 1, 1, new Color(1f, 0.25f, 0.25f, 0.3f), Color.red);
            return;
        }

        // 찍힐 모양을 반투명 그림으로 미리 보여준다.
        Handles.BeginGUI();
        foreach (TilePaletteView.StampTile s in palette.Stamp)
        {
            Vector3 min = tm.CellToWorld(cell + new Vector3Int(s.dx, s.dy, 0));
            Vector2 a = HandleUtility.WorldToGUIPoint(min);
            Vector2 b = HandleUtility.WorldToGUIPoint(min + new Vector3(size.x, size.y, 0f));
            TilePaletteView.DrawSprite(new Rect(a.x, b.y, b.x - a.x, a.y - b.y), s.sprite, 0.65f);
        }
        Handles.EndGUI();
        DrawCellOutline(tm, cell, palette.StampSize.x, palette.StampSize.y, new Color(0.2f, 0.6f, 1f, 0.12f), Color.cyan);
    }

    private static void DrawCellOutline(Tilemap tm, Vector3Int cell, int w, int h, Color fill, Color outline)
    {
        Vector3 min = tm.CellToWorld(cell);
        Vector3 size = tm.layoutGrid.cellSize;
        Vector3 max = min + new Vector3(size.x * w, size.y * h, 0f);
        Handles.DrawSolidRectangleWithOutline(
            new[] { min, new Vector3(max.x, min.y), max, new Vector3(min.x, max.y) }, fill, outline);
    }

    // ---------------------------------------------------------------- 프롭 미리보기 (흐린 그림)

    private struct GhostSprite
    {
        public Sprite sprite;
        public Matrix4x4 matrix; // 프롭 루트 기준 위치/크기
        public bool flipX;
        public bool flipY;
        public int order;
    }

    private readonly Dictionary<GameObject, List<GhostSprite>> ghostCache = new Dictionary<GameObject, List<GhostSprite>>();
    // 미리보기를 만들 때의 프리팹 크기. 종류의 크기를 바꾸면 미리보기를 다시 만든다.
    private readonly Dictionary<GameObject, Vector3> ghostScale = new Dictionary<GameObject, Vector3>();

    private List<GhostSprite> GetGhost(GameObject prefab)
    {
        if (ghostCache.TryGetValue(prefab, out var cached)
            && ghostScale.TryGetValue(prefab, out Vector3 scale) && scale == prefab.transform.localScale)
        {
            return cached;
        }
        ghostScale[prefab] = prefab.transform.localScale;

        var list = new List<GhostSprite>();
        Matrix4x4 rootToLocal = prefab.transform.worldToLocalMatrix;
        // 프리팹 에셋의 루트 위치는 0 이 아닐 수 있으므로, 루트 기준 상대 위치만 쓰고 크기와 회전은 루트 것을 적용한다.
        Matrix4x4 rootTrs = Matrix4x4.TRS(Vector3.zero, prefab.transform.rotation, prefab.transform.localScale);
        foreach (SpriteRenderer sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null || !sr.enabled)
            {
                continue;
            }
            list.Add(new GhostSprite
            {
                sprite = sr.sprite,
                matrix = rootTrs * rootToLocal * sr.transform.localToWorldMatrix,
                flipX = sr.flipX,
                flipY = sr.flipY,
                order = sr.sortingOrder
            });
        }

        AddRopeGhost(prefab, list, rootTrs, rootToLocal);
        list.Sort((x, y) => x.order.CompareTo(y.order));
        ghostCache[prefab] = list;
        return list;
    }

    // 로프는 프리팹 안에 마디가 없어서, 마디 프리팹을 마디 수만큼 이어서 미리보기를 만든다.
    private static void AddRopeGhost(GameObject prefab, List<GhostSprite> list, Matrix4x4 rootTrs, Matrix4x4 rootToLocal)
    {
        var rope = prefab.GetComponent<RopeProp>();
        if (rope == null)
        {
            return;
        }

        var so = new SerializedObject(rope);
        var link = so.FindProperty("linkPrefab").objectReferenceValue as GameObject;
        var segmentRoot = so.FindProperty("segmentRoot").objectReferenceValue as Transform;
        int count = so.FindProperty("segmentCount").intValue;
        float spacing = so.FindProperty("segmentSpacing").floatValue;
        if (link == null || segmentRoot == null)
        {
            return;
        }

        Matrix4x4 segmentMatrix = rootTrs * rootToLocal * segmentRoot.localToWorldMatrix;
        Matrix4x4 linkToLocal = link.transform.worldToLocalMatrix;
        foreach (SpriteRenderer sr in link.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null)
            {
                continue;
            }
            Matrix4x4 inLink = linkToLocal * sr.transform.localToWorldMatrix;
            for (int i = 0; i < count; i++)
            {
                list.Add(new GhostSprite
                {
                    sprite = sr.sprite,
                    matrix = segmentMatrix * Matrix4x4.Translate(new Vector3(0f, -spacing * i, 0f)) * inLink,
                    flipX = sr.flipX,
                    flipY = sr.flipY,
                    order = sr.sortingOrder
                });
            }
        }
    }

    private void DrawPropPreview(Vector3 world)
    {
        if (selectedProp == null || selectedProp.prefab == null)
        {
            return;
        }

        Vector3 pos = PlacementPosition(selectedProp, world);
        IAreaProp area = selectedProp.prefab.GetComponent<IAreaProp>();
        if (area != null)
        {
            // 물, 투명벽: 차지할 칸 영역을 흐리게 칠해서 보여준다.
            Vector2Int size = area.AreaSize;
            bool wall = selectedProp.Category == PropCategory.Barrier;
            Color fill = wall ? new Color(0.7f, 0.4f, 1f, 0.3f) : new Color(0.3f, 0.7f, 1f, 0.3f);
            Handles.DrawSolidRectangleWithOutline(
                new[] { pos, pos + new Vector3(size.x, 0f), pos + new Vector3(size.x, size.y), pos + new Vector3(0f, size.y) },
                fill, new Color(fill.r, fill.g, fill.b, 1f));
            return;
        }

        // 놓일 모습 그대로 반투명하게 보여준다. 로프는 마디까지 이어서 보여준다.
        Handles.BeginGUI();
        foreach (GhostSprite g in GetGhost(selectedProp.prefab))
        {
            DrawGhostSprite(g, pos);
        }
        Handles.EndGUI();

        Handles.color = new Color(0.3f, 0.9f, 1f, 0.9f);
        Handles.DrawLine(pos + Vector3.left * 0.15f, pos + Vector3.right * 0.15f);
        Handles.DrawLine(pos + Vector3.down * 0.15f, pos + Vector3.up * 0.15f);
        Handles.Label(pos + Vector3.up * 0.3f, selectedProp.displayName, EditorStyles.miniBoldLabel);
    }

    private static void DrawGhostSprite(GhostSprite g, Vector3 position)
    {
        Bounds b = g.sprite.bounds;
        Matrix4x4 m = Matrix4x4.Translate(position) * g.matrix;
        Vector3 c0 = m.MultiplyPoint3x4(new Vector3(b.min.x, b.min.y, 0f));
        Vector3 c1 = m.MultiplyPoint3x4(new Vector3(b.max.x, b.max.y, 0f));
        Vector3 c2 = m.MultiplyPoint3x4(new Vector3(b.min.x, b.max.y, 0f));
        Vector3 c3 = m.MultiplyPoint3x4(new Vector3(b.max.x, b.min.y, 0f));
        float minX = Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x));
        float maxX = Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x));
        float minY = Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y));
        float maxY = Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y));

        Vector2 a = HandleUtility.WorldToGUIPoint(new Vector3(minX, minY, 0f));
        Vector2 d = HandleUtility.WorldToGUIPoint(new Vector3(maxX, maxY, 0f));
        var rect = new Rect(Mathf.Min(a.x, d.x), Mathf.Min(a.y, d.y), Mathf.Abs(d.x - a.x), Mathf.Abs(a.y - d.y));
        TilePaletteView.DrawSprite(rect, g.sprite, 0.6f, g.flipX, g.flipY);
    }

    // ---------------------------------------------------------------- 격자

    // 타일 칸이 나뉜 것을 보여주는 격자. 5칸마다 조금 더 진하게 그린다.
    private void DrawGrid(SceneView view, LevelRoot level)
    {
        if (!showGrid || Event.current.type != EventType.Repaint)
        {
            return;
        }
        if (mode != Mode.Tiles && mode != Mode.Props && mode != Mode.Points)
        {
            return;
        }

        var grid = level.GetComponentInChildren<Grid>(true);
        Vector2 cell = grid != null ? new Vector2(grid.cellSize.x, grid.cellSize.y) : Vector2.one;
        if (cell.x <= 0f || cell.y <= 0f)
        {
            return;
        }

        Vector3 topLeft = HandleUtility.GUIPointToWorldRay(new Vector2(0f, 0f)).origin;
        Vector3 bottomRight = HandleUtility.GUIPointToWorldRay(new Vector2(view.position.width, view.position.height)).origin;
        float minX = Mathf.Min(topLeft.x, bottomRight.x), maxX = Mathf.Max(topLeft.x, bottomRight.x);
        float minY = Mathf.Min(topLeft.y, bottomRight.y), maxY = Mathf.Max(topLeft.y, bottomRight.y);

        int x0 = Mathf.FloorToInt(minX / cell.x), x1 = Mathf.CeilToInt(maxX / cell.x);
        int y0 = Mathf.FloorToInt(minY / cell.y), y1 = Mathf.CeilToInt(maxY / cell.y);
        // 너무 멀리서 볼 때는 선이 화면을 덮지 않게 줄인다.
        float pixelsPerCell = view.position.width / Mathf.Max(0.01f, (maxX - minX) / cell.x);
        if (pixelsPerCell < 3f)
        {
            return;
        }
        bool drawMinor = pixelsPerCell >= 9f;

        var oldZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        for (int x = x0; x <= x1; x++)
        {
            bool major = x % 5 == 0;
            if (!major && !drawMinor) continue;
            Handles.color = major ? new Color(1f, 1f, 1f, 0.22f) : new Color(1f, 1f, 1f, 0.09f);
            Handles.DrawLine(new Vector3(x * cell.x, y0 * cell.y, 0f), new Vector3(x * cell.x, y1 * cell.y, 0f));
        }
        for (int y = y0; y <= y1; y++)
        {
            bool major = y % 5 == 0;
            if (!major && !drawMinor) continue;
            Handles.color = major ? new Color(1f, 1f, 1f, 0.22f) : new Color(1f, 1f, 1f, 0.09f);
            Handles.DrawLine(new Vector3(x0 * cell.x, y * cell.y, 0f), new Vector3(x1 * cell.x, y * cell.y, 0f));
        }
        Handles.zTest = oldZTest;
    }
}
