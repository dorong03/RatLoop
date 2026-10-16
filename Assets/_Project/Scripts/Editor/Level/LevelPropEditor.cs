using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 모든 LevelProp 의 인스펙터.
// 위쪽에는 기획자 설정([PlannerField])만, 나머지는 접힌 "개발자 설정" 안에 보여준다.
[CustomEditor(typeof(LevelProp), true)]
[CanEditMultipleObjects]
public class LevelPropEditor : Editor
{
    private static bool showDeveloperSettings;
    private string[] excludedFromDeveloper;

    protected virtual void OnEnable()
    {
        var excluded = new List<string> { "m_Script" };
        excluded.AddRange(PlannerFieldUtility.GetPlannerFieldNames(target.GetType()));
        excludedFromDeveloper = excluded.ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("기획자 설정", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (PlannerFieldUtility.GetPlannerProperties(serializedObject).Count == 0)
            {
                EditorGUILayout.LabelField("조절할 값이 없습니다.", EditorStyles.miniLabel);
            }
            else
            {
                PlannerFieldUtility.DrawPlannerFields(serializedObject);
            }

            if (!serializedObject.isEditingMultipleObjects &&
                PrefabUtility.IsPartOfPrefabInstance(target) &&
                PlannerFieldUtility.HasPrefabOverrides(serializedObject))
            {
                if (GUILayout.Button("기본값으로 되돌리기"))
                {
                    PlannerFieldUtility.RevertToPrefabDefaults(serializedObject);
                }
            }
        }

        DrawValidation();

        EditorGUILayout.Space();
        showDeveloperSettings = EditorGUILayout.Foldout(showDeveloperSettings, "개발자 설정", true);
        if (showDeveloperSettings)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                DrawPropertiesExcluding(serializedObject, excludedFromDeveloper);
                EditorGUILayout.LabelField("카탈로그 ID", ((LevelProp)target).CatalogId ?? "-");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawValidation()
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            return;
        }

        var report = new PropValidationReport();
        ((LevelProp)target).Validate(report);
        foreach (PropValidationReport.Entry e in report.Entries)
        {
            EditorGUILayout.HelpBox(e.message,
                e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
        }
    }
}
