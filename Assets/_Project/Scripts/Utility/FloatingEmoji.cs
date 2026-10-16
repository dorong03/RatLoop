using UnityEngine;

public class FloatingEmoji : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float swayAmplitude = 0.5f;
    [SerializeField] private float swayFrequency = 2.0f;

    [Header("Life Cycle & Scale")]
    [SerializeField] private float lifetime = 3.0f;
    [SerializeField] private float shrinkDuration = 0.5f;

    private float timer = 0f;
    private Vector3 initialScale;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        initialScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        
        float newY = transform.position.y + (floatSpeed * Time.deltaTime);
        float newX = startPosition.x + Mathf.Sin(timer * swayFrequency) * swayAmplitude;
        transform.position = new Vector3(newX, newY, transform.position.z);

        HandleScaleAndDestruction();
    }

    private void HandleScaleAndDestruction()
    {
        float remainingTime = lifetime - timer;

        if (remainingTime <= shrinkDuration)
        {
            float t = remainingTime / shrinkDuration;
            transform.localScale = initialScale * t;
        }
        
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}