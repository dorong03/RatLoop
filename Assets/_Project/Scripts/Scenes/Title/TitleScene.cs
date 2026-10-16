using UnityEngine;
using UnityEngine.InputSystem;
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

        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.wKey.wasPressedThisFrame)
        {
            selectedStart = true;
            SoundManager.instance.PlaySFX(SfxType.TitleButtonChange);
            UpdateUI();
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame ||
            Keyboard.current.sKey.wasPressedThisFrame)
        {
            selectedStart = false;
            SoundManager.instance.PlaySFX(SfxType.TitleButtonChange);
            UpdateUI();
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
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