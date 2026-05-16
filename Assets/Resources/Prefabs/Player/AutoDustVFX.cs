using UnityEngine;

public class AutoDustVFX : MonoBehaviour
{
    [SerializeField] private GameObject dustPrefab; 
    [SerializeField] private Transform jumpDustPos;
    [SerializeField] private Transform landDustPos;
    [SerializeField] private string[] groundTags = { "Ground" };
    [SerializeField] private int dustCount = 4;
    [SerializeField] private float spreadForce = 2.5f;
    [SerializeField] private float destroyTime = 0.5f;

    private Rigidbody2D rb;
    private float lastYVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (lastYVelocity <= 0.1f && rb.linearVelocity.y > 1.5f)
        {
            CreateDust(ref jumpDustPos);
        }

        lastYVelocity = rb.linearVelocity.y;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        foreach (string tag in groundTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                CreateDust(ref landDustPos);
                break;
            }
        }
    }

    void CreateDust(ref Transform spawnPos)
    {
        for (int i = 0; i < dustCount; i++)
        {
            GameObject dust = Instantiate(dustPrefab, spawnPos.position, Quaternion.identity);
            Rigidbody2D dustRb = dust.GetComponent<Rigidbody2D>();

            if (dustRb != null)
            {
                float randomX = Random.Range(-1f, 1f);
                Vector2 spreadDir = new Vector2(randomX, 0.3f).normalized;
                dustRb.AddForce(spreadDir * spreadForce, ForceMode2D.Impulse);
            }

            Destroy(dust, destroyTime);
        }
    }
}