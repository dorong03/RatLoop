using UnityEngine;
using UnityEngine.InputSystem;

public class GoalTrigger : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.instance.LevelClear();
        }
    }
}