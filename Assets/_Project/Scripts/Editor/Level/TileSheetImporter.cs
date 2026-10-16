using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// 새 타일 이미지(시트)를 레벨 에디터 팔레트에 추가한다.
//  1. 이미지를 Assets/_Project/Art/Tiles/<이름>/ 으로 가져온다. (이미 프로젝트 안에 있으면 그 폴더로 옮긴다)
//  2. 픽셀 아트 설정(점 필터, 압축 없음)으로 바꾸고, 칸 크기대로 잘라 스프라이트를 만든다. (빈 칸은 건너뜀)
//  3. 스프라이트마다 타일 에셋을 만든다. 팔레트는 이 타일들을 이미지 배치 그대로 보여준다.
// 이미 잘려 있는 이미지는 다시 자르지 않고, 없는 타일만 만든다.
public static class TileSheetImporter
{
    public const int DefaultCellSize = 16;

    [MenuItem("RatLoop/타일/선택한 이미지로 타일 만들기")]
    private static void FromSelection()
    {
        int total = 0;
        foreach (Object obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                total += Process(AssetDatabase.GetAssetPath(obj), DefaultCellSize);
            }
        }
        EditorUtility.DisplayDialog("타일 만들기", $"타일 {total}개를 만들었습니다.", "확인");
    }

    [MenuItem("RatLoop/타일/선택한 이미지로 타일 만들기", true)]
    private static bool FromSelectionValidate()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                return true;
            }
        }
        return false;
    }

    // 컴퓨터에 있는 PNG 파일을 가져와 타일로 만든다. 만든 타일 수를 돌려준다.
    public static int ImportFromDisk(string filePath, int cellSize)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return 0;
        }

        string sheetName = MakeSafeName(Path.GetFileNameWithoutExtension(filePath));
        string folder = EnsureFolder(TilePaletteView.TilesRoot, sheetName);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{sheetName}.png");
        File.Copy(filePath, Path.GetFullPath(assetPath));
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        return Process(assetPath, cellSize);
    }

    public static int Process(string assetPath, int cellSize)
    {
        cellSize = Mathf.Max(1, cellSize);
        string sheetName = Path.GetFileNameWithoutExtension(assetPath);

        // 팔레트는 Art/Tiles 아래만 보므로 바깥에 있으면 옮긴다. (GUID 유지)
        if (!assetPath.StartsWith(TilePaletteView.TilesRoot + "/"))
        {
            string folder = EnsureFolder(TilePaletteView.TilesRoot, MakeSafeName(sheetName));
            string target = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Path.GetFileName(assetPath)}");
            string error = AssetDatabase.MoveAsset(assetPath, target);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("타일 이미지를 옮기지 못했습니다: " + error);
                return 0;
            }
            assetPath = target;
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null)
        {
            return 0;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = cellSize;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        SliceIfNeeded(importer, assetPath, sheetName, cellSize);
        return CreateTiles(assetPath);
    }

    // 이미지를 픽셀 아트 설정으로 바꾸고 칸 크기대로 자른다. (타일은 만들지 않는다. 프롭 조각용)
    public static void SliceSheet(string assetPath, int cellSize)
    {
        cellSize = Mathf.Max(1, cellSize);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = cellSize;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        SliceIfNeeded(importer, assetPath, Path.GetFileNameWithoutExtension(assetPath), cellSize);
    }

    private static void SliceIfNeeded(TextureImporter importer, string assetPath, string sheetName, int cellSize)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // 원본 파일에서 픽셀을 읽어 완전히 투명한 칸은 건너뛴다.
        var source = new Texture2D(2, 2);
        source.LoadImage(File.ReadAllBytes(Path.GetFullPath(assetPath)));
        Color32[] pixels = source.GetPixels32();
        int width = source.width, height = source.height;
        Object.DestroyImmediate(source);

        // 이미 칸 크기대로 잘려 있으면 그대로 둔다. (기존 타일이 참조하는 스프라이트를 바꾸지 않기 위해)
        // Multiple 로 바꾸면 Unity 가 이미지 전체를 스프라이트 1개로 잡아두는데, 이건 잘린 것으로 보지 않는다.
        SpriteRect[] existing = provider.GetSpriteRects();
        if (existing.Length > 0 && System.Array.TrueForAll(existing, r => IsGridCell(r.rect, cellSize)))
        {
            return;
        }

        var rects = new List<SpriteRect>();
        int index = 0;
        // 위쪽 줄부터 왼쪽에서 오른쪽으로 번호를 붙인다.
        for (int y = height - cellSize; y >= 0; y -= cellSize)
        {
            for (int x = 0; x + cellSize <= width; x += cellSize)
            {
                if (IsEmpty(pixels, width, x, y, cellSize))
                {
                    continue;
                }
                rects.Add(new SpriteRect
                {
                    name = $"{sheetName}_{index++}",
                    rect = new Rect(x, y, cellSize, cellSize),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                });
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIds != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (SpriteRect r in rects)
            {
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            }
            nameIds.SetNameFileIdPairs(pairs);
        }
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static bool IsGridCell(Rect r, int cellSize)
    {
        return Mathf.Approximately(r.width, cellSize) && Mathf.Approximately(r.height, cellSize) &&
               Mathf.Approximately(r.x % cellSize, 0f) && Mathf.Approximately(r.y % cellSize, 0f);
    }

    private static bool IsEmpty(Color32[] pixels, int width, int x0, int y0, int size)
    {
        for (int y = y0; y < y0 + size; y++)
        {
            for (int x = x0; x < x0 + size; x++)
            {
                if (pixels[y * width + x].a > 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // 스프라이트마다 타일 에셋을 만든다. 기존 타일과 같은 설정(충돌 모양 = 스프라이트)을 쓴다.
    private static int CreateTiles(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        int created = 0;
        foreach (Object obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
        {
            if (!(obj is Sprite sprite))
            {
                continue;
            }

            string tilePath = $"{folder}/{sprite.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<TileBase>(tilePath) != null)
            {
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Sprite;
            tile.flags = TileFlags.LockColor;
            AssetDatabase.CreateAsset(tile, tilePath);
            created++;
        }
        AssetDatabase.SaveAssets();
        return created;
    }

    private static string EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
        return path;
    }

    public static string MakeSafeName(string name)
    {
        var chars = new System.Text.StringBuilder();
        foreach (char c in name.Trim())
        {
            chars.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        }
        return chars.Length > 0 ? chars.ToString() : "Tiles";
    }
}
