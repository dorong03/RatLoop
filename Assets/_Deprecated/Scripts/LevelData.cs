using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelID;
    public int timeLimit;
    public int maxReplyCount;
    public LevelData nextLevelData;
}
