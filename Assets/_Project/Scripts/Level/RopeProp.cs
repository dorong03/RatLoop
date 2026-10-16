using UnityEngine;

// 매달릴 수 있는 로프(체인). 마디 수만 정하면 마디를 자동으로 만들고 HingeJoint2D 로 잇는다.
// 에디터(프리팹 편집 화면 포함)에서는 저장되지 않는 미리보기 마디를 보여주고, 게임 시작 시 실제 마디를 만든다.
[ExecuteAlways]
public class RopeProp : LevelProp
{
    private const string LinkPrefix = "Link_";

    [PlannerField("마디 수", 0)]
    [SerializeField, Range(1, 30)] private int segmentCount = 6;

    [PlannerField("마디 무게", 1)]
    [SerializeField, Range(0.1f, 10f)] private float segmentMass = 1.3f;

    [PlannerField("흔들림 저항", 2)]
    [SerializeField, Range(0f, 5f)] private float segmentDamping = 0.3f;

    [Header("구성")]
    [Tooltip("마디 하나. Rigidbody2D, HingeJoint2D, Collider2D 가 있어야 한다.")]
    [SerializeField] private GameObject linkPrefab;
    [Tooltip("마디 사이 간격 (로프 로컬 좌표)")]
    [SerializeField] private float segmentSpacing = 1.25f;
    [SerializeField] private Transform segmentRoot;

    public override PropCategory Category => PropCategory.Rope;
    protected override string SortingLayerName => SortingLayers.Props;

    public override void ApplySettings()
    {
        Rebuild();
        base.ApplySettings();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            if (CountLinks() == 0)
            {
                Rebuild();
                ApplySortingLayer();
            }
        }
        else
        {
            Rebuild();
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            ClearLinks();
        }
    }

    private void Rebuild()
    {
        ClearLinks();
        if (linkPrefab == null || segmentRoot == null)
        {
            return;
        }

        int poleLayer = PhysicsLayers.Index(PhysicsLayers.Pole);
        Rigidbody2D previous = null;
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject link = Instantiate(linkPrefab, segmentRoot);
            link.name = LinkPrefix + i;
            link.transform.localPosition = new Vector3(0f, -segmentSpacing * i, 0f);
            link.transform.localRotation = Quaternion.identity;
            if (!Application.isPlaying)
            {
                // 에디터 미리보기용 마디: 프리팹에 저장되지 않고 선택/수정할 수 없다.
                foreach (Transform t in link.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                }
            }
            SetLayerRecursively(link.transform, poleLayer);

            Rigidbody2D rb = link.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.mass = segmentMass;
                rb.linearDamping = segmentDamping;
            }

            HingeJoint2D hinge = link.GetComponent<HingeJoint2D>();
            if (hinge != null)
            {
                hinge.autoConfigureConnectedAnchor = true;
                hinge.connectedBody = previous;
            }
            previous = rb;
        }
    }

    private int CountLinks()
    {
        if (segmentRoot == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Transform child in segmentRoot)
        {
            if (child.name.StartsWith(LinkPrefix))
            {
                count++;
            }
        }
        return count;
    }

    private void ClearLinks()
    {
        if (segmentRoot == null)
        {
            return;
        }

        for (int i = segmentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = segmentRoot.GetChild(i);
            if (!child.name.StartsWith(LinkPrefix))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public override void Validate(PropValidationReport report)
    {
        if (linkPrefab == null)
        {
            report.Error($"{name}: 로프 마디 프리팹이 비어 있습니다.", this);
        }
        else if (linkPrefab.GetComponent<HingeJoint2D>() == null || linkPrefab.GetComponent<Rigidbody2D>() == null)
        {
            report.Error($"{name}: 로프 마디에 HingeJoint2D 와 Rigidbody2D 가 필요합니다.", this);
        }

        if (segmentRoot == null)
        {
            report.Error($"{name}: 마디를 넣을 Segments 오브젝트가 없습니다.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (segmentRoot == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Vector3 top = segmentRoot.position;
        Vector3 bottom = segmentRoot.TransformPoint(new Vector3(0f, -segmentSpacing * segmentCount, 0f));
        Gizmos.DrawLine(top, bottom);
    }
}
