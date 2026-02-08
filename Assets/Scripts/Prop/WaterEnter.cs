using UnityEngine;

public class WaterEnter : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);
        if (other.tag == "Player")
        {
            GameManager.instance.EndGame();
        } else if (other.tag == "Ghost")
        {
            other.GetComponent<PlayerMovement>().enabled = false;
            other.GetComponent<GhostController>().enabled = false;
            other.GetComponent<Animator>().SetTrigger("Die");
        }
    }
}
