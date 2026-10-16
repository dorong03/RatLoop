using System.Collections;
using UnityEngine;

// 치즈. 플레이어가 닿으면 레벨 클리어. 레벨에 정확히 하나 있어야 한다.
public class GoalProp : LevelProp
{
    private bool isTriggered = false;
    private Vector3 _startPos;

    [Header("둥둥 떠다니기 설정")]
    [PlannerField("떠다니는 높이", 0)]
    [SerializeField, Range(0f, 2f)] private float bobHeight = 0.3f;
    [PlannerField("떠다니는 속도", 1)]
    [SerializeField, Range(0f, 10f)] private float bobSpeed = 2.5f;

    [Header("클리어 애니메이션 이펙트")]
    [SerializeField] private GameObject effectAnimPrefab;
    [SerializeField] private float effectInterval = 0.3f; 

    [Header("입 위치 설정")]
    [SerializeField] private string mouthObjectName = "MouthPoint";

    public override PropCategory Category => PropCategory.Goal;
    protected override string SortingLayerName => SortingLayers.Props;
    protected override string PhysicsLayerName => PhysicsLayers.Cheese;

    private void Start()
    {
        _startPos = transform.position;
    }

    public override void Validate(PropValidationReport report)
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.isTrigger)
        {
            report.Error("치즈에 트리거 콜라이더가 없어 먹을 수 없습니다.", this);
        }
    }

    private void Update()
    {
        if (!isTriggered)
        {
            float newY = _startPos.y + ((Mathf.Sin(Time.time * bobSpeed) + 1f) * 0.5f) * bobHeight;
            transform.position = new Vector3(_startPos.x, newY, _startPos.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!isTriggered)
            {
                isTriggered = true;
                Transform mouthTransform = collision.transform.Find(mouthObjectName);
                if (mouthTransform == null)
                {
                    mouthTransform = collision.transform;
                }
                if (effectAnimPrefab != null)
                {
                    StartCoroutine(SpawnEffectRoutine(mouthTransform));
                }
                CameraEffect cam = Camera.main.GetComponent<CameraEffect>();
                cam.ZoomUpPlayer(1.5f, 0.5f);
                GameManager.instance.ClearLevel();
            }
        }
    }

    private IEnumerator SpawnEffectRoutine(Transform mouthTransform)
    {
        while (true)
        {
            GameObject effect = Instantiate(effectAnimPrefab, mouthTransform.position, Quaternion.identity, mouthTransform);
            Destroy(effect, effectInterval);
            yield return new WaitForSeconds(effectInterval);
        }
    }
}