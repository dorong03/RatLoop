using UnityEngine;

// 밀거나 쌓을 수 있는 물리 프롭 (공, 박스, 책, 병 ...).
// 여기 있는 값이 Rigidbody2D / Collider2D 의 원본이다. Rigidbody2D 를 직접 고치지 말고 이 값을 고친다.
[RequireComponent(typeof(Rigidbody2D))]
public class PhysicsProp : SizedProp
{
    [PlannerField("무게", 0)]
    [SerializeField, Range(0.1f, 200f)] private float mass = 1f;

    [PlannerField("중력", 1)]
    [SerializeField, Range(0f, 5f)] private float gravityScale = 1f;

    [PlannerField("이동 감속", 2)]
    [SerializeField, Range(0f, 10f)] private float linearDamping;

    [PlannerField("회전 감속", 3)]
    [SerializeField, Range(0f, 10f)] private float angularDamping = 0.05f;

    [PlannerField("마찰", 4)]
    [SerializeField, Range(0f, 1f)] private float friction = 0.4f;

    [PlannerField("튕김", 5)]
    [SerializeField, Range(0f, 1f)] private float bounciness;

    [PlannerField("회전 잠금", 6)]
    [SerializeField] private bool freezeRotation;

    [PlannerField("물에 뜨는 정도", 7)]
    [Tooltip("0이면 가라앉고 1이면 물의 기본 부력만큼 떠요. 무게와 상관없이 적용돼요.")]
    [SerializeField, Range(0f, 3f)] private float floatFactor = 1f;

    private PhysicsMaterial2D runtimeMaterial;

    public float FloatFactor => floatFactor;
    public override PropCategory Category => PropCategory.Physics;
    protected override string SortingLayerName => SortingLayers.Props;
    protected override string PhysicsLayerName => PhysicsLayers.Ground;

    public override void ApplySettings()
    {
        base.ApplySettings();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.useAutoMass = false;
        rb.mass = mass;
        rb.gravityScale = gravityScale;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.constraints = freezeRotation
            ? rb.constraints | RigidbodyConstraints2D.FreezeRotation
            : rb.constraints & ~RigidbodyConstraints2D.FreezeRotation;

        // 마찰/튕김은 프롭마다 다르므로 실행 중에만 개별 머티리얼을 만든다. (에셋을 만들지 않는다)
        if (Application.isPlaying)
        {
            if (runtimeMaterial == null)
            {
                runtimeMaterial = new PhysicsMaterial2D(name + "_Material");
            }
            runtimeMaterial.friction = friction;
            runtimeMaterial.bounciness = bounciness;
            rb.sharedMaterial = runtimeMaterial;
        }
    }

    public override void Validate(PropValidationReport report)
    {
        base.Validate(report);

        if (mass <= 0f)
        {
            report.Error($"{name}: 무게는 0보다 커야 해요.", this);
        }

        if (GetComponentsInChildren<Collider2D>(true).Length == 0)
        {
            report.Error($"{name}: 콜라이더가 없어서 다른 물체와 부딪히지 않아요.", this);
        }
    }

    // 기존 프리팹의 Rigidbody2D 값을 그대로 가져올 때 사용한다. (에디터 마이그레이션용)
    public void CopyFromRigidbody(Rigidbody2D rb, float defaultFriction, float defaultBounciness)
    {
        mass = rb.mass;
        gravityScale = rb.gravityScale;
        linearDamping = rb.linearDamping;
        angularDamping = rb.angularDamping;
        freezeRotation = (rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0;
        friction = defaultFriction;
        bounciness = defaultBounciness;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
