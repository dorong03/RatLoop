using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ToiletPaperController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CircleCollider2D circleCollider;

    [Header("Sprites")]
    [SerializeField] private Sprite[] stages;

    [Header("Collider Radius")]
    [SerializeField] private float[] colliderRadius;

    [Header("Consume")]
    [SerializeField] private float distancePerStage = 3f;

    [Header("Roll Visual")]
    [SerializeField] private Transform rollTransform;
    [SerializeField] private SpriteRenderer trailRenderer;
    [SerializeField] private float maxUnrollDistance = 5f;
    [SerializeField] private float minRollScale = 0.3f;

    private Vector3 lastPosition;
    private float movedDistance;
    private int currentStage;

    private bool hasStages;
    private bool hasRollVisual;

    private void Reset()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();

        if (distancePerStage <= 0f)
        {
            distancePerStage = 0.01f;
        }

        if (maxUnrollDistance <= 0f)
        {
            maxUnrollDistance = 0.01f;
        }

        minRollScale = Mathf.Clamp01(minRollScale);
    }

    private void Start()
    {
        CacheReferences();
        lastPosition = transform.position;
        hasStages = spriteRenderer != null && stages != null && stages.Length > 0;
        hasRollVisual = rollTransform != null || trailRenderer != null;

        if (hasStages)
        {
            currentStage = Mathf.Clamp(currentStage, 0, stages.Length - 1);
            ApplyStage();
        }
    }

    private void Update()
    {
        if (!hasStages && !hasRollVisual)
        {
            return;
        }

        float delta = Vector3.Distance(transform.position, lastPosition);

        movedDistance += delta;
        lastPosition = transform.position;

        if (hasStages)
        {
            while (movedDistance >= distancePerStage)
            {
                movedDistance -= distancePerStage;
                ConsumePaper();
            }
        }

        if (hasRollVisual)
        {
            ApplyRollVisual();
        }
    }

    private void ConsumePaper()
    {
        if (stages == null || stages.Length == 0 || currentStage >= stages.Length - 1)
        {
            return;
        }

        currentStage++;
        ApplyStage();
    }

    private void ApplyStage()
    {
        if (spriteRenderer == null || stages == null || stages.Length == 0)
        {
            return;
        }

        currentStage = Mathf.Clamp(currentStage, 0, stages.Length - 1);
        spriteRenderer.sprite = stages[currentStage];

        if (circleCollider != null && colliderRadius != null && currentStage < colliderRadius.Length)
        {
            circleCollider.radius = colliderRadius[currentStage];
        }
    }

    private void ApplyRollVisual()
    {
        float unrollRatio = Mathf.Clamp01(movedDistance / maxUnrollDistance);

        if (rollTransform != null)
        {
            float scale = Mathf.Lerp(1f, minRollScale, unrollRatio);
            rollTransform.localScale = new Vector3(scale, scale, rollTransform.localScale.z);
        }

        if (trailRenderer != null)
        {
            trailRenderer.size = new Vector2(Mathf.Max(0.01f, unrollRatio * maxUnrollDistance), trailRenderer.size.y);
        }
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (circleCollider == null)
        {
            circleCollider = GetComponent<CircleCollider2D>();
        }
    }
}
