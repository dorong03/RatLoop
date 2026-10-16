using UnityEngine;

public enum PropCategory
{
    Physics,     // 밀거나 쌓을 수 있는 물리 프롭
    Static,      // 고정된 발판 (버스, 박스 더미)
    Decoration,  // 부딪히지 않는 장식, 공중에 떠 있는 오브젝트
    Rope,        // 매달릴 수 있는 로프
    Water,       // 물 영역
    Gimmick,     // 특수 동작이 있는 오브젝트
    Goal,        // 치즈 (레벨에 하나)
    Spawn,       // 시작 위치 (레벨에 하나)
    Barrier      // 눈에 보이지 않는 벽 (투명벽)
}

// 칸 단위 크기를 가진 영역 프롭 (물, 투명벽). 오브젝트 위치는 영역의 왼쪽 아래 모서리다.
public interface IAreaProp
{
    Vector2Int AreaSize { get; }
}

// 레벨에 배치하는 모든 오브젝트의 베이스.
// 레이어와 소팅 레이어는 기획자가 고르지 않고 여기서 자동으로 지정한다.
// 프롭만의 기능은 상속해서 추가하고, 기획자가 조절할 값에는 [PlannerField] 를 붙인다.
[DisallowMultipleComponent]
public abstract class LevelProp : MonoBehaviour
{
    [SerializeField, HideInInspector] private string catalogId;

    public string CatalogId
    {
        get => catalogId;
        set => catalogId = value;
    }

    public abstract PropCategory Category { get; }

    // 렌더러에 지정할 소팅 레이어
    protected abstract string SortingLayerName { get; }

    // 오브젝트에 지정할 물리 레이어. null 이면 건드리지 않는다.
    protected virtual string PhysicsLayerName => null;

    // 직렬화된 값을 실제 컴포넌트(Rigidbody2D, Collider 등)에 반영한다.
    // 에디터에서 값을 바꿀 때와 게임 시작 시 모두 호출된다.
    public virtual void ApplySettings()
    {
        ApplySortingLayer();
        ApplyPhysicsLayer();
    }

    // 세이프티 체크. 프롭 하나만 보고 판단할 수 있는 것만 검사한다.
    public virtual void Validate(PropValidationReport report)
    {
    }

    protected virtual void Awake()
    {
        if (Application.isPlaying)
        {
            ApplySettings();
        }
    }

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        // OnValidate 안에서는 레이어 변경 같은 작업이 경고를 내므로 다음 에디터 틱에 반영한다.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying)
            {
                OnEditorValueChanged();
            }
        };
#endif
    }

    // 에디터에서 값이 바뀌거나 실행 취소/다시 실행이 된 뒤 불린다.
    // 위치를 옮기는 것처럼 실행 취소와 충돌할 수 있는 작업은 여기서 하지 않는다.
    protected virtual void OnEditorValueChanged()
    {
        ApplySettings();
    }

    protected void ApplySortingLayer()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.sortingLayerName != SortingLayerName)
            {
                r.sortingLayerName = SortingLayerName;
            }
        }
    }

    protected void ApplyPhysicsLayer()
    {
        string layerName = PhysicsLayerName;
        if (string.IsNullOrEmpty(layerName))
        {
            return;
        }

        SetLayerRecursively(transform, PhysicsLayers.Index(layerName));
    }

    protected static void SetLayerRecursively(Transform root, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer != layer)
            {
                t.gameObject.layer = layer;
            }
        }
    }
}
