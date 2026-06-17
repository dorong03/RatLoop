using UnityEngine;
using UnityEngine.InputSystem;

public class TitleScene : MonoBehaviour
{
    [SerializeField] private GameObject startImage;
    [SerializeField] private GameObject exitImage;
    [SerializeField] private UIAnimation animationUI;

    private bool selectedStart = true;
    private bool isLoading = false;

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
                animationUI.PlayAnimation("LevelSelection");
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