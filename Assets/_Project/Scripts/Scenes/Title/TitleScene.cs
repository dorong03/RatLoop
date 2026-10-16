using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScene : MonoBehaviour
{
    private static bool IsAnimationPlayed;
    
    [SerializeField] private GameObject startImage;
    [SerializeField] private GameObject exitImage;
    [SerializeField] private UIAnimation animationUI;

    private bool selectedStart = true;
    private bool isLoading = false;

    // 도메인 리로드를 끈 상태에서도 게임을 새로 실행할 때마다 초기화되도록 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsAnimationPlayed = false;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (isLoading)
            return;

        // 설정 창이 열려 있으면 타이틀 메뉴는 입력을 받지 않는다.
        if (UIManager.instance != null && UIManager.instance.IsMenuOpen)
            return;

        Vector2Int nav = GameInput.NavigatePressed;
        if (nav.y > 0)
        {
            selectedStart = true;
            SoundManager.instance.PlaySFX(SfxType.TitleButtonChange);
            UpdateUI();
        }

        if (nav.y < 0)
        {
            selectedStart = false;
            SoundManager.instance.PlaySFX(SfxType.TitleButtonChange);
            UpdateUI();
        }

        if (GameInput.SubmitPressed)
        {
            SoundManager.instance.PlaySFX(SfxType.ButtonSelected);

            if (selectedStart)
            {
                isLoading = true;
                if (IsAnimationPlayed)
                {
                    SceneManager.LoadScene("LevelSelection");
                }
                else
                {
                    IsAnimationPlayed = true;
                    animationUI.PlayAnimation("LevelSelection");
                }
            }
            else
            {
                OnClickExit();
            }
        }
    }

    private void UpdateUI()
    {
        startImage.SetActive(selectedStart);
        exitImage.SetActive(!selectedStart);
    }

    public void OnClickExit()
    {
        Application.Quit();
    }
}