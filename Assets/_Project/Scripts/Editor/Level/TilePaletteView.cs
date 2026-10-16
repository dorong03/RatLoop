using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 타일 팔레트. 타일 이미지 파일 안에서의 위치 그대로 보여준다. (Unity 타일 팔레트와 같은 방식)
// 칸을 클릭하면 한 칸, 끌면 영역이 선택되고, 선택한 모양 그대로 레벨에 찍힌다(스탬프).
// 여러 칸으로 이루어진 타일(예: 3칸짜리 기둥)도 선택한 모양이 흩어지지 않는다.
public class TilePaletteView
{
    public const string TilesRoot = "Assets/_Project/Art/Tiles";

    private class Group
    {
        public string name;
        public Texture2D texture;
        public int cellPixels;
        public int columns;
        public int rows;
        public readonly Dictionary<Vector2Int, TileBase> cells = new Dictionary<Vector2Int, TileBase>();
        public int tileCount;
    }

    public readonly struct StampTile
    {
        // 스탬프 왼쪽 아래 칸을 (0,0) 으로 한 위치
        public readonly int dx;
        public readonly int dy;
        public readonly TileBase tile;
        public readonly Sprite sprite;

        public StampTile(int dx, int dy, TileBase tile)
        {
            this.dx = dx;
            this.dy = dy;
            this.tile = tile;
            sprite = (tile as Tile)?.sprite;
        }
    }

    private static readonly float[] ZoomValues = { 1f, 1.5f, 2f, 3f };
    private static readonly string[] ZoomLabels = { "작게", "보통-", "보통", "크게" };

    private List<Group> groups;
    private string[] groupNames;
    private int groupIndex;
    private int zoomIndex = 2;
    private Vector2 scroll;

    // 선택한 영역 (팔레트 이미지의 칸 좌표, 위가 0행)
    private RectInt selection;
    private bool hasSelection;
    private Vector2Int dragAnchor;
    private string hoverText = "";

    private readonly List<StampTile> stamp = new List<StampTile>();
    private Vector2Int stampSize;

    public IReadOnlyList<StampTile> Stamp => stamp;
    public Vector2Int StampSize => stampSize;
    public bool HasStamp => stamp.Count > 0;

    public void ClearSelection()
    {
        hasSelection = false;
        stamp.Clear();
        stampSize = Vector2Int.zero;
    }

