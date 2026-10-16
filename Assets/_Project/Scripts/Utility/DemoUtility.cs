using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DemoUtility : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            GameManager gm = GameManager.instance;
            if (gm.CurrentLevel != null && gm.CurrentLevelIndex >= 0 && gm.CurrentLevelIndex + 1 < gm.Levels.Count)
            {
                gm.EnterLevel(gm.CurrentLevelIndex + 1);
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
