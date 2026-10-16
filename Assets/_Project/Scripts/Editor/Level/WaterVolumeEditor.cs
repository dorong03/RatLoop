using UnityEditor;
using UnityEngine;

// 물 영역: 씬 뷰의 손잡이를 끌어 칸 단위로 크기를 바꾼다.
[CustomEditor(typeof(WaterVolume))]
[CanEditMultipleObjects]
public class WaterVolumeEditor : LevelPropEditor
{
    private void OnSceneGUI()
    {
        AreaHandles.Draw((WaterVolume)target, serializedObject, (WaterVolume)target, new Color(0.3f, 0.8f, 1f), "물");
    }
}
