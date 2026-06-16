using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterEnter : MonoBehaviour
{
    private static readonly HashSet<int> playedWaterEnterSfxIds = new HashSet<int>();
    private bool isPlayerEnterWater = false;
    private int waterEnterTime = 0;
    private Coroutine exitGraceCoroutine;
    private const float ExitGraceTime = 0.15f;

    private const string PlayerTag = "Player";

    private async void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == PlayerTag)
        {
            if (isPlayerEnterWater)
            {
                if (exitGraceCoroutine != null)
                {
                    StopCoroutine(exitGraceCoroutine);
                    exitGraceCoroutine = null;
                }
                return;
            }

            isPlayerEnterWater = true;
            waterEnterTime++;
            int currentTime = waterEnterTime;

            var rb = other.GetComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            other.GetComponent<PlayerMovement>().enabled = false;
            other.GetComponent<PlayerController>().enabled = false;
            PlayWaterEnterSfxOnce(other);

            await Awaitable.WaitForSecondsAsync(3f);
            if (isPlayerEnterWater && currentTime.Equals(waterEnterTime))
            {
                GameManager.instance.PlayerDie();
            }
        }
        else if (other.tag == "Ghost")
        {
            var rb = other.GetComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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
            exitGraceCoroutine = StartCoroutine(ExitGraceRoutine(other));
        }
    }

    private IEnumerator ExitGraceRoutine(Collider2D other)
    {
        yield return new WaitForSeconds(ExitGraceTime);
        isPlayerEnterWater = false;
        if (other != null)
        {
            other.GetComponent<PlayerMovement>().enabled = true;
            other.GetComponent<PlayerController>().enabled = true;
        }
        exitGraceCoroutine = null;
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