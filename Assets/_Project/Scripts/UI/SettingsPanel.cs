using UnityEngine;
using UnityEngine.UI;

// 설정 화면. UIManager 프리팹의 SettingsPanel 오브젝트에 붙어 있다.
// 제목, 조작 안내, 항목의 글꼴과 위치는 프리팹에서 직접 고치고, 항목 문구와 색은 이 컴포넌트에서 고친다.
// 위/아래: 항목 선택, 좌/우: 값 조절, 확인: 실행, 취소/설정 키: 닫기
public class SettingsPanel : MonoBehaviour
{
    private enum Row { Bgm, Sfx, Screen, ResetProgress, Close }

    [Header("항목 (위에서부터 배경음, 효과음, 화면, 진행 초기화, 닫기)")]
    [SerializeField] private Text[] rows = new Text[5];

    [Header("항목 문구")]
    [SerializeField] private string bgmLabel = "배경음";
    [SerializeField] private string sfxLabel = "효과음";
    [SerializeField] private string screenLabel = "화면";
    [SerializeField] private string fullscreenText = "전체 화면";
    [SerializeField] private string windowedText = "창 모드";
    [SerializeField] private string resetLabel = "진행 초기화";
    [SerializeField] private string resetConfirmText = "정말 초기화할까요? (확인: 예 / 취소: 아니오)";
    [SerializeField] private string resetDoneText = "진행을 초기화했어요";
    [SerializeField] private string closeLabel = "닫기";

    [Header("선택 표시")]
    [SerializeField] private string selectedLeft = "> ";
    [SerializeField] private string selectedRight = " <";
    [SerializeField] private Color normalColor = new Color(0.196f, 0.196f, 0.196f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.85f, 0.42f, 0.08f, 1f);

    private int selected;
    private bool confirmingReset;

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
            selected = (selected - nav.y + rows.Length) % rows.Length;
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
                    rows[(int)Row.ResetProgress].text = resetDoneText;
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
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null)
            {
                continue;
            }
            bool isSelected = i == selected;
            rows[i].text = (isSelected ? selectedLeft : "") + Label((Row)i) + (isSelected ? selectedRight : "");
            rows[i].color = isSelected ? selectedColor : normalColor;
        }
    }

    private string Label(Row row)
    {
        switch (row)
        {
            case Row.Bgm: return $"{bgmLabel}   {Bar(GameSettings.BgmStep)}";
            case Row.Sfx: return $"{sfxLabel}   {Bar(GameSettings.SfxStep)}";
            case Row.Screen: return $"{screenLabel}   {(GameSettings.Fullscreen ? fullscreenText : windowedText)}";
            case Row.ResetProgress: return confirmingReset ? resetConfirmText : resetLabel;
            default: return closeLabel;
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
