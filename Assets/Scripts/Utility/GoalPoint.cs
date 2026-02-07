using UnityEngine;
using UnityEngine.InputSystem;

public class GoalTrigger : MonoBehaviour
{
    private bool isTriggered = false;
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