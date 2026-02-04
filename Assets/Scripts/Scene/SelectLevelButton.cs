using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelButton : MonoBehaviour
{
    private int level;
    
    [SerializeField] private Text levelText;
    [SerializeField] private Button _button;

    public void SetUp(int level)
    {
        this.level = level;
        levelText.text = level.ToString();
        _button.onClick.AddListener(OnClickLevelButton);
    }

    public void OnClickLevelButton()
    {
        string sceneName = "Level_" + level.ToString();

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);    
        }
        else
        {
            Debug.Log($"{level} 레벨 씬이 존재하지 않음");
        }
    }
}
