using System.IO;
using UnityEditor;
using UnityEngine;

// 새 프롭 만들기 (코드 없이 이미지로 프롭 추가)
// 이미지와 종류만 고르면 프리팹, 콜라이더, 레이어, 카탈로그 등록까지 자동으로 한다.
// 특수한 동작이 필요한 기믹은 개발자가 LevelProp 을 상속해 만든다. (Docs/AddingContent.md)
public class NewPropWindow : EditorWindow
{
    private enum Kind { Physics, Static, Decoration }
    private enum Shape { Box, Circle }

    private static readonly string[] KindLabels = { "물리 프롭 (밀고 쌓기)", "고정 프롭 (밟는 발판)", "장식 (통과)" };
    private static readonly string[] ShapeLabels = { "사각형", "원" };
    private const string PropsFolder = "Assets/_Project/Prefabs/Props/Custom";
    private const string ArtFolder = "Assets/_Project/Art/Props/Custom";

    private Sprite sprite;
    private string displayName = "";
    private Kind kind;
    private Shape shape;
    private float mass = 3f;
    private string description = "";
    private System.Action<string> onCreated;

    public static void Open(System.Action<string> created)
    {
        var w = GetWindow<NewPropWindow>(true, "새 프롭 만들기", true);
        w.minSize = w.maxSize = new Vector2(380f, 470f);
        w.onCreated = created;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("1. 이미지", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            sprite = (Sprite)EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Width(72), GUILayout.Height(72));
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("프로젝트의 스프라이트를 끌어 놓거나,", EditorStyles.miniLabel);
                if (GUILayout.Button("컴퓨터에서 PNG 고르기..."))
                {
                    string path = EditorUtility.OpenFilePanel("프롭 이미지", "", "png");
                    if (!string.IsNullOrEmpty(path))
                    {
                        sprite = ImportSprite(path);
                        if (string.IsNullOrEmpty(displayName) && sprite != null)
                        {
                            displayName = Path.GetFileNameWithoutExtension(path);
                        }
                    }
                    GUIUtility.ExitGUI();
                }
                if (sprite != null)
                {
                    Vector2 size = sprite.bounds.size;
                    EditorGUILayout.LabelField($"크기: {size.x:0.##} x {size.y:0.##} 칸", EditorStyles.miniLabel);
                }
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("2. 이름과 종류", EditorStyles.boldLabel);
        displayName = EditorGUILayout.TextField("이름", displayName);
        kind = (Kind)GUILayout.SelectionGrid((int)kind, KindLabels, 1);
        if (kind != Kind.Decoration)
        {
            shape = (Shape)GUILayout.Toolbar((int)shape, ShapeLabels);
            EditorGUILayout.LabelField("부딪히는 모양입니다. 둥근 물건은 '원'을 고르세요.", EditorStyles.miniLabel);
        }
        if (kind == Kind.Physics)
        {
            mass = EditorGUILayout.Slider("무게", mass, 0.5f, 100f);
            EditorGUILayout.LabelField("마찰, 튕김, 물에 뜨는 정도는 만든 뒤 '선택·이동'에서 바꿀 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
        }
        description = EditorGUILayout.TextField("설명 (선택)", description);

        EditorGUILayout.Space(10);
        string problem = Problem();
        if (problem != null)
        {
            EditorGUILayout.HelpBox(problem, MessageType.Info);
        }
        using (new EditorGUI.DisabledScope(problem != null))
        {
            if (GUILayout.Button("만들기", GUILayout.Height(30)))
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

    private string Problem()
    {
        if (sprite == null) return "이미지를 골라주세요.";
        if (string.IsNullOrWhiteSpace(displayName)) return "이름을 써주세요.";
        return null;
    }

    // 컴퓨터의 PNG 를 프로젝트로 가져와 픽셀 아트 스프라이트로 설정한다.
    private static Sprite ImportSprite(string filePath)
    {
        EnsureFolder(ArtFolder);
        string name = TileSheetImporter.MakeSafeName(Path.GetFileNameWithoutExtension(filePath));
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{ArtFolder}/{name}.png");
        File.Copy(filePath, Path.GetFullPath(assetPath));
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

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

    private string Create()
    {
        PropCatalog catalog = LevelEditorTools.Catalog;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("새 프롭 만들기", "PropCatalog 에셋을 찾을 수 없습니다.", "확인");
            return null;
        }

        string id = "custom_" + GUID.Generate().ToString().Substring(0, 8);
        string fileName = TileSheetImporter.MakeSafeName(displayName);
        EnsureFolder(PropsFolder);
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{PropsFolder}/{fileName}.prefab");

        var root = new GameObject(fileName);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.AddComponent<SpriteRenderer>().sprite = sprite;

        if (kind != Kind.Decoration)
        {
            var colliderGo = new GameObject("Collider");
            colliderGo.transform.SetParent(root.transform, false);
            Bounds b = sprite.bounds;
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
