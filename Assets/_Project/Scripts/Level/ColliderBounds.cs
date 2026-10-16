using UnityEngine;

// 콜라이더의 월드 영역을 콜라이더 모양과 Transform 으로 직접 계산한다.
// Collider2D.bounds 는 물리 시뮬레이션이 돌지 않는 프리팹 편집 화면에서 갱신되지 않기 때문에
// 에디터 세이프티 체크에서는 이 값을 쓴다.
public static class ColliderBounds
{
    public static Bounds Get(Collider2D col)
    {
        Vector2 min, max;
        switch (col)
        {
            case BoxCollider2D box:
                min = box.offset - box.size * 0.5f;
                max = box.offset + box.size * 0.5f;
                break;
            case CircleCollider2D circle:
                min = circle.offset - Vector2.one * circle.radius;
                max = circle.offset + Vector2.one * circle.radius;
                break;
            case CapsuleCollider2D capsule:
                min = capsule.offset - capsule.size * 0.5f;
                max = capsule.offset + capsule.size * 0.5f;
                break;
            case PolygonCollider2D polygon when polygon.points.Length > 0:
                min = max = polygon.points[0];
                foreach (Vector2 p in polygon.points)
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
                min += polygon.offset;
                max += polygon.offset;
                break;
            default:
                return col.bounds;
        }

        Transform t = col.transform;
        var bounds = new Bounds(t.TransformPoint(min), Vector3.zero);
        bounds.Encapsulate(t.TransformPoint(new Vector2(min.x, max.y)));
        bounds.Encapsulate(t.TransformPoint(new Vector2(max.x, min.y)));
        bounds.Encapsulate(t.TransformPoint(max));
        bounds.extents = new Vector3(bounds.extents.x, bounds.extents.y, 0f);
        return bounds;
    }
}
