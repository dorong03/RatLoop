using UnityEngine;

public class LevelSetUp : MonoBehaviour
{
    [Header("레벨 데이터 넣기")]
    [SerializeField] private LevelData levelData;

    private void Start()
    {
        if (GameManager.instance.currentLevelData == null)
        {
            if (levelData != null)
            {
                GameManager.instance.EnterLevel(levelData);
            }
            else
            {
                Debug.LogError("레벨 데이터 넣어!!");
            }
        }
    }
}
