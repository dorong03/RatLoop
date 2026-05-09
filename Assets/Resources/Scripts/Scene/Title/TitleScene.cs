using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    private void Start()
    {
        startButton.onClick.AddListener(OnClickStart);
        exitButton.onClick.AddListener(OnClickExit);
    }
    
    public void OnClickStart()
    {
        SceneManager.LoadScene("LevelSelction");
    }

    public void OnClickExit()
    {
        Application.Quit();
    }
}
