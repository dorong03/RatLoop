using UnityEngine;
using System.Collections; 

public class WaterSplash : MonoBehaviour
{
    [SerializeField] private GameObject[] droplets;
    [SerializeField] private int dropCount = 5;
    [SerializeField] private float splashForce = 4f;
    [SerializeField] private float destroyTime = 1f;
    private bool isSplashing = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Water") && !isSplashing)
        {
            StartCoroutine(SplashRoutine());
        }
    }

    private IEnumerator SplashRoutine()
    {
        isSplashing = true; 
        CreateSplash();
        yield return new WaitForSeconds(4f);
        isSplashing = false; 
    }

    private void CreateSplash()
    {
        for (int i = 0; i < dropCount; i++)
        {
            int randomIndex = Random.Range(0, droplets.Length);
            GameObject drop = Instantiate(droplets[randomIndex], transform.position, Quaternion.identity);
            Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                float randomX = Random.Range(-1f, 1f);
                Vector2 splashDirection = new Vector2(randomX, 1f).normalized;
                rb.AddForce(splashDirection * splashForce, ForceMode2D.Impulse);
            }

            Destroy(drop, destroyTime);
        }
    }
}