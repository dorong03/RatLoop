using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleScene : MonoBehaviour
{
    [SerializeField] private GameObject startImage;
    [SerializeField] private GameObject exitImage;

    private bool selectedStart = true;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.wKey.wasPressedThisFrame)
        {
            selectedStart = true;
            UpdateUI();
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame ||
            Keyboard.current.sKey.wasPressedThisFrame)
        {
            selectedStart = false;
            UpdateUI();
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (selectedStart)
            {
                OnClickStart();
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

    public void OnClickStart()
    {
        SceneManager.LoadScene("LevelSelection");
    }

    public void OnClickExit()
    {
        Application.Quit();
    }
}