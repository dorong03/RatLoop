using System;
using System.Collections;
using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    private Camera _camara;
    
    private Transform target;
    
    [SerializeField] float smoothTime = 0.3f;
    
    [SerializeField] private float lookDownAmount = 4f;
    [SerializeField] private float lookUpAmount = 4f;
    
    // 오프셋
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    
    private float defaultYOffset;
    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        defaultYOffset = offset.y;
        target = GameObject.FindGameObjectWithTag("Player").transform;
        _camara = gameObject.GetComponent<Camera>();
    }
    
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void SetCameraLook(bool isUp, bool isDown)
    {
        if (isUp)
        {
            offset.y = defaultYOffset + lookUpAmount;
        }
        else if (isDown)
        {
            offset.y = defaultYOffset - lookDownAmount;
        }
        else
        {
            offset.y = defaultYOffset;
        }
    }

    public void ZoomUpPlayer(float zoomAmout)
    {
        _camara.fieldOfView = zoomAmout;
    }

    public void PlayPreViewSequence(Action onComplete)
    {
        StartCoroutine(PreviewSequence(onComplete));
    }
    
    public IEnumerator PreviewSequence(Action onComplete)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        target = player;
        yield return new WaitForSeconds(2f);

        GameObject cheese = GameObject.FindGameObjectWithTag("Cheese");
        if (cheese != null)
        {
            target = cheese.transform;
            yield return new WaitForSeconds(2f);
        }

        target = player;
        yield return new WaitForSeconds(1f);
        onComplete?.Invoke();
    }
}