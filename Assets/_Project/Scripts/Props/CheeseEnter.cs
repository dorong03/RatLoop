using System.Collections;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.InputSystem;

public class GoalTrigger : MonoBehaviour
{
    private bool isTriggered = false;
    private Vector3 _startPos;

    [Header("둥둥 떠다니기 설정")]
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float bobSpeed = 2.5f;

    [Header("클리어 애니메이션 이펙트")]
    [SerializeField] private GameObject effectAnimPrefab;
    [SerializeField] private float effectInterval = 0.3f; 

    [Header("입 위치 설정")]
    [SerializeField] private string mouthObjectName = "MouthPoint";

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        if (!isTriggered)
        {
            float newY = _startPos.y + ((Mathf.Sin(Time.time * bobSpeed) + 1f) * 0.5f) * bobHeight;
            transform.position = new Vector3(_startPos.x, newY, _startPos.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!isTriggered)
            {
                isTriggered = true;
                Transform mouthTransform = collision.transform.Find(mouthObjectName);
                if (mouthTransform == null)
                {
                    mouthTransform = collision.transform;
                }
                if (effectAnimPrefab != null)
                {
                    StartCoroutine(SpawnEffectRoutine(mouthTransform));
                }
                CameraEffect cam = Camera.main.GetComponent<CameraEffect>();
                cam.ZoomUpPlayer(1.5f, 0.5f);
                GameManager.instance.ClearLevel();
            }
        }
    }

    private IEnumerator SpawnEffectRoutine(Transform mouthTransform)
    {
        while (true)
        {
            GameObject effect = Instantiate(effectAnimPrefab, mouthTransform.position, Quaternion.identity, mouthTransform);
            Destroy(effect, effectInterval);
            yield return new WaitForSeconds(effectInterval);
        }
    }
}