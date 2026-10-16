using UnityEngine;

// 투명벽. 눈에는 보이지 않지만 플레이어와 고스트가 지나가지 못한다.
// 칸 단위로 크기를 정하며 오브젝트 위치는 영역의 왼쪽 아래 모서리다. (WaterVolume 과 같은 방식)
// "물체도 막기" 를 켜면 박스, 공 같은 물리 프롭도 지나가지 못한다.
// 게임 화면에는 그려지지 않고, 에디터 씬 화면에만 반투명하게 보인다.
[RequireComponent(typeof(BoxCollider2D))]
public class InvisibleWall : LevelProp, IAreaProp
{
    [PlannerField("가로 칸 수", 0, PerInstance = true)]
    [SerializeField, Range(1, 64)] private int width = 1;

    [PlannerField("세로 칸 수", 1, PerInstance = true)]
    [SerializeField, Range(1, 64)] private int height = 4;

    [PlannerField("물체도 막기", 2)]
    [Tooltip("끄면 플레이어와 고스트만 막고 박스, 공 같은 물체는 통과한다.")]
    [SerializeField] private bool blockProps;

    public override PropCategory Category => PropCategory.Barrier;
    protected override string SortingLayerName => SortingLayers.Effects;
    protected override string PhysicsLayerName => blockProps ? PhysicsLayers.Barrier : PhysicsLayers.Wall;

    public Vector2Int AreaSize => new Vector2Int(width, height);
    public bool BlocksProps => blockProps;

    public override void ApplySettings()
    {
        SnapToGrid();
        ApplyDerived();
    }

    // 위치를 다시 맞추는 일은 실행 취소와 부딪히므로 값 변경 뒤에는 하지 않는다.
    protected override void OnEditorValueChanged()
    {
        ApplyDerived();
    }

    private void ApplyDerived()
    {
        base.ApplySettings();

        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = false;
        box.size = new Vector2(width, height);
        box.offset = new Vector2(width * 0.5f, height * 0.5f);
    }

    private void SnapToGrid()
    {
        Vector3 p = transform.localPosition;
        Vector3 snapped = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), p.z);
        if (snapped != p)
        {
            transform.localPosition = snapped;
        }
        if (transform.localRotation != Quaternion.identity)
        {
            transform.localRotation = Quaternion.identity;
        }
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }
    }

    public override void Validate(PropValidationReport report)
    {
        if (width < 1 || height < 1)
        {
            report.Error($"{name}: 투명벽 크기는 1칸 이상이어야 합니다.", this);
        }
    }

    private void OnDrawGizmos()
    {
        // 게임 화면에는 그려지지 않는 벽이라 에디터에서만 표시한다.
        Vector3 center = transform.position + new Vector3(width * 0.5f, height * 0.5f);
        Color fill = blockProps ? new Color(1f, 0.3f, 0.3f, 0.28f) : new Color(0.7f, 0.4f, 1f, 0.28f);
        Gizmos.color = fill;
        Gizmos.DrawCube(center, new Vector3(width, height, 0.1f));
        Gizmos.color = new Color(fill.r, fill.g, fill.b, 0.95f);
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
    }
}
