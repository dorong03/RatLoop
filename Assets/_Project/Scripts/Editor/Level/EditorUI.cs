using System;
using UnityEditor;
using UnityEngine;

// 레벨 에디터 도구들이 함께 쓰는 화면 스타일과 그리기 도우미.
// 모든 창이 같은 글자 크기, 색, 버튼 모양을 쓰도록 여기서만 정한다.
//
// 화면 구성 원칙 (Unity 타일 팔레트, ProBuilder, LDtk 같은 레벨 도구를 참고)
//   - 위: 창 전체에 해당하는 버튼 (저장, 테스트 플레이 등)
//   - 가운데: 스크롤되는 본문. 지금 모드에 필요한 것만 보여준다.
//   - 아래: 스크롤되지 않는 바닥 줄. 지금 할 수 있는 일 안내나 '만들기' 같은 주요 버튼을 둔다.
//   - 관련된 기능은 한 줄이나 한 카드에 모으고, 지우기처럼 되돌리기 어려운 동작은 메뉴(⋮) 안에 둔다.
public static class EditorUI
{
    public enum ButtonKind { Normal, Primary, Play, Danger }

    public static readonly Color PrimaryColor = new Color(0.32f, 0.6f, 1f);
    public static readonly Color PlayColor = new Color(0.42f, 0.86f, 0.48f);
    public static readonly Color DangerColor = new Color(1f, 0.46f, 0.42f);
    public static readonly Color WarningColor = new Color(1f, 0.82f, 0.32f);
    public static readonly Color OkColor = new Color(0.5f, 0.88f, 0.55f);
    public static readonly Color SelectedColor = new Color(0.35f, 0.65f, 1f);

    private static GUIStyle title;
    private static GUIStyle section;
    private static GUIStyle caption;
    private static GUIStyle card;
    private static GUIStyle footer;
    private static GUIStyle banner;
    private static GUIStyle badge;
    private static GUIStyle placeholder;
    private static Texture2D footerTexture;

    private static Color TextGrey => EditorGUIUtility.isProSkin ? new Color(0.68f, 0.68f, 0.68f) : new Color(0.38f, 0.38f, 0.38f);

    // 창 제목 (레벨 이름, 창 이름)
    public static GUIStyle Title => title ?? (title = new GUIStyle(EditorStyles.boldLabel)
    {
        fontSize = 14,
        alignment = TextAnchor.MiddleLeft,
        clipping = TextClipping.Clip
    });

