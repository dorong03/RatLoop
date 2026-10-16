using UnityEngine;

public class PropContainer : MonoBehaviour
{
    private void Start()
    {
        RegisterProps();
    }
    
    private void RegisterProps()
    {
        var prop = GetComponentsInChildren<TitleProp>();
        if (prop != null)
        {
            foreach (var p in prop)
            {
                p.OnPropFall += OnPropFall;    
            }
        }
    }

    private void OnPropFall(GameObject prop)
    {
        prop.transform.position = transform.position;
        Rigidbody2D rb = prop.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
