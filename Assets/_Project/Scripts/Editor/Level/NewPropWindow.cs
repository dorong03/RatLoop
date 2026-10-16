using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 새 프롭 만들기 (코드 없이 그림으로 프롭 추가)
// 그림과 종류만 고르면 프리팹, 콜라이더, 레이어, 카탈로그 등록까지 자동으로 한다.
// 한 장짜리 그림을 쓰거나, 칸으로 나뉜 이미지에서 여러 칸을 끌어서 골라 하나의 프롭(예: 책)으로 만들 수 있다.
// 특수한 동작이 필요한 기믹은 개발자가 LevelProp 을 상속해 만든다. (Docs/AddingContent.md)
//
// 화면: 위 제목 → 스크롤되는 단계 카드(그림, 이름과 종류, 부딪히는 모양) → 바닥 줄(취소, 만들기)
public class NewPropWindow : EditorWindow
{
    private enum Kind { Physics, Static, Decoration }
    private enum Shape { Box, Circle }
    private enum Source { Single, Sheet }

    private static readonly GUIContent[] KindLabels =
    {
        new GUIContent("물리 프롭", "밀거나 쌓을 수 있어요. 무게가 있어요."),
        new GUIContent("고정 프롭", "밟을 수 있고 움직이지 않아요."),
        new GUIContent("장식", "부딪히지 않고 지나가요.")
    };
    private static readonly string[] KindHelp =
    {
        "밀거나 쌓을 수 있는 물체예요. 박스, 공, 책 같은 것에 써요.",
        "밟을 수 있고 움직이지 않는 발판이에요. 버스, 선반 같은 것에 써요.",
        "부딪히지 않고 지나가는 그림이에요. 벽시계, 포스터 같은 것에 써요."
    };
    private static readonly string[] ShapeLabels = { "사각형", "원" };
    private static readonly string[] SourceLabels = { "한 장짜리 그림", "여러 칸을 묶은 그림" };
    private static readonly float[] ZoomValues = { 1f, 2f, 3f, 4f };
    private static readonly string[] ZoomLabels = { "작게", "보통", "크게", "아주 크게" };
    private const string PropsFolder = "Assets/_Project/Prefabs/Props/Custom";
    private const string ArtFolder = "Assets/_Project/Art/Props/Custom";

    // 프롭을 이루는 그림 조각 하나. 위치는 프롭 중심 기준.
    private struct Piece
    {
        public Sprite sprite;
        public Vector2 position;
    }

    private Source source;
    private Sprite sprite;
    private string displayName = "";
    private Kind kind;
    private Shape shape;
    private float mass = 3f;
    private string description = "";
    private System.Action<string> onCreated;
    private Vector2 bodyScroll;

    // 여러 칸 고르기
    private Texture2D sheet;
    private List<Sprite> sheetSprites = new List<Sprite>();
    private int cellSize = TileSheetImporter.DefaultCellSize;
    private int zoomIndex = 1;
    private Vector2 sheetScroll;
    private RectInt selection;
    private bool hasSelection;
    private Vector2Int dragAnchor;
    private readonly List<Sprite> selectedSprites = new List<Sprite>();

