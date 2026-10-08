using System;
using SQZL.Entity.Player;
using UnityEngine;

namespace SQZL.World.Interactable
{
    public class ItemPickup : MonoBehaviour
    {
        [Header("Pickup Config")] [SerializeField]
        private bool isCollectible;

        [SerializeField] private string identifier;
        [SerializeField] private int amount;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // Get the player's inventory
                PlayerInventory inv = other.gameObject.GetComponent<PlayerInventory>();

                if (isCollectible)
                {
                    inv.AccumulateConsumable(identifier, amount);
                }
                else
                {
                    inv.ToggleUnlockable(identifier, true);
                }

                // Make the appropriate modification
                // TODO Should probably have a coroutine to call in the player if this is an unlockable rather than a collectible

                // Destroy the pickup
                Destroy(gameObject);
            }
        }
    }
}