    // 카드 안 소제목
    public static GUIStyle Section => section ?? (section = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });

    // 설명 글 (회색, 줄바꿈)
    public static GUIStyle Caption => caption ?? (caption = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
    {
        fontSize = 11,
        normal = { textColor = TextGrey }
    });

    // 관련 항목을 묶는 상자
    public static GUIStyle Card => card ?? (card = new GUIStyle(EditorStyles.helpBox)
    {
        padding = new RectOffset(10, 10, 8, 9),
        margin = new RectOffset(4, 4, 3, 5)
    });

    // 창 맨 아래 고정 줄
    public static GUIStyle Footer => footer ?? (footer = new GUIStyle
    {
        padding = new RectOffset(10, 10, 7, 7),
        normal = { background = FooterTexture, textColor = TextGrey },
        fontSize = 11,
        wordWrap = true,
        alignment = TextAnchor.MiddleLeft
    });

    // 씬 화면 왼쪽 위 안내
    public static GUIStyle Banner => banner ?? (banner = new GUIStyle(EditorStyles.helpBox)
    {
        fontSize = 12,
        alignment = TextAnchor.MiddleLeft,
        padding = new RectOffset(10, 10, 5, 5),
        richText = true
    });

    // 분류 같은 짧은 꼬리표
    public static GUIStyle Badge => badge ?? (badge = new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleRight,
        normal = { textColor = TextGrey }
    });

    private static GUIStyle Placeholder => placeholder ?? (placeholder = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = new Color(TextGrey.r, TextGrey.g, TextGrey.b, 0.7f) },
        padding = new RectOffset(4, 0, 1, 0)
    });

    private static Texture2D FooterTexture
    {
        get
        {
            if (footerTexture == null)
            {
                footerTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                footerTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.78f, 0.78f, 0.78f));
                footerTexture.Apply();
            }
            return footerTexture;
        }
    }

    // ---------------------------------------------------------------- 묶음

    public static EditorGUILayout.VerticalScope CardScope() => new EditorGUILayout.VerticalScope(Card);

    // 카드 위에 붙는 제목과 설명
    public static void Header(string text, string description = null)
    {
        EditorGUILayout.Space(4);
        GUILayout.Label(text, Section);
        if (!string.IsNullOrEmpty(description))
        {
            GUILayout.Label(description, Caption);
        }
    }

    // 제목 오른쪽에 작은 버튼이 하나 붙는 줄. 버튼을 누르면 true.
    public static bool HeaderWithButton(string text, GUIContent button, float buttonWidth)
    {
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(text, Section);
            GUILayout.FlexibleSpace();
            return GUILayout.Button(button, EditorStyles.miniButton, GUILayout.Width(buttonWidth));
        }
    }

    public static void Separator()
    {
        EditorGUILayout.Space(4);
        Rect r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.25f));
        EditorGUILayout.Space(4);
    }

    // ---------------------------------------------------------------- 버튼

    public static bool Button(string text, ButtonKind kind = ButtonKind.Normal, params GUILayoutOption[] options)
    {
        return Button(new GUIContent(text), kind, options);
    }

    public static bool Button(GUIContent content, ButtonKind kind = ButtonKind.Normal, params GUILayoutOption[] options)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = KindColor(kind, old);
        bool clicked = GUILayout.Button(content, options);
        GUI.backgroundColor = old;
        return clicked;
    }

    public static bool ToolbarButton(GUIContent content, ButtonKind kind, float width)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = KindColor(kind, old);
        bool clicked = GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.Width(width));
        GUI.backgroundColor = old;
        return clicked;
    }

    private static Color KindColor(ButtonKind kind, Color normal)
    {
        switch (kind)
        {
            case ButtonKind.Primary: return PrimaryColor;
            case ButtonKind.Play: return PlayColor;
            case ButtonKind.Danger: return DangerColor;
            default: return normal;
        }
    }

    // ⋮ 버튼. 누르면 메뉴를 만들어 보여준다.
    public static void MenuButton(Action<GenericMenu> build, params GUILayoutOption[] options)
    {
        if (GUILayout.Button(Icon("_Menu", "", "더 보기"), options))
        {
            var menu = new GenericMenu();
            build(menu);
            menu.ShowAsContext();
        }
    }

    // ---------------------------------------------------------------- 기타

    // 에디터 기본 아이콘 + 글자. 어두운 테마에서는 밝은 아이콘(d_)을 쓴다.
    public static GUIContent Icon(string iconName, string text, string tooltip = null)
    {
        Texture icon = null;
        if (!string.IsNullOrEmpty(iconName))
        {
            if (EditorGUIUtility.isProSkin)
            {
                icon = EditorGUIUtility.FindTexture("d_" + iconName);
            }
            if (icon == null)
            {
                icon = EditorGUIUtility.FindTexture(iconName);
            }
        }
        string spaced = icon != null && !string.IsNullOrEmpty(text) ? " " + text : text;
        return new GUIContent(spaced, icon, tooltip);
    }

    // 상태 표시용 색 점이 붙은 글자
    public static void StatusLabel(string text, Color color, params GUILayoutOption[] options)
    {
        Color old = GUI.contentColor;
        GUI.contentColor = color;
        GUILayout.Label("●", EditorStyles.miniLabel, GUILayout.Width(12));
        GUI.contentColor = old;
        GUILayout.Label(text, EditorStyles.miniBoldLabel, options);
    }

    // 비어 있을 때 회색 안내 글이 보이는 입력칸
    public static string TextField(string value, string hint, params GUILayoutOption[] options)
    {
        string result = EditorGUILayout.TextField(value, options);
        if (string.IsNullOrEmpty(result) && Event.current.type == EventType.Repaint)
        {
            GUI.Label(GUILayoutUtility.GetLastRect(), hint, Placeholder);
        }
        return result;
    }

    // 바닥 고정 줄을 그린다. 내용은 draw 에서 그린다.
    public static void FooterBar(Action draw)
    {
        using (new EditorGUILayout.VerticalScope(Footer))
        {
            draw();
        }
    }
}
