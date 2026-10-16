using UnityEditor;
using UnityEngine;

// 칸 단위 영역 프롭(물, 투명벽)의 크기를 씬 화면에서 끌어서 바꾸는 손잡이.
// 오른쪽 손잡이는 가로, 위쪽 손잡이는 세로, 오른쪽 위 손잡이는 둘 다 바꾼다.
public static class AreaHandles
{
    public static void Draw(Component target, SerializedObject serializedObject, IAreaProp area, Color color, string label)
    {
        Vector2Int size = area.AreaSize;
        Vector3 origin = target.transform.position;
        float handleSize = HandleUtility.GetHandleSize(origin) * 0.08f;

        Handles.color = color;
        Vector3 right = origin + new Vector3(size.x, size.y * 0.5f);
        Vector3 top = origin + new Vector3(size.x * 0.5f, size.y);
        Vector3 corner = origin + new Vector3(size.x, size.y);

        EditorGUI.BeginChangeCheck();
        Vector3 newRight = Handles.Slider(right, Vector3.right, handleSize, Handles.DotHandleCap, 1f);
        Vector3 newTop = Handles.Slider(top, Vector3.up, handleSize, Handles.DotHandleCap, 1f);
        Vector3 newCorner = Handles.FreeMoveHandle(corner, handleSize * 1.4f, Vector3.one, Handles.RectangleHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(newRight.x, newCorner.x) - origin.x));
            int height = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(newTop.y, newCorner.y) - origin.y));
            if (width != size.x || height != size.y)
            {
                serializedObject.Update();
                serializedObject.FindProperty("width").intValue = width;
                serializedObject.FindProperty("height").intValue = height;
                serializedObject.ApplyModifiedProperties();
                if (target is LevelProp prop)
                {
                    prop.ApplySettings();
                }
            }
        }

        Handles.Label(origin + new Vector3(0f, size.y + 0.6f), $"{label} {size.x} x {size.y}", EditorStyles.boldLabel);
    }
}
