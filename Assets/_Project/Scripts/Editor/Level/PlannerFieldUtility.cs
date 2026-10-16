using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// [PlannerField] 가 붙은 필드를 찾아 그린다. 인스펙터와 레벨 에디터 창이 같이 쓴다.
//
// 값은 두 종류다.
//  - 종류 공통 값: 프리팹(종류)에 저장된다. 레벨에 놓인 프롭을 골라도 프리팹 원본을 고치므로
//                 같은 프롭은 모든 레벨에서 항상 같은 값을 가진다.
//  - 개체별 값 (PerInstance): 물 크기, 로프 길이처럼 놓은 자리마다 다른 값. 레벨의 개체에 저장된다.
public static class PlannerFieldUtility
{
    public readonly struct PlannerProperty
    {
        public readonly SerializedProperty property;
        public readonly GUIContent label;
        public readonly int order;
        public readonly bool perInstance;

        public PlannerProperty(SerializedProperty property, GUIContent label, int order, bool perInstance)
        {
            this.property = property;
            this.label = label;
            this.order = order;
            this.perInstance = perInstance;
        }
    }

    public enum Scope { All, Shared, PerInstance }

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

    private static bool InScope(PlannerFieldAttribute attr, Scope scope)
    {
        return scope == Scope.All || (scope == Scope.PerInstance) == attr.PerInstance;
    }

    public static List<PlannerProperty> GetPlannerProperties(SerializedObject serializedObject, Scope scope = Scope.All)
    {
        var result = new List<PlannerProperty>();
        if (serializedObject == null || serializedObject.targetObject == null)
        {
            return result;
        }

        foreach (var (field, attr) in GetFields(serializedObject.targetObject.GetType()))
        {
            if (!InScope(attr, scope))
            {
                continue;
            }
            SerializedProperty prop = serializedObject.FindProperty(field.Name);
            if (prop == null)
            {
                continue;
            }

            string tooltip = field.GetCustomAttribute<TooltipAttribute>()?.tooltip;
            result.Add(new PlannerProperty(prop, new GUIContent(attr.Label, tooltip), attr.Order, attr.PerInstance));
        }
        return result;
    }

    public static bool HasFields(Type type, Scope scope)
    {
        return GetFields(type).Exists(f => InScope(f.attr, scope));
    }

    public static string[] GetPlannerFieldNames(Type type, Scope scope = Scope.All)
    {
        return GetFields(type).FindAll(f => InScope(f.attr, scope)).ConvertAll(f => f.field.Name).ToArray();
    }

    // 필드를 그리고, 하나라도 바뀌면 true.
    public static bool DrawPlannerFields(SerializedObject serializedObject, Scope scope = Scope.All)
    {
        EditorGUI.BeginChangeCheck();
        foreach (PlannerProperty p in GetPlannerProperties(serializedObject, scope))
        {
            EditorGUILayout.PropertyField(p.property, p.label, true);
        }
        return EditorGUI.EndChangeCheck();
    }

    // ------------------------------------------------------------ 종류 공통 값 (프리팹)

    // 레벨에 놓인 프롭의 "종류" = 그 프롭을 만든 프리팹 원본의 컴포넌트.
    // 프리팹으로 만든 게 아니면(예: 레벨에 직접 만든 시작 위치) 그 개체 자신을 돌려준다.
    public static Component GetTypeSource(Component instance)
    {
        if (instance == null)
        {
            return null;
        }
        if (EditorUtility.IsPersistent(instance) || !PrefabUtility.IsPartOfPrefabInstance(instance))
        {
            return instance;
        }
        Component source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        return source != null ? source : instance;
    }

    public static bool IsEditingType(Component instance)
    {
        return GetTypeSource(instance) != instance;
    }

    // 종류 공통 값을 그린다. 고치면 프리팹 원본에 저장되어 모든 레벨의 같은 프롭에 적용된다.
    public static bool DrawSharedFields(Component instance)
    {
        Component source = GetTypeSource(instance);
        var so = new SerializedObject(source);
        so.Update();
        bool changed = DrawPlannerFields(so, Scope.Shared);
        if (changed)
        {
            so.ApplyModifiedProperties();
            // 레벨에 놓은 것을 고쳤든 팔레트에서 프리팹을 직접 고쳤든, 원본의 크기 같은 파생 값까지 맞춰 저장한다.
            if (source != instance || EditorUtility.IsPersistent(source))
            {
                SaveTypeSource(source);
            }
        }
        return changed;
    }

    public static void SaveTypeSource(Component source)
    {
        if (source is LevelProp prop)
        {
            // 프리팹 원본의 Rigidbody2D 같은 파생 값도 맞춘다. (로프 마디처럼 씬이 필요한 작업은 프롭이 알아서 건너뜀)
            prop.ApplySettings();
        }
        EditorUtility.SetDirty(source);
        EditorUtility.SetDirty(source.transform);
        AssetDatabase.SaveAssetIfDirty(source);
    }

    // 개체에 종류 공통 값이 따로 바뀌어 있는지 (종류 설정과 다른 값을 쓰고 있는지)
    public static List<string> GetSharedOverrides(Component instance)
    {
        var result = new List<string>();
        if (!IsEditingType(instance))
        {
            return result;
        }

        var so = new SerializedObject(instance);
        foreach (PlannerProperty p in GetPlannerProperties(so, Scope.Shared))
        {
            if (p.property.prefabOverride)
            {
                result.Add(p.label.text);
            }
        }
        return result;
    }

    // 개체에 따로 바뀐 종류 공통 값을 없애서 종류 설정을 따르게 한다.
    public static void RevertSharedOverrides(Component instance)
    {
        var so = new SerializedObject(instance);
        foreach (PlannerProperty p in GetPlannerProperties(so, Scope.Shared))
        {
            if (p.property.prefabOverride)
            {
                PrefabUtility.RevertPropertyOverride(p.property, InteractionMode.UserAction);
            }
        }
    }
}
