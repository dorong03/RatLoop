using System;
using UnityEngine;

public class WaterEnter : MonoBehaviour
{
    private bool isPlayerEnterWater = false;
    
    private async void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);
        if (other.tag == "Player")
        {
            isPlayerEnterWater = true;
            other.GetComponent<PlayerMovement>().enabled = false;

            await Awaitable.WaitForSecondsAsync(3f);
            if (isPlayerEnterWater)
            {
                GameManager.instance.PlayerDie();
            }
        } 
        else if (other.tag == "Ghost")
        {
            other.GetComponent<PlayerMovement>().enabled = false;
            other.GetComponent<GhostController>().enabled = false;
            other.GetComponent<Animator>().SetTrigger("Die");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.tag.Equals("Player") || other.tag.Equals("Ghost"))
        {
            other.GetComponent<PlayerMovement>().enabled = true;
            if (other.tag.Equals("Player"))
            {
                isPlayerEnterWater = false;
            }
        }
    }
}
