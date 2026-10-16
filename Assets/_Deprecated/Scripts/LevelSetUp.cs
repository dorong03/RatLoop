using UnityEngine;

// 구버전 레벨 씬(Assets/_Deprecated) 호환용. 레벨은 이제 Game 씬의 LevelLoader 가 불러온다.
public class LevelSetUp : MonoBehaviour
{
    [SerializeField] private LevelData levelData;
}
