using UnityEngine;

// 플레이어 시작 위치. 레벨에 정확히 하나 있어야 한다.
public class SpawnPoint : LevelProp
{
    // SpawnManager.AlignToGround 가 아래로 땅을 찾는 거리와 같다.
    public const float GroundSearchDistance = 10f;

    public override PropCategory Category => PropCategory.Spawn;
    protected override string SortingLayerName => SortingLayers.Effects;

    // 땅이 아래에 있는지는 LevelRoot.Validate 가 타일맵으로 검사한다.

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1f);
    }
}
