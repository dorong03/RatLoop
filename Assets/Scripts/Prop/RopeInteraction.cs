using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RopeInteraction : MonoBehaviour
{
    [SerializeField] private List<DuctItem> itemsToDrop = new List<DuctItem>();
    [SerializeField] private float range = 2.0f;
    [SerializeField] private Transform player;

    private void Update()
    {

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= range && Keyboard.current.qKey.wasPressedThisFrame)
        {
            TriggerAllDrops();
        }
    }

    private void TriggerAllDrops()
    {
        foreach (DuctItem item in itemsToDrop)
        {
            item.Drop();
        }

        itemsToDrop.Clear();
    }
}