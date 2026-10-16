using System;

// 기획자용 레벨 에디터와 인스펙터의 "기획자 설정" 영역에 노출할 필드에 붙인다.
// PropertyAttribute 가 아니므로 [Range], [Tooltip] 과 함께 쓸 수 있다.
//
// 기본은 "종류 공통" 값이다. 프리팹(종류)에 저장되어, 같은 프롭은 모든 레벨에서 항상 같은 값을 가진다.
// 물 크기, 로프 길이처럼 놓은 자리마다 달라야 하는 값만 PerInstance = true 로 표시한다.
//
//   [PlannerField("질량", 0)]                              // 종류 공통 (프리팹에서 한 번에 설정)
//   [PlannerField("가로 칸 수", 0, PerInstance = true)]     // 놓은 자리마다 따로
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class PlannerFieldAttribute : Attribute
{
    public string Label { get; }
    public int Order { get; }

    // true 면 레벨에 놓인 개체마다 따로 정하는 값, false 면 같은 종류 전체가 함께 쓰는 값
    public bool PerInstance { get; set; }

    public PlannerFieldAttribute(string label, int order = 0)
    {
        Label = label;
        Order = order;
    }
}
