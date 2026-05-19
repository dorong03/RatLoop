using UnityEngine;
using System.Collections;

public class InfoBreakedBlock : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private int breakHitCount = 3;
    [SerializeField] private float minUpwardVelocity = 0.5f;
    [SerializeField] private float bottomHitTolerance = 0.08f;
    [SerializeField] private string hitterTag = "Player";

    [Header("Bump Effect")]
    [SerializeField] private float bumpHeight = 0.22f;
    [SerializeField] private float bumpHeightPerHit = 0.12f;
    [SerializeField] private float bumpDuration = 0.1f;
    [SerializeField] private float settleDuration = 0.12f;

    [Header("Break Effect")]
    [SerializeField] private float breakDuration = 0.55f;
    [SerializeField] private float breakFlyDistance = 2.2f;
    [SerializeField] private float breakFlyAngle = 38f;
    [SerializeField] private float breakArcHeight = 0.8f;
    [SerializeField] private float breakSpinDegrees = 900f;
    [SerializeField] private float breakShrinkScale = 0.2f;
    [SerializeField] private bool destroyAfterBreak = true;

    private Collider2D blockCollider;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private int currentHitCount;
    private bool isAnimating;
    private bool isBroken;
    private Vector3 lastHitterWorldPosition;

    private void OnValidate()
    {
        breakHitCount = Mathf.Max(1, breakHitCount);
    }

    private void Awake()
    {
        blockCollider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }

        originalLocalPosition = transform.localPosition;
        originalLocalScale = transform.localScale;
    }

    private void Update()
    {
        if (!isBroken || !isAnimating)
        {
            Vector3 offset = new Vector3(0, 0, 0);
            offset.y = Mathf.Sin(Time.time * 2) * 0.2f;
            transform.position = originalLocalPosition + offset;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken || isAnimating || !IsHitter(collision))
        {
            return;
        }

        if (!IsBottomHit(collision))
        {
            return;
        }

        currentHitCount++;
        lastHitterWorldPosition = GetHitterPosition(collision);

        if (currentHitCount >= breakHitCount)
        {
            StartCoroutine(BreakRoutine());
        }
        else
        {
            StartCoroutine(BumpRoutine());
        }
    }

    private bool IsHitter(Collision2D collision)
    {
        if (collision.collider.CompareTag(hitterTag))
        {
            return true;
        }

        if (collision.rigidbody != null && collision.rigidbody.CompareTag(hitterTag))
        {
            return true;
        }

        return collision.transform.root.CompareTag(hitterTag);
    }

    private Vector3 GetHitterPosition(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            return collision.rigidbody.position;
        }

        return collision.transform.position;
    }

    private bool IsBottomHit(Collision2D collision)
    {
        if (blockCollider == null || collision.rigidbody == null)
        {
            return false;
        }

        if (collision.rigidbody.linearVelocity.y < minUpwardVelocity)
        {
            return false;
        }

        float bottomY = blockCollider.bounds.min.y;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.point.y <= bottomY + bottomHitTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator BumpRoutine()
    {
        isAnimating = true;

        float stackedBumpHeight = GetStackedBumpHeight();
        Vector3 bumpPosition = originalLocalPosition + Vector3.up * stackedBumpHeight;
        yield return MoveLocalPosition(originalLocalPosition, bumpPosition, bumpDuration);
        yield return MoveLocalPosition(bumpPosition, originalLocalPosition, settleDuration);

        isAnimating = false;
    }

    private IEnumerator BreakRoutine()
    {
        isBroken = true;
        isAnimating = true;

        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float flyDirectionX = lastHitterWorldPosition.x <= startPosition.x ? 1f : -1f;
        Vector3 launchDirection = Quaternion.Euler(0f, 0f, breakFlyAngle * flyDirectionX) * Vector3.right * flyDirectionX;
        Vector3 endPosition = startPosition + launchDirection.normalized * breakFlyDistance;
        float startPopHeight = GetStackedBumpHeight();

        float timer = 0f;
        while (timer < breakDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / breakDuration);
            float eased = EaseOutCubic(t);
            float fade = Mathf.Clamp01((t - 0.2f) / 0.8f);
            float scale = Mathf.Lerp(1f, breakShrinkScale, EaseInCubic(fade));

            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            position.y += Mathf.Sin(t * Mathf.PI) * breakArcHeight + startPopHeight * (1f - eased);

            transform.position = position;
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, breakSpinDegrees * eased * flyDirectionX);
            transform.localScale = originalLocalScale * scale;

            SetAlpha(1f - EaseInCubic(fade));
            yield return null;
        }

        SetAlpha(0f);

        if (destroyAfterBreak)
        {
            Destroy(gameObject);
            yield break;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator MoveLocalPosition(Vector3 from, Vector3 to, float duration)
    {
        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (timer < safeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / safeDuration);
            transform.localPosition = Vector3.Lerp(from, to, EaseOutCubic(t));
            yield return null;
        }

        transform.localPosition = to;
    }

    private float GetStackedBumpHeight()
    {
        return bumpHeight + bumpHeightPerHit * Mathf.Max(0, currentHitCount - 1);
    }

    private void SetAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color color = originalColors[i];
            color.a *= alpha;
            spriteRenderers[i].color = color;
        }
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }
}
