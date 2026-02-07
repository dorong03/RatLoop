using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelButton : MonoBehaviour
{
    private int level;
    
    [SerializeField] private Text levelText;
    [SerializeField] private Button _button;
    [SerializeField] private Image lockImage;

    private bool isUnlocked;

    public void SetUp(int level)
    {
        this.level = level;
        levelText.text = level.ToString();
        _button.onClick.AddListener(OnClickLevelButton);
        isUnlocked = GameManager.instance.clearLevelIndex >= level;
        if (GameManager.instance.clearLevelIndex.Equals(level))
        {
            EventSystem.current.SetSelectedGameObject(_button.gameObject);
        }
        lockImage.gameObject.SetActive(!isUnlocked);
    }

    public void OnClickLevelButton()
    {
        if (!isUnlocked)
        {
            return;
        }
        GameManager.instance.OpenLevel(level);
    }
}
