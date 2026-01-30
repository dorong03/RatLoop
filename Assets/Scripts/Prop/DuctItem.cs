using UnityEngine;

public class DuctItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private bool isDropped = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.isTrigger = true;
    }

    public void Drop()
    {
        if (isDropped)
        {
            return;
        }

        isDropped = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false; 
        rb.gravityScale = 1.5f;
    }
}