using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

public class CameraEffect : MonoBehaviour
{
    private Camera _camera;
    private PixelPerfectCamera _ppc;
    
    private Transform target;
    
    [SerializeField] float smoothTime = 0.3f;
    [SerializeField] private float lookDownAmount = 4f;
    [SerializeField] private float lookUpAmount = 4f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    
    private float defaultYOffset;
    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        defaultYOffset = offset.y;
        _camera = GetComponent<Camera>();
        _ppc = GetComponent<PixelPerfectCamera>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
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

    public async void ZoomUpPlayer(float zoomAmount, float time)
    {
        if (_ppc == null) return;
        _ppc.enabled = false;

        float startSize = _camera.orthographicSize;
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.deltaTime;
            float t = timer / time;
            _camera.orthographicSize = Mathf.Lerp(startSize, zoomAmount, t);
            await Awaitable.NextFrameAsync();
        }

        _camera.orthographicSize = zoomAmount;
        int targetResY = Mathf.RoundToInt(zoomAmount * 2 * _ppc.assetsPPU);
        int targetResX = Mathf.RoundToInt(targetResY * (16f / 9f));

        _ppc.refResolutionX = targetResX;
        _ppc.refResolutionY = targetResY;
        
        _ppc.enabled = true;
    }

    public void PlayPreViewSequence(Action onComplete)
    {
        StartCoroutine(PreviewSequence(onComplete));
    }
    
    public IEnumerator PreviewSequence(Action onComplete)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null) target = player;
        yield return new WaitForSeconds(2f);

        GameObject cheese = GameObject.FindGameObjectWithTag("Cheese");
        if (cheese != null)
        {
            target = cheese.transform;
            yield return new WaitForSeconds(2f);
        }

        if (player != null) target = player;
        yield return new WaitForSeconds(1f);
        onComplete?.Invoke();
    }
}