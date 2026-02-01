using UnityEngine;
using System.Collections.Generic;

public class Rope2D : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private GameObject ropeSegmentPrefab; 
    [SerializeField] private int segmentCount = 10;

    [HideInInspector] public List<GameObject> ropeSegments = new List<GameObject>();

    [ContextMenu("Generate Rope")]
    private void GenerateRope()
    {
        RemoveRope();

        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Rigidbody2D previousRigidbody = null;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 spawnPos = Vector3.Lerp(startPos, endPos, t);
            GameObject segment = Instantiate(ropeSegmentPrefab, spawnPos, Quaternion.identity, transform);
            ropeSegments.Add(segment);
            HingeJoint2D joint = segment.GetComponent<HingeJoint2D>();

            if (i == 0)
            {
                segment.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            }
            else
            {
                joint.connectedBody = previousRigidbody;
            }

            previousRigidbody = segment.GetComponent<Rigidbody2D>();
        }
    }

    private void RemoveRope()
    {
        foreach (GameObject segment in ropeSegments)
        {
            DestroyImmediate(segment);
        }
        ropeSegments.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, t);
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}