using System.Collections.Generic;
using UnityEngine;

// 게임에 나오는 레벨의 순서. 스테이지 번호 = 목록 순서 + 1.
// 스테이지 선택 화면은 이 목록으로 버튼을 만들고, 클리어하면 다음 순서가 열린다.
[CreateAssetMenu(fileName = "LevelList", menuName = "RatLoop/Level List")]
public class LevelList : ScriptableObject
{
    [SerializeField] private List<LevelRoot> levels = new List<LevelRoot>();

    public int Count => levels.Count;

    public LevelRoot Get(int index)
    {
        return index >= 0 && index < levels.Count ? levels[index] : null;
    }

    public int IndexOf(LevelRoot level)
    {
        return levels.IndexOf(level);
    }

    public void Validate(PropValidationReport report)
    {
        var seen = new HashSet<LevelRoot>();
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] == null)
            {
                report.Error($"레벨 목록 {i + 1}번이 비어 있습니다.", this);
            }
            else if (!seen.Add(levels[i]))
            {
                report.Error($"레벨 목록에 '{levels[i].name}' 가 두 번 들어 있습니다.", this);
            }
        }
    }
}
