using System;
using UnityEngine;

public class TitleProp : MonoBehaviour
{
    public Action<GameObject> OnPropFall;

    void OnTriggerEnter2D(Collider2D collider)
    {
        Debug.Log(collider.gameObject.name);
        if (collider.CompareTag("Deadzone"))
        {
            OnPropFall?.Invoke(gameObject);
        }
    }
}
