using UnityEngine;
using UnityEditor;
 
public class ResetPlayerPrefs : MonoBehaviour
{
    [MenuItem("Window/PlayerPrefs Init")]
    private static void ResetPrefs()
    {
        PlayerPrefs.SetInt("ClearLevelIndex", 1);
    }
}