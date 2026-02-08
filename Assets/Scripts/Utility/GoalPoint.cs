using UnityEngine;
using UnityEngine.InputSystem;

public class GoalTrigger : MonoBehaviour
{
    private bool isTriggered = false;
    private Vector3 _startPos;

    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float bobSpeed = 2.5f;

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
                SoundManager.instance.PlaySFX(SfxType.Cheese);
                GameManager.instance.LevelClear();
                isTriggered = true;
            }
        }
    }
}