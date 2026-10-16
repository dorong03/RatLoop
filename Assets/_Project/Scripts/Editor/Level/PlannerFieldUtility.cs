using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// [PlannerField] 가 붙은 필드를 찾아 그린다. 인스펙터와 레벨 에디터 창이 같이 쓴다.
public static class PlannerFieldUtility
{
    public readonly struct PlannerProperty
    {
        public readonly SerializedProperty property;
        public readonly GUIContent label;
        public readonly int order;

        public PlannerProperty(SerializedProperty property, GUIContent label, int order)
        {
            this.property = property;
            this.label = label;
            this.order = order;
        }
    }

    private static readonly Dictionary<Type, List<(FieldInfo field, PlannerFieldAttribute attr)>> Cache =
        new Dictionary<Type, List<(FieldInfo, PlannerFieldAttribute)>>();

    // 베이스 클래스 필드가 먼저, 같은 클래스 안에서는 order 순서.
    private static List<(FieldInfo field, PlannerFieldAttribute attr)> GetFields(Type type)
    {
        if (Cache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var hierarchy = new List<Type>();
        for (Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
        {
            hierarchy.Insert(0, t);
        }

        var result = new List<(FieldInfo, PlannerFieldAttribute)>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (Type t in hierarchy)
        {
            var declared = new List<(FieldInfo, PlannerFieldAttribute)>();
            foreach (FieldInfo field in t.GetFields(flags))
            {
                var attr = field.GetCustomAttribute<PlannerFieldAttribute>();
                if (attr != null)
                {
                    declared.Add((field, attr));
                }
            }
            declared.Sort((a, b) => a.Item2.Order.CompareTo(b.Item2.Order));
            result.AddRange(declared);
        }

        Cache[type] = result;
        return result;
    }

    public static List<PlannerProperty> GetPlannerProperties(SerializedObject serializedObject)
    {
        var result = new List<PlannerProperty>();
        if (serializedObject.targetObject == null)
        {
            return result;
        }

        foreach (var (field, attr) in GetFields(serializedObject.targetObject.GetType()))
        {
            SerializedProperty prop = serializedObject.FindProperty(field.Name);
            if (prop == null)
            {
                continue;
            }

            string tooltip = field.GetCustomAttribute<TooltipAttribute>()?.tooltip;
            result.Add(new PlannerProperty(prop, new GUIContent(attr.Label, tooltip), attr.Order));
        }
        return result;
    }

    public static string[] GetPlannerFieldNames(Type type)
    {
        return GetFields(type).ConvertAll(f => f.field.Name).ToArray();
    }

    // 기획자 필드를 그리고, 하나라도 바뀌면 true.
    public static bool DrawPlannerFields(SerializedObject serializedObject)
    {
        EditorGUI.BeginChangeCheck();
        foreach (PlannerProperty p in GetPlannerProperties(serializedObject))
        {
            EditorGUILayout.PropertyField(p.property, p.label, true);
        }
        return EditorGUI.EndChangeCheck();
    }

    public static bool HasPrefabOverrides(SerializedObject serializedObject)
    {
        foreach (PlannerProperty p in GetPlannerProperties(serializedObject))
        {
            if (p.property.prefabOverride)
            {
                return true;
            }
        }
        return false;
    }

    // 프리팹(카탈로그 기본값)으로 되돌린다.
    public static void RevertToPrefabDefaults(SerializedObject serializedObject)
    {
        foreach (PlannerProperty p in GetPlannerProperties(serializedObject))
        {
            if (p.property.prefabOverride)
            {
                PrefabUtility.RevertPropertyOverride(p.property, InteractionMode.UserAction);
            }
        }
    }
}
