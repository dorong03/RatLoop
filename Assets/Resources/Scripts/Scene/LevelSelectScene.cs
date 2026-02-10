using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectScene : MonoBehaviour
{
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Transform buttonsGrid;
    
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Title"); 
        }
    }
    
    private void Start()
    {
        List<LevelData> allLevels = GameManager.instance.SortedLevelData;
        
        foreach(var data in allLevels)
        {
            GameObject levelButton = Instantiate(levelButtonPrefab, buttonsGrid);
            SelectLevelButton selcButton = levelButton.GetComponent<SelectLevelButton>();
            selcButton.SetUp(data);
        }
    }
}
