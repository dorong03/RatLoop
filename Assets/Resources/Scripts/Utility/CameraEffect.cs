using System;
using System.Collections;
using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    private Transform target;
    
    // 카메라 따라가는 속도 조절
    [SerializeField] float smoothTime = 0.3f;
    // 아래키로 화면 내릴 수 있는 수치
    [SerializeField] private float lookDownAmount = 4f;
    // 오프셋
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    
    private float defaultYOffset;
    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        defaultYOffset = offset.y;
        target = GameObject.FindGameObjectWithTag("Player").transform;
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
    
    
    // 아래보는 키 눌렀을때 왔다 갔다
    public void SetLookDown(bool isLookingDown)
    {
        if (isLookingDown)
        {
            offset.y = defaultYOffset - lookDownAmount;
        }
        else
        {
            offset.y = defaultYOffset;
        }
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