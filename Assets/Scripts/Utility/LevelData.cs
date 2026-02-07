using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelIndex;
    public float timeLimit;
    public int maxLives;
}
