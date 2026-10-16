using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DemoUtility : MonoBehaviour
{
    // 개발용 단축키 (L: 다음 레벨, P: 처음부터). 에디터와 개발 빌드에서만 동작한다.
    void Update()
    {
        if (!Debug.isDebugBuild || Keyboard.current == null)
        {
            return;
        }

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
