using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelButton : MonoBehaviour
{
    private int level;
    
    [SerializeField] private Text levelText;
    [SerializeField] private Button _button;
    [SerializeField] private Image lockImage;

    public void SetUp(int level)
    {
        this.level = level;
        levelText.text = level.ToString();
        _button.onClick.AddListener(OnClickLevelButton);
        bool isUnlocked = GameManager.instance.clearLevelIndex >= level;
        lockImage.gameObject.SetActive(!isUnlocked);
    }

    public void OnClickLevelButton()
    {
        GameManager.instance.OpenLevel(level);
    }
}
