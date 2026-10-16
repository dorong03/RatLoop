using UnityEngine;

// 움직이지 않는 발판 (버스, 생수 묶음 ...). 밟을 수 있고 밀리지 않는다.
public class StaticProp : SizedProp
{
    public override PropCategory Category => PropCategory.Static;
    protected override string SortingLayerName => SortingLayers.Props;
    protected override string PhysicsLayerName => PhysicsLayers.Ground;

    public override void Validate(PropValidationReport report)
    {
        base.Validate(report);

        if (GetComponentsInChildren<Collider2D>(true).Length == 0)
        {
            report.Warning($"{name}: 콜라이더가 없어서 밟을 수 없어요. 장식이라면 장식 프롭으로 만들어 주세요.", this);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            report.Warning($"{name}: 고정 프롭에 움직이는 Rigidbody2D가 있어요. 물리 프롭으로 바꿔 주세요.", this);
        }
    }
}