    public void Refresh()
    {
        groups = new List<Group>();
        var byName = new Dictionary<string, Group>();
        foreach (string guid in AssetDatabase.FindAssets("t:TileBase", new[] { TilesRoot }))
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid));
            Sprite sprite = (tile as Tile)?.sprite;
            if (sprite == null || sprite.texture == null)
            {
                continue;
            }

            Texture2D tex = sprite.texture;
            if (!byName.TryGetValue(tex.name, out Group g))
            {
                int cell = Mathf.Max(1, Mathf.RoundToInt(sprite.pixelsPerUnit));
                g = new Group
                {
                    name = tex.name,
                    texture = tex,
                    cellPixels = cell,
                    columns = Mathf.Max(1, tex.width / cell),
                    rows = Mathf.Max(1, tex.height / cell)
                };
                byName[tex.name] = g;
                groups.Add(g);
            }

            Rect r = sprite.textureRect;
            int col = Mathf.FloorToInt(r.x / g.cellPixels);
            // 텍스처 좌표는 아래가 0 이므로 위가 0행이 되도록 뒤집는다.
            int row = Mathf.FloorToInt((tex.height - r.yMax) / g.cellPixels);
            g.cells[new Vector2Int(col, row)] = tile;
            g.tileCount++;
        }

        groups.Sort((a, b) => b.tileCount.CompareTo(a.tileCount));
        groupNames = new string[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            groupNames[i] = $"{groups[i].name} ({groups[i].tileCount}개)";
        }
        groupIndex = Mathf.Clamp(groupIndex, 0, Mathf.Max(0, groups.Count - 1));
        ClearSelection();
    }

    // 선택이 바뀌면 true
    public bool Draw(float height)
    {
        if (groups == null)
        {
            Refresh();
        }
        if (groups.Count == 0)
        {
            EditorGUILayout.HelpBox("타일이 없습니다: " + TilesRoot, MessageType.Warning);
            return false;
        }

        bool changed = false;
        using (new EditorGUILayout.HorizontalScope())
        {
            int newGroup = EditorGUILayout.Popup(groupIndex, groupNames);
            if (newGroup != groupIndex)
            {
                groupIndex = newGroup;
                ClearSelection();
                changed = true;
            }
            zoomIndex = EditorGUILayout.Popup(zoomIndex, ZoomLabels, GUILayout.Width(64));
        }

        Group group = groups[groupIndex];
        float px = group.cellPixels * ZoomValues[zoomIndex];
        scroll = EditorGUILayout.BeginScrollView(scroll, true, true, GUILayout.Height(height));
        Rect area = GUILayoutUtility.GetRect(group.columns * px, group.rows * px, GUILayout.Width(group.columns * px), GUILayout.Height(group.rows * px));
        changed |= HandleMouse(group, area, px);
        DrawContents(group, area, px);
        EditorGUILayout.EndScrollView();

        string info;
        if (hasSelection && stamp.Count > 0)
        {
            info = stamp.Count == 1
                ? $"선택: 1칸 ({stamp[0].tile.name})"
                : $"선택: {stampSize.x} x {stampSize.y} 칸, 타일 {stamp.Count}개 (모양 그대로 찍힙니다)";
        }
        else
        {
            info = string.IsNullOrEmpty(hoverText) ? "타일을 클릭하거나 끌어서 영역을 고르세요." : hoverText;
        }
        EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
        return changed;
    }

    private bool HandleMouse(Group group, Rect area, float px)
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        EventType type = e.GetTypeForControl(id);
        bool inside = area.Contains(e.mousePosition);

        if (type == EventType.MouseMove || type == EventType.MouseDrag)
        {
            Vector2Int hover = CellAt(group, area, px, e.mousePosition);
            hoverText = group.cells.TryGetValue(hover, out TileBase t) ? $"{t.name}  (칸 {hover.x + 1}, {hover.y + 1})" : "";
        }

        bool changed = false;
        if (type == EventType.MouseDown && e.button == 0 && inside)
        {
            GUIUtility.hotControl = id;
            dragAnchor = CellAt(group, area, px, e.mousePosition);
            selection = new RectInt(dragAnchor.x, dragAnchor.y, 1, 1);
            hasSelection = true;
            RebuildStamp(group);
            changed = true;
            e.Use();
        }
        else if (type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            Vector2Int cell = CellAt(group, area, px, e.mousePosition);
            int x0 = Mathf.Min(dragAnchor.x, cell.x);
            int y0 = Mathf.Min(dragAnchor.y, cell.y);
            int x1 = Mathf.Max(dragAnchor.x, cell.x);
            int y1 = Mathf.Max(dragAnchor.y, cell.y);
            var rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            if (!rect.Equals(selection))
            {
                selection = rect;
                RebuildStamp(group);
                changed = true;
            }
            e.Use();
        }
        else if (type == EventType.MouseUp && GUIUtility.hotControl == id)
        {
            GUIUtility.hotControl = 0;
            e.Use();
        }
        return changed;
    }

    private static Vector2Int CellAt(Group group, Rect area, float px, Vector2 mouse)
    {
        int col = Mathf.Clamp(Mathf.FloorToInt((mouse.x - area.x) / px), 0, group.columns - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((mouse.y - area.y) / px), 0, group.rows - 1);
        return new Vector2Int(col, row);
    }

    // 선택 영역 안에 있는 타일만 스탬프로 만든다. 빈 칸은 찍지 않는다.
    private void RebuildStamp(Group group)
    {
        stamp.Clear();
        int bottomRow = selection.yMax - 1;
        int minDx = int.MaxValue, minDy = int.MaxValue, maxDx = int.MinValue, maxDy = int.MinValue;
        for (int row = selection.y; row < selection.yMax; row++)
        {
            for (int col = selection.x; col < selection.xMax; col++)
            {
                if (!group.cells.TryGetValue(new Vector2Int(col, row), out TileBase tile))
                {
                    continue;
                }
                int dx = col - selection.x;
                int dy = bottomRow - row; // 아래쪽 줄이 0, 위로 갈수록 커진다
                stamp.Add(new StampTile(dx, dy, tile));
                minDx = Mathf.Min(minDx, dx); maxDx = Mathf.Max(maxDx, dx);
                minDy = Mathf.Min(minDy, dy); maxDy = Mathf.Max(maxDy, dy);
            }
        }

        if (stamp.Count == 0)
        {
            stampSize = Vector2Int.zero;
            return;
        }

        // 빈 가장자리는 잘라내서 모양이 선택한 영역이 아니라 타일 기준이 되게 한다.
        if (minDx != 0 || minDy != 0)
        {
            for (int i = 0; i < stamp.Count; i++)
            {
                stamp[i] = new StampTile(stamp[i].dx - minDx, stamp[i].dy - minDy, stamp[i].tile);
            }
        }
        stampSize = new Vector2Int(maxDx - minDx + 1, maxDy - minDy + 1);
    }

    private void DrawContents(Group group, Rect area, float px)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f, 1f));

        // 칸 구분선
        var line = new Color(1f, 1f, 1f, 0.06f);
        for (int c = 0; c <= group.columns; c++)
        {
            EditorGUI.DrawRect(new Rect(area.x + c * px, area.y, 1f, area.height), line);
        }
        for (int r = 0; r <= group.rows; r++)
        {
            EditorGUI.DrawRect(new Rect(area.x, area.y + r * px, area.width, 1f), line);
        }

        foreach (var kv in group.cells)
        {
            Sprite sprite = (kv.Value as Tile)?.sprite;
            if (sprite == null)
            {
                continue;
            }
            var rect = new Rect(area.x + kv.Key.x * px, area.y + kv.Key.y * px, px, px);
            DrawSprite(rect, sprite);
        }

        if (hasSelection)
        {
            var sel = new Rect(area.x + selection.x * px, area.y + selection.y * px, selection.width * px, selection.height * px);
            Handles.DrawSolidRectangleWithOutline(sel, new Color(0.25f, 0.6f, 1f, 0.22f), new Color(0.35f, 0.7f, 1f, 1f));
        }
    }

    public static void DrawSprite(Rect rect, Sprite sprite, float alpha = 1f)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTextureWithTexCoords(rect, tex, uv);
        GUI.color = old;
    }
}
