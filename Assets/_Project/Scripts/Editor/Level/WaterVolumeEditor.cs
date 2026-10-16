using UnityEditor;
using UnityEngine;

// 물 영역: 씬 뷰에서 오른쪽/위쪽 손잡이를 끌어 칸 단위로 크기를 바꾼다.
[CustomEditor(typeof(WaterVolume))]
[CanEditMultipleObjects]
public class WaterVolumeEditor : LevelPropEditor
{
    private void OnSceneGUI()
    {
        var water = (WaterVolume)target;
        Vector2Int size = water.Size;
        Vector3 origin = water.transform.position;

        Handles.color = new Color(0.3f, 0.8f, 1f);
        float handleSize = HandleUtility.GetHandleSize(origin) * 0.08f;

        Vector3 rightHandle = origin + new Vector3(size.x, size.y * 0.5f);
        Vector3 topHandle = origin + new Vector3(size.x * 0.5f, size.y);

        EditorGUI.BeginChangeCheck();
        Vector3 newRight = Handles.Slider(rightHandle, Vector3.right, handleSize, Handles.DotHandleCap, 1f);
        Vector3 newTop = Handles.Slider(topHandle, Vector3.up, handleSize, Handles.DotHandleCap, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(newRight.x - origin.x));
            int height = Mathf.Max(1, Mathf.RoundToInt(newTop.y - origin.y));

            serializedObject.Update();
            serializedObject.FindProperty("width").intValue = width;
            serializedObject.FindProperty("height").intValue = height;
            serializedObject.ApplyModifiedProperties();
            water.ApplySettings();
        }

        Handles.Label(origin + new Vector3(0f, size.y + 0.6f), $"물 {size.x} x {size.y}", EditorStyles.boldLabel);
    }
}
