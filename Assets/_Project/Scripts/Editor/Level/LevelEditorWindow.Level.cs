using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 레벨 탭: 레벨 이름, 제한 시간 같은 기본 설정과 배경(그림, 크기, 위치, 카메라를 따라 움직이는 정도).
public partial class LevelEditorWindow
{
    private const string BackgroundsFolder = "Assets/_Project/Art/Backgrounds";
    private const string BackgroundObjectName = "Background";

    private List<Sprite> backgroundSprites;

    // Art/Backgrounds 안의 배경 그림들. 프로젝트가 바뀌면 다시 찾는다. (OnProjectChange)
    private List<Sprite> BackgroundSprites
    {
        get
        {
            if (backgroundSprites == null)
            {
                backgroundSprites = new List<Sprite>();
                foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { BackgroundsFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (o is Sprite s)
                        {
                            backgroundSprites.Add(s);
                        }
                    }
                }
                backgroundSprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            }
            return backgroundSprites;
        }
    }

    private void DrawLevelMode(LevelRoot level)
    {
        EditorUI.Header("기본 설정");
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

        DrawBackgroundSettings(level);
    }

    private static SpriteRenderer FindBackground(LevelRoot level)
    {
        Transform t = level.transform.Find(BackgroundObjectName);
        return t != null ? t.GetComponent<SpriteRenderer>() : null;
    }

    private void DrawBackgroundSettings(LevelRoot level)
    {
        if (EditorUI.HeaderWithButton("배경", EditorUI.Icon("Toolbar Plus", "배경 그림 추가", "PC의 PNG를 배경 그림으로 가져와요"), 112))
        {
            ImportBackground(level);
        }

        SpriteRenderer sr = FindBackground(level);
        if (sr == null)
        {
            using (EditorUI.CardScope())
            {
                GUILayout.Label("이 레벨에는 배경이 없어요.", EditorUI.Caption);
                if (GUILayout.Button("배경 만들기"))
                {
                    sr = CreateBackground(level);
                    Selection.activeGameObject = sr.gameObject;
                }
            }
            return;
        }

        // 그림 고르기: 누르면 바로 바뀐다.
        using (EditorUI.CardScope())
        {
            GUILayout.Label("그림을 누르면 바로 바뀌어요.", EditorUI.Caption);
            EditorGUILayout.Space(2);
            if (BackgroundSprites.Count == 0)
            {
                GUILayout.Label($"배경 그림이 없어요. '배경 그림 추가'로 넣어 주세요. ({BackgroundsFolder})", EditorUI.Caption);
            }
            foreach (Sprite s in BackgroundSprites)
            {
                DrawBackgroundCell(sr, s);
            }
        }

        // 크기, 위치, 움직임
        using (EditorUI.CardScope())
        {
            Transform t = sr.transform;
            EditorGUI.BeginChangeCheck();
            float size = EditorGUILayout.Slider("크기", Mathf.Abs(t.localScale.y), 0.25f, 4f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "배경 크기 바꾸기");
                t.localScale = new Vector3(Mathf.Sign(t.localScale.x) * size, size, t.localScale.z);
            }

            EditorGUI.BeginChangeCheck();
            Vector2 pos = EditorGUILayout.Vector2Field("위치", new Vector2(t.localPosition.x, t.localPosition.y));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "배경 위치 바꾸기");
                t.localPosition = new Vector3(pos.x, pos.y, t.localPosition.z);
            }

            if (sr.TryGetComponent(out ParallaxSprite parallax))
            {
                var pso = new SerializedObject(parallax);
                pso.Update();
                SerializedProperty effect = pso.FindProperty("parallaxEffect");
                if (effect != null)
                {
                    effect.floatValue = EditorGUILayout.Slider("카메라 따라 움직임", effect.floatValue, -1f, 1f);
                    pso.ApplyModifiedProperties();
                    GUILayout.Label("0이면 제자리에 있어요. 1에 가까울수록 카메라를 따라 천천히 움직여서 멀리 있는 것처럼 보여요.", EditorUI.Caption);
                }
            }

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("찾기", "씬 화면에서 배경을 보여줘요"), GUILayout.Width(52)))
                {
                    SelectAndFrame(sr.gameObject);
                }
            }
        }
    }

    // 배경 그림 한 줄: 넓은 그림이므로 창 폭에 맞춘 미리 보기와 이름
    private void DrawBackgroundCell(SpriteRenderer sr, Sprite s)
    {
        float width = Mathf.Max(120f, position.width - 52f);
        float aspect = s.rect.width / Mathf.Max(1f, s.rect.height);
        float height = Mathf.Clamp(width / aspect, 36f, 90f);
        Rect r = GUILayoutUtility.GetRect(width, height + 4f, GUILayout.ExpandWidth(true), GUILayout.Height(height + 4f));
        bool current = sr.sprite == s;
        bool hover = r.Contains(Event.current.mousePosition);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(r, current ? new Color(0.25f, 0.55f, 1f, 0.55f) : hover ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.15f));
            var inner = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
            TilePaletteView.DrawSprite(FitRect(inner, s), s);
            var label = new GUIContent(current ? $"{s.name} · 지금 배경" : s.name);
            Vector2 size = EditorStyles.miniBoldLabel.CalcSize(label);
            var labelRect = new Rect(inner.x + 4f, inner.yMax - size.y - 3f, size.x + 8f, size.y + 2f);
            EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(labelRect.x + 4f, labelRect.y + 1f, size.x, size.y), label, EditorStyles.whiteMiniLabel);
        }

        if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !current)
        {
            SetBackground(sr, s);
        }
    }

    private void SetBackground(SpriteRenderer sr, Sprite s)
    {
        Undo.RecordObject(sr, "배경 바꾸기");
        sr.sprite = s;
        SceneView.RepaintAll();
        ShowNotification(new GUIContent($"배경을 바꿨어요 · {s.name}"));
    }

    private static SpriteRenderer CreateBackground(LevelRoot level)
    {
        var go = new GameObject(BackgroundObjectName);
        Undo.RegisterCreatedObjectUndo(go, "배경 만들기");
        go.transform.SetParent(level.transform, false);
        go.transform.SetAsFirstSibling();
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = SortingLayers.Background;
        go.AddComponent<ParallaxSprite>();
        return sr;
    }

    // PC의 PNG를 Art/Backgrounds 로 가져와 배경용(한 장, 픽셀 아트) 설정을 하고 지금 레벨에 바로 적용한다.
    private void ImportBackground(LevelRoot level)
    {
        string file = EditorUtility.OpenFilePanel("배경 그림 고르기 (PNG)", "", "png");
        if (string.IsNullOrEmpty(file))
        {
            return;
        }

        string name = TileSheetImporter.MakeSafeName(Path.GetFileNameWithoutExtension(file));
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{BackgroundsFolder}/{name}.png");
        File.Copy(file, Path.GetFullPath(assetPath));
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

        backgroundSprites = null;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        SpriteRenderer sr = FindBackground(level) ?? CreateBackground(level);
        if (sprite != null)
        {
            SetBackground(sr, sprite);
        }
        GUIUtility.ExitGUI();
    }
}
