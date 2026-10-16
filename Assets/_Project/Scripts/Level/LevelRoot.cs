using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 레벨 프리팹의 루트. 레벨 설정값과 레벨 전체에 대한 세이프티 체크를 담당한다.
//
// 구조:
//   LevelRoot
//     Background            배경 (패럴랙스)
//     Grid
//       Decoration          뒤쪽 장식 타일 (충돌 없음)
//       Ground              걷는 땅/벽 타일
//       DecorationFront     앞쪽 장식 타일 (충돌 없음)
//     Water                 WaterVolume 들
//     Props                 프롭들 (치즈 포함)
//     Bounds                레벨 경계 벽
//     SpawnPoint
public class LevelRoot : MonoBehaviour
{
    public enum CameraZoom
    {
        Normal, // 기준 해상도 228x128
        Wide    // 기준 해상도 320x180 (더 넓게 보임)
    }

    [PlannerField("레벨 이름", 0)]
    [SerializeField] private string displayName;

    [PlannerField("제한 시간(초)", 1)]
    [SerializeField, Range(5, 600)] private int timeLimit = 60;

    [PlannerField("리플레이 횟수", 2)]
    [SerializeField, Range(0, 10)] private int maxReplayCount = 1;

    [PlannerField("카메라 줌", 3)]
    [SerializeField] private CameraZoom cameraZoom = CameraZoom.Normal;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public int TimeLimit => timeLimit;
    public int MaxReplayCount => maxReplayCount;
    public CameraZoom Zoom => cameraZoom;

    public Vector2Int PixelPerfectReference =>
        cameraZoom == CameraZoom.Wide ? new Vector2Int(320, 180) : new Vector2Int(228, 128);

    public SpawnPoint SpawnPoint => GetComponentInChildren<SpawnPoint>(true);

    public Tilemap GroundTilemap
    {
        get
        {
            foreach (Tilemap tm in GetComponentsInChildren<Tilemap>(true))
            {
                if (tm.gameObject.layer == PhysicsLayers.Index(PhysicsLayers.Ground))
                {
                    return tm;
                }
            }
            return null;
        }
    }

    // 레벨 전체 세이프티 체크. 저장, 목록 등록, 테스트 플레이 전에 실행한다.
    public void Validate(PropValidationReport report, PropCatalog catalog = null)
    {
        if (timeLimit <= 0)
        {
            report.Error("제한 시간은 0초보다 커야 합니다.", this);
        }
        if (maxReplayCount < 0)
        {
            report.Error("리플레이 횟수는 0 이상이어야 합니다.", this);
        }

        SpawnPoint[] spawns = GetComponentsInChildren<SpawnPoint>(true);
        if (spawns.Length != 1)
        {
            report.Error($"시작 위치는 정확히 1개여야 합니다. (현재 {spawns.Length}개)", this);
        }

        GoalProp[] goals = GetComponentsInChildren<GoalProp>(true);
        if (goals.Length != 1)
        {
            report.Error($"치즈는 정확히 1개여야 합니다. (현재 {goals.Length}개)", this);
        }

        Tilemap ground = GroundTilemap;
        if (ground == null)
        {
            report.Error("Ground 타일맵이 없습니다.", this);
        }
        else if (spawns.Length == 1 && !HasGroundBelow(ground, spawns[0].transform.position, SpawnPoint.GroundSearchDistance))
        {
            report.Warning($"시작 위치 아래 {SpawnPoint.GroundSearchDistance}칸 안에 땅이 없습니다.", spawns[0]);
        }

        foreach (LevelProp prop in GetComponentsInChildren<LevelProp>(true))
        {
            prop.Validate(report);

            // 고정 프롭은 지형에 걸쳐 있어도 밀려나지 않으므로 물리 프롭만 검사한다.
            if (ground != null && prop is PhysicsProp && IsInsideGround(ground, prop))
            {
                report.Warning($"{prop.name}: 지형 타일 안에 박혀 있습니다. 시작하자마자 튕겨 나갈 수 있습니다.", prop);
            }

#if UNITY_EDITOR
            if (catalog != null && !(prop is SpawnPoint))
            {
                GameObject source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(prop.gameObject);
                if (source == null || catalog.FindByPrefab(source) == null)
                {
                    report.Warning($"{prop.name}: 카탈로그에 없는 프롭입니다.", prop);
                }
            }
#endif
        }

        CheckPhysicsOverlaps(report);
    }

    private static bool HasGroundBelow(Tilemap ground, Vector3 worldPosition, float distance)
    {
        Vector3Int cell = ground.WorldToCell(worldPosition);
        for (int i = 0; i <= Mathf.CeilToInt(distance); i++)
        {
            if (ground.HasTile(cell + Vector3Int.down * i))
            {
                return true;
            }
        }
        return false;
    }

    // 콜라이더 영역 안쪽(가장자리 제외)에 지형 타일이 있으면 박혀 있다고 본다.
    private static bool IsInsideGround(Tilemap ground, LevelProp prop)
    {
        foreach (Collider2D col in prop.GetComponentsInChildren<Collider2D>(true))
        {
            if (col.isTrigger)
            {
                continue;
            }

            Bounds b = col.bounds;
            b.Expand(-0.2f);
            if (b.size.x <= 0f || b.size.y <= 0f)
            {
                continue;
            }

            Vector3Int min = ground.WorldToCell(b.min);
            Vector3Int max = ground.WorldToCell(b.max);
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    if (ground.HasTile(new Vector3Int(x, y, 0)))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void CheckPhysicsOverlaps(PropValidationReport report)
    {
        var bodies = new List<PhysicsProp>(GetComponentsInChildren<PhysicsProp>(true));
        for (int i = 0; i < bodies.Count; i++)
        {
            for (int j = i + 1; j < bodies.Count; j++)
            {
                if (Overlaps(bodies[i], bodies[j]))
                {
                    report.Warning($"{bodies[i].name} 와 {bodies[j].name} 가 겹쳐 있습니다.", bodies[i]);
                }
            }
        }
    }

    private static bool Overlaps(LevelProp a, LevelProp b)
    {
        foreach (Collider2D ca in a.GetComponentsInChildren<Collider2D>(true))
        {
            Bounds ba = ca.bounds;
            ba.Expand(-0.1f);
            foreach (Collider2D cb in b.GetComponentsInChildren<Collider2D>(true))
            {
                if (!ca.isTrigger && !cb.isTrigger && ba.Intersects(cb.bounds))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
