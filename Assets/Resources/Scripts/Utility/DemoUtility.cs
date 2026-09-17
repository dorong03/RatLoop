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

            if (gm.currentLevelData != null &&
                gm.currentLevelData.nextLevelData != null)
            {
                var nextLevel = gm.currentLevelData.nextLevelData;

                // 이동할 스테이지까지 해금
                if (nextLevel.levelID > gm.maxClearLevelID)
                {
                    gm.maxClearLevelID = nextLevel.levelID;

                    // 게임을 껐다 켜도 해금 상태 유지
                    PlayerPrefs.SetInt("MaxClearLevelID", gm.maxClearLevelID);
                    PlayerPrefs.Save();
                }

                // 다음 스테이지로 이동
                gm.EnterLevel(nextLevel);
            }
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Destroy(GameManager.instance.gameObject);
            Destroy(UIManager.instance.gameObject);
            Destroy(SoundManager.instance.gameObject);
            Destroy(DataCollector.instance.gameObject);
            Destroy(DataSaver.instance.gameObject);
            Time.timeScale = 1f;
            PlayerPrefs.DeleteKey("MaxClearLevelID");
            SceneManager.LoadScene(0);
        }
    }
}
