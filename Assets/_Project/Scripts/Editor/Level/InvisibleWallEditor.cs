using UnityEditor;
using UnityEngine;

// 투명벽: 씬 뷰의 손잡이를 끌어 칸 단위로 크기를 바꾼다.
[CustomEditor(typeof(InvisibleWall))]
[CanEditMultipleObjects]
public class InvisibleWallEditor : LevelPropEditor
{
    private void OnSceneGUI()
    {
        var wall = (InvisibleWall)target;
        AreaHandles.Draw(wall, serializedObject, wall, wall.BlocksProps ? new Color(1f, 0.4f, 0.4f) : new Color(0.8f, 0.5f, 1f), "투명벽");
    }
}
