using UnityEngine;

// 부딪히지 않는 장식. 공중에 떠 있는 광고판처럼 플레이어가 그냥 지나간다.
public class DecorProp : LevelProp
{
    [PlannerField("둥둥 떠다니기", 0)]
    [SerializeField] private bool bobbing;

    [PlannerField("떠다니는 높이", 1)]
    [SerializeField, Range(0f, 2f)] private float bobHeight = 0.2f;

    [PlannerField("떠다니는 속도", 2)]
    [SerializeField, Range(0f, 5f)] private float bobSpeed = 1.5f;

    private Vector3 startPosition;

    public override PropCategory Category => PropCategory.Decoration;
    protected override string SortingLayerName => SortingLayers.Decoration;
    protected override string PhysicsLayerName => PhysicsLayers.Decoration;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!bobbing)
        {
            return;
        }

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * offset;
    }

    public override void Validate(PropValidationReport report)
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (!col.isTrigger)
            {
                report.Warning($"{name}: 장식 프롭에 콜라이더가 있습니다. Decoration 레이어라 충돌하지는 않습니다.", this);
                break;
            }
        }
    }
}
