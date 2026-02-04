using UnityEngine;
using UnityEditor;
 
public class ResetPlayerPrefs : MonoBehaviour
{
    [MenuItem("Window/PlayerPrefs 초기화")]
    private static void ResetPrefs()
    {
        PlayerPrefs.SetInt("ClearLevelIndex", 1);
    }
}