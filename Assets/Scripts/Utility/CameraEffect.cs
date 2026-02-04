using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    public Transform target;

    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0, 0, -10f);
    private Vector3 _velocity = Vector3.zero;

    void Update()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
            return;
        }
        
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);
    }
    
}