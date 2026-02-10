using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelButton : MonoBehaviour
{
    private LevelData levelData;
    
    [SerializeField] private Text levelText;
    [SerializeField] private Button _button;
    [SerializeField] private Image lockImage;

    private bool isUnlocked;

    public void SetUp(LevelData levelData)
    {
        this.levelData = levelData;
        levelText.text = levelData.levelID.ToString();
        _button.onClick.AddListener(OnClickLevelButton);
        isUnlocked = GameManager.instance.maxClearLevelID >= levelData.levelID;
        if (isUnlocked)
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
        GameManager.instance.EnterLevel(levelData);
    }
}