    public static void Open(System.Action<string> created)
    {
        var w = GetWindow<NewPropWindow>(true, "새 프롭 만들기", true);
        w.minSize = new Vector2(420f, 520f);
        w.maxSize = new Vector2(1400f, 1200f);
        if (w.position.width < 480f || w.position.height < 640f)
        {
            Rect p = w.position;
            p.size = new Vector2(Mathf.Max(p.width, 480f), Mathf.Max(p.height, 680f));
            w.position = p;
        }
        w.onCreated = created;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(6);
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("새 프롭 만들기", EditorUI.Title);
                GUILayout.Label("그림을 고르고 종류를 정하면 프롭 팔레트에 바로 추가돼요.", EditorUI.Caption);
            }
        }
        EditorGUILayout.Space(4);

        bodyScroll = EditorGUILayout.BeginScrollView(bodyScroll);
        DrawImageStep();
        DrawKindStep();
        if (kind != Kind.Decoration)
        {
            DrawShapeStep();
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();

        // 만들기 버튼은 내용이 길어져도 항상 보이도록 바닥 줄에 둔다.
        string problem = Problem();
        EditorUI.FooterBar(() =>
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(problem ?? "준비됐어요. 만들면 팔레트에서 바로 고른 상태가 돼요.", EditorUI.Caption);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("취소", GUILayout.Width(64), GUILayout.Height(26)))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                using (new EditorGUI.DisabledScope(problem != null))
                {
                    if (EditorUI.Button("만들기", EditorUI.ButtonKind.Primary, GUILayout.Width(88), GUILayout.Height(26)))
                    {
                        string id = Create();
                        if (id != null)
                        {
                            onCreated?.Invoke(id);
                            Close();
                        }
                        GUIUtility.ExitGUI();
                    }
                }
            }
        });
    }

    // ---------------------------------------------------------------- 단계 카드

    private void DrawImageStep()
    {
        EditorUI.Header("1. 그림");
        using (EditorUI.CardScope())
        {
            Source newSource = (Source)GUILayout.Toolbar((int)source, SourceLabels, GUILayout.Height(24));
            if (newSource != source)
            {
                source = newSource;
                GUI.FocusControl(null);
            }
            EditorGUILayout.Space(4);
            if (source == Source.Single)
            {
                DrawSingleImage();
            }
            else
            {
                DrawSheetImage();
            }
        }
    }

    private void DrawKindStep()
    {
        EditorUI.Header("2. 이름과 종류");
        using (EditorUI.CardScope())
        {
            displayName = EditorUI.TextField(displayName, "팔레트에 보일 이름");
            description = EditorUI.TextField(description, "설명 (선택) · 팔레트에서 마우스를 올리면 보여요");
            EditorGUILayout.Space(4);
            kind = (Kind)GUILayout.Toolbar((int)kind, KindLabels, GUILayout.Height(24));
            GUILayout.Label(KindHelp[(int)kind], EditorUI.Caption);
        }
    }

    private void DrawShapeStep()
    {
        EditorUI.Header(kind == Kind.Physics ? "3. 부딪히는 모양과 무게" : "3. 부딪히는 모양");
        using (EditorUI.CardScope())
        {
            shape = (Shape)GUILayout.Toolbar((int)shape, ShapeLabels, GUILayout.Height(22));
            GUILayout.Label("모양은 그림의 불투명한 부분에 맞춰져요. 둥근 물건은 원을 골라 주세요.", EditorUI.Caption);
            if (kind == Kind.Physics)
            {
                EditorGUILayout.Space(4);
                mass = EditorGUILayout.Slider("무게", mass, 0.5f, 100f);
                GUILayout.Label("크기, 마찰, 튕김 같은 값은 만든 뒤 '선택' 탭의 공통 설정에서 바꿀 수 있어요.", EditorUI.Caption);
            }
        }
    }

    // ---------------------------------------------------------------- 한 장짜리 그림

    private void DrawSingleImage()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            sprite = (Sprite)EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Width(72), GUILayout.Height(72));
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("프로젝트 창의 스프라이트를 왼쪽 칸에 끌어 놓거나, PC에서 PNG를 불러오세요.", EditorUI.Caption);
                if (GUILayout.Button("PC에서 PNG 불러오기…"))
                {
                    string path = EditorUtility.OpenFilePanel("프롭 그림 고르기 (PNG)", "", "png");
                    if (!string.IsNullOrEmpty(path))
                    {
                        sprite = ImportSprite(path);
                        SuggestName(Path.GetFileNameWithoutExtension(path));
                    }
                    GUIUtility.ExitGUI();
                }
                if (sprite != null)
                {
                    Vector2 size = sprite.bounds.size;
                    GUILayout.Label($"크기 {size.x:0.##} × {size.y:0.##}칸", EditorStyles.miniBoldLabel);
                }
            }
        }
        if (sprite != null && LooksLikeBackground(sprite.rect.width, sprite.rect.height))
        {
            EditorGUILayout.HelpBox(BackgroundNotice, MessageType.Warning);
        }
    }

    // 방 전체 그림처럼 큰 그림은 프롭이 아니라 배경일 가능성이 높다.
    private const string BackgroundNotice = "방 전체를 그린 배경 그림 같아요. 배경은 프롭이 아니라 레벨 에디터의 '레벨' 탭 > 배경에서 넣어요.";

    private static bool LooksLikeBackground(float width, float height) => width >= 640f || height >= 360f;

    // 큰 그림을 칸으로 나누기 전에 한 번 묻는다. (배경을 프롭으로 잘못 가져와 조각이 수천 개 생기는 것을 막는다)
    private static bool ConfirmNotBackground(string pngPath)
    {
        byte[] header = new byte[24];
        using (FileStream f = File.OpenRead(pngPath))
        {
            if (f.Read(header, 0, header.Length) < header.Length)
            {
                return true;
            }
        }
        int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        if (!LooksLikeBackground(width, height))
        {
            return true;
        }
        return EditorUtility.DisplayDialog("배경 그림 같아요",
            $"{width} × {height}픽셀의 큰 그림이에요. 방 전체를 그린 배경이라면 레벨 에디터의 '레벨' 탭 > 배경에서 넣어 주세요.\n\n그래도 칸으로 나눠서 프롭을 만들까요?",
            "나눠서 만들기", "취소");
    }

    // ---------------------------------------------------------------- 여러 칸을 묶은 그림

    private void DrawSheetImage()
    {
        GUILayout.Label("책처럼 여러 칸으로 그려진 그림을 끌어서 고르면, 고른 모양 그대로 하나의 프롭이 돼요.", EditorUI.Caption);
        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            var picked = (Texture2D)EditorGUILayout.ObjectField(sheet, typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60));
            if (EditorGUI.EndChangeCheck())
            {
                SetSheet(picked);
            }
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("프로젝트 창의 이미지를 왼쪽 칸에 끌어 놓거나, PC에서 PNG를 불러오세요.", EditorUI.Caption);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("PC에서 PNG 불러오기…"))
                    {
                        string path = EditorUtility.OpenFilePanel("프롭 그림 고르기 (여러 칸, PNG)", "", "png");
                        if (!string.IsNullOrEmpty(path) && ConfirmNotBackground(path))
                        {
                            SetSheet(ImportSheet(path, cellSize));
                            SuggestName(Path.GetFileNameWithoutExtension(path));
                        }
                        GUIUtility.ExitGUI();
                    }
                    GUILayout.Label("한 칸", EditorStyles.miniLabel, GUILayout.Width(28));
                    cellSize = Mathf.Clamp(EditorGUILayout.IntField(cellSize, GUILayout.Width(36)), 4, 256);
                    GUILayout.Label("픽셀", EditorStyles.miniLabel, GUILayout.Width(28));
                }
            }
        }

        if (sheet == null)
        {
            return;
        }

        if (LooksLikeBackground(sheet.width, sheet.height))
        {
            EditorGUILayout.HelpBox(BackgroundNotice, MessageType.Warning);
        }

        if (sheetSprites.Count == 0)
        {
            EditorGUILayout.HelpBox("아직 칸으로 나뉘지 않은 이미지예요. 원본은 그대로 두고, 복사본을 만들어 칸 크기대로 나눌게요.", MessageType.Info);
            if (GUILayout.Button("복사본 만들어 나누기"))
            {
                SetSheet(ImportSheet(Path.GetFullPath(AssetDatabase.GetAssetPath(sheet)), cellSize));
                GUIUtility.ExitGUI();
            }
            return;
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("칸을 클릭하거나, 끌어서 여러 칸을 함께 골라요.", EditorUI.Caption);
            GUILayout.FlexibleSpace();
            zoomIndex = EditorGUILayout.Popup(zoomIndex, ZoomLabels, GUILayout.Width(80));
        }

        // 한 칸이 24, 48, 72, 96 픽셀로 보이게 한다.
        float scale = ZoomValues[zoomIndex] * 24f / Mathf.Max(1, cellSize);
        float w = sheet.width * scale, h = sheet.height * scale;
        sheetScroll = EditorGUILayout.BeginScrollView(sheetScroll, GUILayout.Height(Mathf.Clamp(h + 18f, 80f, 260f)));
        Rect area = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
        HandleSheetMouse(area, scale);
        DrawSheet(area, scale);
        EditorGUILayout.EndScrollView();

        if (selectedSprites.Count > 0)
        {
            Vector2 size = PiecesBounds(BuildPieces()).size;
            EditorGUILayout.Space(4);
            GUILayout.Label($"미리 보기 · 조각 {selectedSprites.Count}개 · {size.x:0.##} × {size.y:0.##}칸", EditorStyles.miniBoldLabel);
            DrawSelectionPreview();
        }
    }

    private void SetSheet(Texture2D tex)
    {
        sheet = tex;
        sheetSprites.Clear();
        hasSelection = false;
        selectedSprites.Clear();
        if (tex == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.spriteImportMode == SpriteImportMode.Multiple)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (o is Sprite s)
                {
                    sheetSprites.Add(s);
                }
            }
            if (importer.spritePixelsPerUnit > 0f)
            {
                cellSize = Mathf.RoundToInt(importer.spritePixelsPerUnit);
            }
        }
        SuggestName(tex.name);
    }

    private void HandleSheetMouse(Rect area, float scale)
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        EventType type = e.GetTypeForControl(id);
        float px = cellSize * scale;
        int columns = Mathf.Max(1, Mathf.CeilToInt(sheet.width / (float)cellSize));
        int rows = Mathf.Max(1, Mathf.CeilToInt(sheet.height / (float)cellSize));
        Vector2Int CellAt(Vector2 m) => new Vector2Int(
            Mathf.Clamp(Mathf.FloorToInt((m.x - area.x) / px), 0, columns - 1),
            Mathf.Clamp(Mathf.FloorToInt((m.y - area.y) / px), 0, rows - 1));

        if (type == EventType.MouseDown && e.button == 0 && area.Contains(e.mousePosition))
        {
            GUIUtility.hotControl = id;
            dragAnchor = CellAt(e.mousePosition);
            selection = new RectInt(dragAnchor.x, dragAnchor.y, 1, 1);
            hasSelection = true;
            RebuildSelection();
            e.Use();
        }
        else if (type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            Vector2Int c = CellAt(e.mousePosition);
            selection = new RectInt(Mathf.Min(dragAnchor.x, c.x), Mathf.Min(dragAnchor.y, c.y),
                Mathf.Abs(dragAnchor.x - c.x) + 1, Mathf.Abs(dragAnchor.y - c.y) + 1);
            RebuildSelection();
            e.Use();
            Repaint();
        }
        else if (type == EventType.MouseUp && GUIUtility.hotControl == id)
        {
            GUIUtility.hotControl = 0;
            e.Use();
        }
    }

    // 선택 영역(위가 0행인 칸 좌표) 안에 중심이 있는 조각을 고른다.
    private void RebuildSelection()
    {
        selectedSprites.Clear();
        foreach (Sprite s in sheetSprites)
        {
            Vector2 c = s.rect.center;
            float col = c.x / cellSize;
            float row = (sheet.height - c.y) / cellSize;
            if (col >= selection.x && col < selection.xMax && row >= selection.y && row < selection.yMax)
            {
                selectedSprites.Add(s);
            }
        }
    }

    private void DrawSheet(Rect area, float scale)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f, 1f));
        GUI.DrawTexture(area, sheet, ScaleMode.StretchToFill, true);

        float px = cellSize * scale;
        var line = new Color(1f, 1f, 1f, 0.08f);
        for (float x = 0; x <= area.width + 0.5f; x += px)
        {
            EditorGUI.DrawRect(new Rect(area.x + x, area.y, 1f, area.height), line);
        }
        for (float y = 0; y <= area.height + 0.5f; y += px)
        {
            EditorGUI.DrawRect(new Rect(area.x, area.y + y, area.width, 1f), line);
        }

        // 고른 조각은 밝게 테두리를 친다.
        foreach (Sprite s in selectedSprites)
        {
            Rect r = s.rect;
            var gui = new Rect(area.x + r.x * scale, area.y + (sheet.height - r.yMax) * scale, r.width * scale, r.height * scale);
            Handles.DrawSolidRectangleWithOutline(gui, new Color(1f, 1f, 1f, 0f), new Color(1f, 0.85f, 0.3f, 1f));
        }

        if (hasSelection)
        {
            var sel = new Rect(area.x + selection.x * px, area.y + selection.y * px, selection.width * px, selection.height * px);
            Handles.DrawSolidRectangleWithOutline(sel, new Color(0.25f, 0.6f, 1f, 0.22f), new Color(0.35f, 0.7f, 1f, 1f));
        }
    }

    // 만들어질 프롭 모습 (조각을 합친 그림)
    private void DrawSelectionPreview()
    {
        List<Piece> pieces = BuildPieces();
        Bounds b = PiecesBounds(pieces);
        float unit = Mathf.Min(48f, 120f / Mathf.Max(b.size.x, b.size.y, 0.01f));
        Rect box = GUILayoutUtility.GetRect(b.size.x * unit + 8f, b.size.y * unit + 8f, GUILayout.ExpandWidth(false));
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        EditorGUI.DrawRect(box, new Color(0.12f, 0.12f, 0.12f, 1f));
        foreach (Piece p in pieces)
        {
            Rect sr = SpriteLocalRect(p.sprite);
            float x = p.position.x + sr.xMin - b.min.x;
            float yTop = b.max.y - (p.position.y + sr.yMax);
            TilePaletteView.DrawSprite(new Rect(box.x + 4f + x * unit, box.y + 4f + yTop * unit, sr.width * unit, sr.height * unit), p.sprite);
        }
    }

    // ---------------------------------------------------------------- 공통

    private void SuggestName(string name)
    {
        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrEmpty(name))
        {
            displayName = name;
        }
    }

    private string Problem()
    {
        if (source == Source.Single && sprite == null) return "그림을 골라 주세요.";
        if (source == Source.Sheet && selectedSprites.Count == 0) return "프롭으로 쓸 칸을 골라 주세요.";
        if (string.IsNullOrWhiteSpace(displayName)) return "이름을 입력해 주세요.";
        return null;
    }

    // 스프라이트 사각형 전체(투명 여백 포함)를 스프라이트 기준점 기준 로컬 좌표로
    private static Rect SpriteLocalRect(Sprite s)
    {
        float ppu = s.pixelsPerUnit;
        return new Rect(-s.pivot.x / ppu, -s.pivot.y / ppu, s.rect.width / ppu, s.rect.height / ppu);
    }

    // 고른 조각을 이미지 속 배치 그대로 놓고, 전체 중심이 프롭 중심(0,0)이 되게 한다.
    private List<Piece> BuildPieces()
    {
        var pieces = new List<Piece>();
        if (source == Source.Single)
        {
            if (sprite != null)
            {
                pieces.Add(new Piece { sprite = sprite, position = Vector2.zero });
            }
            return pieces;
        }

        if (selectedSprites.Count == 0)
        {
            return pieces;
        }

        Rect union = selectedSprites[0].rect;
        foreach (Sprite s in selectedSprites)
        {
            union = Rect.MinMaxRect(Mathf.Min(union.xMin, s.rect.xMin), Mathf.Min(union.yMin, s.rect.yMin),
                Mathf.Max(union.xMax, s.rect.xMax), Mathf.Max(union.yMax, s.rect.yMax));
        }
        foreach (Sprite s in selectedSprites)
        {
            float ppu = s.pixelsPerUnit;
            // 스프라이트 기준점의 이미지 속 위치 - 전체 중심
            Vector2 pivotInSheet = s.rect.position + s.pivot;
            pieces.Add(new Piece { sprite = s, position = (pivotInSheet - union.center) / ppu });
        }
        return pieces;
    }

    // 조각들의 실제 그림 영역(투명 부분 제외)을 합친 범위. 콜라이더 크기로 쓴다.
    private static Bounds PiecesBounds(List<Piece> pieces)
    {
        var b = new Bounds();
        bool first = true;
        foreach (Piece p in pieces)
        {
            Bounds sb = p.sprite.bounds;
            sb.center += (Vector3)p.position;
            if (first)
            {
                b = sb;
                first = false;
            }
            else
            {
                b.Encapsulate(sb);
            }
        }
        return b;
    }

    // 조각들의 불투명한 픽셀만 합친 범위. 투명 여백까지 부딪히지 않도록 콜라이더는 이 크기로 만든다.
    private static Bounds OpaqueBounds(List<Piece> pieces)
    {
        var pixelsByTexture = new Dictionary<Texture2D, (Color32[] pixels, int width)>();
        var b = new Bounds();
        bool first = true;
        foreach (Piece p in pieces)
        {
            Texture2D tex = p.sprite.texture;
            if (!pixelsByTexture.TryGetValue(tex, out var data))
            {
                // 텍스처가 읽기 가능하지 않을 수 있으므로 원본 파일에서 픽셀을 읽는다.
                var copy = new Texture2D(2, 2);
                copy.LoadImage(File.ReadAllBytes(Path.GetFullPath(AssetDatabase.GetAssetPath(tex))));
                data = (copy.GetPixels32(), copy.width);
                DestroyImmediate(copy);
                pixelsByTexture[tex] = data;
            }

            RectInt r = new RectInt((int)p.sprite.rect.x, (int)p.sprite.rect.y, (int)p.sprite.rect.width, (int)p.sprite.rect.height);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int y = r.yMin; y < r.yMax; y++)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    int i = y * data.width + x;
                    if (i >= 0 && i < data.pixels.Length && data.pixels[i].a > 0)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
                }
            }
            if (minX > maxX)
            {
                continue; // 완전히 투명한 조각
            }

            float ppu = p.sprite.pixelsPerUnit;
            Vector2 pivotPx = p.sprite.rect.position + p.sprite.pivot;
            Vector2 min = p.position + (new Vector2(minX, minY) - pivotPx) / ppu;
            Vector2 max = p.position + (new Vector2(maxX + 1, maxY + 1) - pivotPx) / ppu;
            var pb = new Bounds((min + max) * 0.5f, max - min);
            if (first)
            {
                b = pb;
                first = false;
            }
            else
            {
                b.Encapsulate(pb);
            }
        }
        return first ? PiecesBounds(pieces) : b;
    }

    // 컴퓨터의 PNG 를 프로젝트로 가져와 픽셀 아트 스프라이트로 설정한다.
    private static Sprite ImportSprite(string filePath)
    {
        string assetPath = CopyIntoProject(filePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = TileSheetImporter.DefaultCellSize;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // 컴퓨터의 PNG (또는 프로젝트 이미지의 복사본)를 가져와 칸 크기대로 나눈다.
    private static Texture2D ImportSheet(string filePath, int cell)
    {
        string assetPath = CopyIntoProject(filePath);
        TileSheetImporter.SliceSheet(assetPath, cell);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static string CopyIntoProject(string filePath)
    {
        EnsureFolder(ArtFolder);
        string name = TileSheetImporter.MakeSafeName(Path.GetFileNameWithoutExtension(filePath));
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{ArtFolder}/{name}.png");
        File.Copy(filePath, Path.GetFullPath(assetPath));
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        return assetPath;
    }

    private string Create()
    {
        PropCatalog catalog = LevelEditorTools.Catalog;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("새 프롭 만들기", "PropCatalog 에셋을 찾을 수 없어요.", "확인");
            return null;
        }

        List<Piece> pieces = BuildPieces();
        string id = "custom_" + GUID.Generate().ToString().Substring(0, 8);
        string fileName = TileSheetImporter.MakeSafeName(displayName);
        EnsureFolder(PropsFolder);
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{PropsFolder}/{fileName}.prefab");

        var root = new GameObject(fileName);
        Transform visualParent = root.transform;
        if (pieces.Count > 1)
        {
            visualParent = new GameObject("Visuals").transform;
            visualParent.SetParent(root.transform, false);
        }
        foreach (Piece p in pieces)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(visualParent, false);
            visual.transform.localPosition = p.position;
            visual.AddComponent<SpriteRenderer>().sprite = p.sprite;
        }

        if (kind != Kind.Decoration)
        {
            // 조각이 여러 개여도 부딪히는 모양은 하나로 만들어 한 덩어리로 움직이게 한다.
            var colliderGo = new GameObject("Collider");
            colliderGo.transform.SetParent(root.transform, false);
            Bounds b = OpaqueBounds(pieces);
            if (shape == Shape.Circle)
            {
                var circle = colliderGo.AddComponent<CircleCollider2D>();
                circle.radius = Mathf.Max(b.size.x, b.size.y) * 0.5f;
                circle.offset = b.center;
            }
            else
            {
                var box = colliderGo.AddComponent<BoxCollider2D>();
                box.size = b.size;
                box.offset = b.center;
            }
        }

        LevelProp prop;
        switch (kind)
        {
            case Kind.Physics:
                root.AddComponent<Rigidbody2D>();
                prop = root.AddComponent<PhysicsProp>();
                var so = new SerializedObject(prop);
                so.FindProperty("mass").floatValue = mass;
                so.ApplyModifiedPropertiesWithoutUndo();
                break;
            case Kind.Static:
                prop = root.AddComponent<StaticProp>();
                break;
            default:
                prop = root.AddComponent<DecorProp>();
                break;
        }
        prop.CatalogId = id;
        prop.ApplySettings();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        LevelEditorTools.AddCatalogEntry(id, displayName.Trim(), AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), description);
        return id;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
