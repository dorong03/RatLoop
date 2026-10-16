using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 설정 화면. UIManager 가 기존 패널(FinalClearPanel)을 복제해서 만든다. (같은 배경과 폰트 사용)
// 위/아래: 항목 선택, 좌/우: 값 조절, 확인: 실행, 취소/설정 키: 닫기
public class SettingsPanel : MonoBehaviour
{
    private enum Row { Bgm, Sfx, Screen, ResetProgress, Close }

    private static readonly Color NormalColor = new Color(0.196f, 0.196f, 0.196f, 1f);
    private static readonly Color SelectedColor = new Color(0.85f, 0.42f, 0.08f, 1f);

    private readonly List<Text> rows = new List<Text>();
    private Text hint;
    private int selected;
    private bool confirmingReset;

    // template 의 첫 번째 Text 를 제목으로, 두 번째 Text 를 항목 모양으로 쓴다.
    public static SettingsPanel Create(GameObject template, Transform parent)
    {
        GameObject go = Instantiate(template, parent);
        go.name = "SettingsPanel";
        go.SetActive(false);

        Text[] texts = go.GetComponentsInChildren<Text>(true);
        Text title = texts[0];
        Text rowTemplate = texts[1];
        title.text = "설정";

        var panel = go.AddComponent<SettingsPanel>();
        int rowCount = System.Enum.GetValues(typeof(Row)).Length;
        Vector2 basePos = rowTemplate.rectTransform.anchoredPosition;
        for (int i = 0; i < rowCount; i++)
        {
            Text row = i == 0 ? rowTemplate : Instantiate(rowTemplate, rowTemplate.transform.parent);
            Setup(row, basePos + new Vector2(0f, -70f * i));
            panel.rows.Add(row);
        }

        panel.hint = Instantiate(rowTemplate, rowTemplate.transform.parent);
        Setup(panel.hint, basePos + new Vector2(0f, -70f * rowCount - 40f));
        panel.hint.fontSize = Mathf.RoundToInt(rowTemplate.fontSize * 0.6f);
        panel.hint.text = "위/아래: 선택    좌/우: 조절    확인(Enter): 실행    취소(Esc): 닫기";
        return panel;
    }

    private static void Setup(Text text, Vector2 position)
    {
        text.rectTransform.anchoredPosition = position;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.alignment = TextAnchor.MiddleCenter;
    }

    private void OnEnable()
    {
        selected = 0;
        confirmingReset = false;
        Refresh();
    }

    // UIManager 가 이 패널이 맨 위에 있을 때 매 프레임 호출한다. 닫아야 하면 true.
    public bool HandleInput(bool closeOnCancel)
    {
        if (GameInput.SettingsPressed)
        {
            return true;
        }

        if (GameInput.CancelPressed)
        {
            if (confirmingReset)
            {
                confirmingReset = false;
                Refresh();
                return false;
            }
            return closeOnCancel;
        }

        Vector2Int nav = GameInput.NavigatePressed;
        if (nav.y != 0 && !confirmingReset)
        {
            selected = (selected - nav.y + rows.Count) % rows.Count;
            PlayMoveSound();
            Refresh();
        }
        else if (nav.x != 0)
        {
            Adjust(nav.x);
        }

        if (GameInput.SubmitPressed)
        {
            return Submit();
        }
        return false;
    }

    private void Adjust(int direction)
    {
        switch ((Row)selected)
        {
            case Row.Bgm: GameSettings.SetBgmStep(GameSettings.BgmStep + direction); break;
            case Row.Sfx: GameSettings.SetSfxStep(GameSettings.SfxStep + direction); PlayMoveSound(); break;
            case Row.Screen: GameSettings.SetFullscreen(!GameSettings.Fullscreen); break;
            default: return;
        }
        Refresh();
    }

    private bool Submit()
    {
        switch ((Row)selected)
        {
            case Row.Screen:
                GameSettings.SetFullscreen(!GameSettings.Fullscreen);
                break;
            case Row.ResetProgress:
                if (confirmingReset)
                {
                    GameManager.instance.ResetProgress();
                    confirmingReset = false;
                    rows[(int)Row.ResetProgress].text = "진행을 초기화했습니다";
                    return false;
                }
                confirmingReset = true;
                break;
            case Row.Close:
                return true;
        }
        Refresh();
        return false;
    }

    private void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            bool isSelected = i == selected;
            rows[i].text = (isSelected ? "> " : "") + Label((Row)i) + (isSelected ? " <" : "");
            rows[i].color = isSelected ? SelectedColor : NormalColor;
        }
    }

    private string Label(Row row)
    {
        switch (row)
        {
            case Row.Bgm: return $"배경음   {Bar(GameSettings.BgmStep)}";
            case Row.Sfx: return $"효과음   {Bar(GameSettings.SfxStep)}";
            case Row.Screen: return $"화면   {(GameSettings.Fullscreen ? "전체 화면" : "창 모드")}";
            case Row.ResetProgress: return confirmingReset ? "정말 초기화할까요? (확인: 예 / 취소: 아니오)" : "진행 초기화";
            default: return "닫기";
        }
    }

    private static string Bar(int step)
    {
        return new string('|', step) + new string('.', GameSettings.VolumeSteps - step) + $"  {step * 10}%";
    }

    private static void PlayMoveSound()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(SfxType.TitleButtonChange);
        }
    }
}
