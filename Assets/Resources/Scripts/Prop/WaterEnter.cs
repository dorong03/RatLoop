using System;
using System.Collections.Generic;
using UnityEngine;

public class WaterEnter : MonoBehaviour
{
    private static readonly HashSet<int> playedWaterEnterSfxIds = new HashSet<int>();
    private bool isPlayerEnterWater = false;

    private const string PlayerTag = "Player";
    
    private async void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == PlayerTag)
        {
            Debug.Log("Player Enter");
            isPlayerEnterWater = true;
            other.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(0, other.GetComponent<Rigidbody2D>().linearVelocity.y, 0);
            other.GetComponent<PlayerMovement>().enabled = false;
            other.GetComponent<PlayerController>().enabled = false;
            PlayWaterEnterSfxOnce(other);
            await Awaitable.WaitForSecondsAsync(3f);
            if (isPlayerEnterWater)
            {
                GameManager.instance.PlayerDie();
            }
        } 
        else if (other.tag == "Ghost")
        {
            other.GetComponent<Rigidbody2D>().linearVelocity = new Vector3(0, other.GetComponent<Rigidbody2D>().linearVelocity.y, 0);
            other.GetComponent<PlayerMovement>().enabled = false;
            other.GetComponent<GhostController>().enabled = false;
            other.GetComponent<Animator>().SetTrigger("Die");
            PlayWaterEnterSfxOnce(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag.Equals(PlayerTag))
        {
            Debug.Log("Player Exit");
            isPlayerEnterWater = false;
            other.GetComponent<PlayerMovement>().enabled = true;
            other.GetComponent<PlayerController>().enabled = true;
        }
    }

    private void PlayWaterEnterSfxOnce(Collider2D other)
    {
        int objectId = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject.GetInstanceID()
            : other.transform.root.gameObject.GetInstanceID();

        if (playedWaterEnterSfxIds.Add(objectId))
        {
            SoundManager.instance.PlaySFX(SfxType.WaterEnter);
        }
    }
}
