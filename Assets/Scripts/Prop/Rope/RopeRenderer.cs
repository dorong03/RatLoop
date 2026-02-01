using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeRenderer : MonoBehaviour
{
    [SerializeField] private Rope2D ropeCreator;
    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (ropeCreator == null || ropeCreator.ropeSegments.Count == 0)
        {
            return;
        }

        lineRenderer.positionCount = ropeCreator.ropeSegments.Count;

        for (int i = 0; i < ropeCreator.ropeSegments.Count; i++)
        {
            lineRenderer.SetPosition(i, ropeCreator.ropeSegments[i].transform.position);
        }
    }
}