using UnityEngine;
using UnityEngine.InputSystem;

public class GoalTrigger : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject clearMessageUI; 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ShowClearMessage();
        }
    }

    private void ShowClearMessage()
    {
        if (clearMessageUI != null)
        {
            clearMessageUI.SetActive(true); 
        }
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().enabled = false;
    }
}