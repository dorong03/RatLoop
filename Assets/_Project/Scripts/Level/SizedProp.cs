using UnityEngine;

// 크기를 바꿀 수 있는 프롭 (물리, 고정, 장식).
// 크기는 종류 공통 값이라 같은 프롭은 모든 레벨에서 같은 크기다.
// 좌우 뒤집기는 놓은 자리마다 다르므로 따로 저장하고, 오브젝트 크기(localScale)는 여기서 계산해 넣는다.
public abstract class SizedProp : LevelProp
{
    [PlannerField("크기", -100)]
    [Tooltip("1이 원래 크기예요. 그림과 부딪히는 모양이 함께 커지고 작아져요.")]
    [SerializeField, Range(0.25f, 4f)] private float size = 1f;

    // 크기 1 일 때의 오브젝트 크기 (프리팹을 만들 때의 크기)
    [SerializeField, HideInInspector] private Vector2 baseScale;

    [SerializeField, HideInInspector] private bool flipX;

    public float Size => size;

    public bool FlipX
    {
        get => flipX;
        set => flipX = value;
    }

    public override void ApplySettings()
    {
        base.ApplySettings();
        ApplyScale();
    }

    private void ApplyScale()
    {
        Vector3 current = transform.localScale;
        if (baseScale == Vector2.zero)
        {
            // 크기 기능이 생기기 전에 만든 프롭: 지금 크기를 원래 크기로 삼는다.
            baseScale = new Vector2(Mathf.Abs(current.x), Mathf.Abs(current.y)) / Mathf.Max(size, 0.01f);
            flipX = current.x < 0f;
        }

        Vector3 scale = new Vector3(baseScale.x * size * (flipX ? -1f : 1f), baseScale.y * size, current.z);
        if (current != scale)
        {
            transform.localScale = scale;
        }
    }

    public override void Validate(PropValidationReport report)
    {
        if (size <= 0f)
        {
            report.Error($"{name}: 크기는 0보다 커야 해요.", this);
        }
    }
}
