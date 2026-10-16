using System;

// 기획자용 레벨 에디터와 인스펙터의 "기획자 설정" 영역에 노출할 필드에 붙인다.
// PropertyAttribute 가 아니므로 [Range], [Tooltip] 과 함께 쓸 수 있다.
//
//   [PlannerField("질량", order: 0)]
//   [SerializeField, Range(0.1f, 200f)] private float mass = 1f;
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class PlannerFieldAttribute : Attribute
{
    public string Label { get; }
    public int Order { get; }

    public PlannerFieldAttribute(string label, int order = 0)
    {
        Label = label;
        Order = order;
    }
}
