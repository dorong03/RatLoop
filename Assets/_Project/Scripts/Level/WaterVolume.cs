using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 물 영역. 칸 단위 크기만 정하면 그림(타일), 판정(트리거), 수면 높이가 한 번에 맞춰진다.
// 오브젝트 위치는 영역의 왼쪽 아래 모서리이며 격자 칸에 맞춰진다.
// 부력은 BuoyancyEffector2D 대신 직접 계산해서, 프롭마다 질량과 별개로 뜨는 정도를 줄 수 있다.
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class WaterVolume : LevelProp
{
    private const string PlayerTag = "Player";
    private const string GhostTag = "Ghost";
    private const string VisualName = "Visual";

    [System.Serializable]
    public class WaterTiles
    {
        // 3x3 가장자리 타일. [0..2] 윗줄(수면), [3..5] 가운데, [6..8] 아랫줄. 각 줄은 왼쪽/가운데/오른쪽.
        public TileBase[] tiles = new TileBase[9];
        public Color color = new Color(1f, 1f, 1f, 0.647f);

        public TileBase Get(int row, int column) => tiles != null && tiles.Length == 9 ? tiles[row * 3 + column] : null;
    }

    [PlannerField("가로 칸 수", 0)]
    [SerializeField, Range(1, 64)] private int width = 5;

    [PlannerField("세로 칸 수", 1)]
    [SerializeField, Range(1, 32)] private int height = 3;

    [PlannerField("부력", 2)]
    [Tooltip("1 이면 물체가 반쯤 잠긴 채 뜬다. 클수록 더 높이 뜬다.")]
    [SerializeField, Range(0f, 5f)] private float buoyancy = 1f;

    [PlannerField("물속 이동 저항", 3)]
    [SerializeField, Range(0f, 20f)] private float linearDrag = 10.46f;

    [PlannerField("물속 회전 저항", 4)]
    [SerializeField, Range(0f, 20f)] private float angularDrag = 5f;

    [PlannerField("물살 방향(도)", 5)]
    [SerializeField, Range(-180f, 180f)] private float flowAngle;

    [PlannerField("물살 세기", 6)]
    [SerializeField, Range(0f, 50f)] private float flowStrength;

    [PlannerField("플레이어 익사 시간(초)", 7)]
    [Tooltip("0 이면 빠져도 죽지 않는다.")]
    [SerializeField, Range(0f, 10f)] private float drownTime = 3f;

    [PlannerField("타일 자동 배치", 8)]
    [Tooltip("끄면 크기를 바꿔도 물 타일을 다시 깔지 않는다. (직접 그린 물 그림을 보존할 때)")]
    [SerializeField] private bool autoTiles = true;

    [Header("그림")]
    [SerializeField] private WaterTiles visualTiles = new WaterTiles();

    // 물 밖으로 잠깐 튀어나갔다 들어오는 경우를 익사 판정에서 무시하는 시간
    private const float ExitGraceTime = 0.15f;
    // 기존 BuoyancyEffector 밀도(2)와 같은 기준. buoyancy 1 일 때 반쯤 잠긴 채 뜬다.
    private const float BaseDensity = 2f;

    private static readonly List<Collider2D> ColliderBuffer = new List<Collider2D>();
    private static readonly List<Collider2D> OverlapBuffer = new List<Collider2D>();
    private readonly HashSet<Rigidbody2D> bodies = new HashSet<Rigidbody2D>();
    private readonly HashSet<int> splashedObjects = new HashSet<int>();
    private BoxCollider2D area;
    private bool isPlayerInWater;
    private int playerEnterCount;
    private Coroutine exitGraceCoroutine;

    public override PropCategory Category => PropCategory.Water;
    protected override string SortingLayerName => SortingLayers.Water;
    protected override string PhysicsLayerName => PhysicsLayers.Water;

    public Vector2Int Size => new Vector2Int(width, height);
    public float SurfaceY => transform.position.y + height;

    // 기존 레벨 변환용: 직접 그린 물 그림을 그대로 쓰고 자동 배치를 끈다.
    public void UseCustomVisual(Color color)
    {
        autoTiles = false;
        visualTiles.color = color;
    }

    public Tilemap VisualTilemap => GetComponentInChildren<Tilemap>(true);

    public void SetSize(int newWidth, int newHeight)
    {
        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);
        ApplySettings();
    }

    public override void ApplySettings()
    {
        SnapToGrid();
        base.ApplySettings();

        gameObject.tag = "Water";

        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.usedByEffector = false;
        area.size = new Vector2(width, height);
        area.offset = new Vector2(width * 0.5f, height * 0.5f);

        RebuildVisual();
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

    private void RebuildVisual()
    {
        Tilemap tilemap = GetComponentInChildren<Tilemap>(true);
        if (tilemap == null)
        {
            return;
        }

        tilemap.color = visualTiles.color;
        if (!autoTiles)
        {
            return;
        }

        tilemap.ClearAllTiles();
        for (int y = 0; y < height; y++)
        {
            int row = y == height - 1 ? 0 : (y == 0 ? 2 : 1);
            for (int x = 0; x < width; x++)
            {
                int column = width == 1 ? 1 : (x == 0 ? 0 : (x == width - 1 ? 2 : 1));
                tilemap.SetTile(new Vector3Int(x, y, 0), visualTiles.Get(row, column));
            }
        }
    }

    public override void Validate(PropValidationReport report)
    {
        if (width < 1 || height < 1)
        {
            report.Error($"{name}: 물 크기는 1칸 이상이어야 합니다.", this);
        }

        for (int i = 0; i < 9; i++)
        {
            if (visualTiles.tiles == null || visualTiles.tiles.Length != 9 || visualTiles.tiles[i] == null)
            {
                report.Warning($"{name}: 물 타일이 비어 있어 일부 칸이 보이지 않습니다.", this);
                break;
            }
        }
    }

    // ---------------------------------------------------------------- 부력

    // 물속 물체는 트리거가 아니라 영역 겹침 검사로 찾는다.
    // 충돌 매트릭스에서 Water 와 Ground(프롭) 레이어가 서로 감지하지 않기 때문이다.
    private void CollectBodies()
    {
        bodies.Clear();
        if (area == null)
        {
            area = GetComponent<BoxCollider2D>();
        }

        var filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = ~LayerMask.GetMask(PhysicsLayers.Water, PhysicsLayers.Decoration)
        };
        OverlapBuffer.Clear();
        Physics2D.OverlapBox(area.bounds.center, area.bounds.size, 0f, filter, OverlapBuffer);
        foreach (Collider2D col in OverlapBuffer)
        {
            Rigidbody2D rb = col.attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                bodies.Add(rb);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        CollectBodies();
        if (bodies.Count == 0)
        {
            return;
        }

        float surfaceY = SurfaceY;
        Vector2 flow = Quaternion.Euler(0f, 0f, flowAngle) * Vector2.right * flowStrength;

        foreach (Rigidbody2D rb in bodies)
        {

            float submerged = GetSubmergedFraction(rb, surfaceY);
            if (submerged <= 0f)
            {
                continue;
            }

            PhysicsProp prop = rb.GetComponent<PhysicsProp>();
            float floatFactor = prop != null ? prop.FloatFactor : 1f;

            // 잠긴 비율만큼 중력 반대 방향으로 민다. 1 / (BaseDensity * buoyancy * floatFactor) 만큼 잠긴 지점에서 균형.
            Vector2 gravity = Physics2D.gravity * rb.gravityScale;
            Vector2 lift = -gravity * (rb.mass * BaseDensity * buoyancy * floatFactor * submerged);
            rb.AddForce(lift + flow * submerged);

            float dt = Time.fixedDeltaTime;
            rb.linearVelocity *= 1f / (1f + linearDrag * submerged * dt);
            rb.angularVelocity *= 1f / (1f + angularDrag * submerged * dt);
        }
    }

    private static float GetSubmergedFraction(Rigidbody2D rb, float surfaceY)
    {
        bool hasBounds = false;
        Bounds bounds = default;
        int count = rb.attachedColliderCount;
        if (count == 0)
        {
            return 0f;
        }

        ColliderBuffer.Clear();
        rb.GetAttachedColliders(ColliderBuffer);
        foreach (Collider2D col in ColliderBuffer)
        {
            if (col.isTrigger)
            {
                continue;
            }
            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!hasBounds || bounds.size.y <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01((surfaceY - bounds.min.y) / bounds.size.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (other.CompareTag(PlayerTag))
        {
            OnPlayerEnter(other);
        }
        else if (other.CompareTag(GhostTag))
        {
            OnGhostEnter(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (other.CompareTag(PlayerTag) && gameObject.activeInHierarchy)
        {
            exitGraceCoroutine = StartCoroutine(ExitGraceRoutine(other));
        }
    }

    // ---------------------------------------------------------------- 익사 (기존 WaterEnter 동작)

    private void OnPlayerEnter(Collider2D other)
    {
        if (isPlayerInWater)
        {
            if (exitGraceCoroutine != null)
            {
                StopCoroutine(exitGraceCoroutine);
                exitGraceCoroutine = null;
            }
            return;
        }

        isPlayerInWater = true;
        playerEnterCount++;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        SetPlayerControl(other, false);
        PlaySplashOnce(other);

        if (drownTime > 0f)
        {
            StartCoroutine(DrownRoutine(playerEnterCount));
        }
    }

    private IEnumerator DrownRoutine(int enterId)
    {
        yield return new WaitForSeconds(drownTime);
        if (isPlayerInWater && enterId == playerEnterCount && GameManager.instance != null)
        {
            GameManager.instance.PlayerDie();
        }
    }

    private IEnumerator ExitGraceRoutine(Collider2D other)
    {
        yield return new WaitForSeconds(ExitGraceTime);
        isPlayerInWater = false;
        if (other != null)
        {
            SetPlayerControl(other, true);
        }
        exitGraceCoroutine = null;
    }

    private void OnGhostEnter(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        SetComponentEnabled<PlayerMovement>(other, false);
        SetComponentEnabled<GhostController>(other, false);
        Animator animator = other.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        PlaySplashOnce(other);
    }

    private static void SetPlayerControl(Collider2D other, bool enabled)
    {
        SetComponentEnabled<PlayerMovement>(other, enabled);
        SetComponentEnabled<PlayerController>(other, enabled);
    }

    private static void SetComponentEnabled<T>(Collider2D other, bool enabled) where T : Behaviour
    {
        T component = other.GetComponent<T>();
        if (component != null)
        {
            component.enabled = enabled;
        }
    }

    private void PlaySplashOnce(Collider2D other)
    {
        GameObject owner = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        if (splashedObjects.Add(owner.GetInstanceID()) && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySFX(SfxType.WaterEnter);
        }
    }

    // ---------------------------------------------------------------- 에디터 표시

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
        Vector3 origin = transform.position;
        Gizmos.DrawWireCube(origin + new Vector3(width * 0.5f, height * 0.5f), new Vector3(width, height));
        Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
        Gizmos.DrawLine(origin + new Vector3(0f, height), origin + new Vector3(width, height));
    }

    private void Reset()
    {
        EnsureVisualChild();
        ApplySettings();
    }

    // 그림용 Tilemap 자식이 없으면 만든다. (프리팹을 처음 만들 때 사용)
    public void EnsureVisualChild()
    {
        if (GetComponent<Grid>() == null)
        {
            gameObject.AddComponent<Grid>();
        }
        if (GetComponentInChildren<Tilemap>(true) != null)
        {
            return;
        }

        var visual = new GameObject(VisualName);
        visual.transform.SetParent(transform, false);
        visual.AddComponent<Tilemap>();
        visual.AddComponent<TilemapRenderer>();
    }
}
