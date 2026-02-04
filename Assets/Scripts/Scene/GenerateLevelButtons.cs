using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GenerateLevelButtons : MonoBehaviour
{
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Transform buttonsGrid;
    
    private void Start()
    {
        int levelCount = 0;
        int totalScenes = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < totalScenes; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (sceneName.StartsWith("Level_"))
            {
                levelCount++;
            }
        }

        for (int i = 1; i <= levelCount; i++)
        {
            GameObject levelButton = Instantiate(levelButtonPrefab, buttonsGrid);
            SelectLevelButton selcButton = levelButton.GetComponent<SelectLevelButton>();
            
            selcButton.SetUp(i);
        }
    }
}
