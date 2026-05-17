using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DemoUtility : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (GameManager.instance.currentLevelData != null)
            {
                LevelData currentLevelData = GameManager.instance.currentLevelData;
                if (currentLevelData.nextLevelData != null)
                {
                    GameManager.instance.EnterLevel(currentLevelData.nextLevelData);
                }
            }
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Destroy(GameManager.instance.gameObject);
            Destroy(UIManager.instance.gameObject);
            Destroy(SoundManager.instance.gameObject);
            Destroy(DataCollector.instance.gameObject);
            Destroy(DataSaver.instance.gameObject);

            SceneManager.LoadScene(0);
        }
    }
}
