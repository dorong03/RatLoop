using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 모든 LevelProp 의 인스펙터.
//  - 종류 공통 설정: 프리팹(종류)에 저장. 레벨에 놓인 개체를 골라도 프리팹 원본을 고친다.
//  - 이 자리 설정: 물 크기, 로프 길이처럼 놓은 자리마다 다른 값.
//  - 개발자 설정: [PlannerField] 가 없는 나머지 값 (접혀 있음)
[CustomEditor(typeof(LevelProp), true)]
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
        var prop = (LevelProp)target;
        bool editingType = PlannerFieldUtility.IsEditingType(prop);

        if (PlannerFieldUtility.HasFields(prop.GetType(), PlannerFieldUtility.Scope.Shared))
        {
            EditorGUILayout.LabelField(editingType ? "종류 공통 설정 (모든 레벨의 같은 프롭에 적용)" : "기획자 설정", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PlannerFieldUtility.DrawSharedFields(prop);

                List<string> overrides = PlannerFieldUtility.GetSharedOverrides(prop);
                if (overrides.Count > 0)
                {
                    EditorGUILayout.HelpBox($"이 자리의 프롭만 값이 다릅니다: {string.Join(", ", overrides)}", MessageType.Warning);
                    if (GUILayout.Button("종류 설정으로 맞추기"))
                    {
                        PlannerFieldUtility.RevertSharedOverrides(prop);
                    }
                }
            }
        }

        serializedObject.Update();
        if (PlannerFieldUtility.HasFields(prop.GetType(), PlannerFieldUtility.Scope.PerInstance))
        {
            EditorGUILayout.LabelField("이 자리 설정", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PlannerFieldUtility.DrawPlannerFields(serializedObject, PlannerFieldUtility.Scope.PerInstance);
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
                EditorGUILayout.LabelField("카탈로그 ID", prop.CatalogId ?? "-");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawValidation()
    {
        var report = new PropValidationReport();
        ((LevelProp)target).Validate(report);
        foreach (PropValidationReport.Entry e in report.Entries)
        {
            EditorGUILayout.HelpBox(e.message,
                e.severity == PropValidationReport.Severity.Error ? MessageType.Error : MessageType.Warning);
        }
    }
}
