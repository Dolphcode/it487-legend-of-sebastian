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
        [SerializeField] private PlayerEntityController pecScript;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // Get the player's inventory
                PlayerInventory inv = other.gameObject.GetComponent<PlayerInventory>();
                PlayerHealthManager health = other.gameObject.GetComponent<PlayerHealthManager>();

                if (isCollectible)
                {
                    inv.AccumulateConsumable(identifier, amount);
                    if (identifier == "health")
                    {
                        // TODO: THIS HARDCODING IS TERRIBLE PLEASE FIX THIS LATER
                        health.Heal(amount); 
                    }
                    else if (identifier == "maxhealth")
                    {
                        health.AddMaxHealth(amount);
                    }
                }
                else
                {
                    if (identifier == "Bow")
                    {
                        if (inv.HasUnlockable("Bow"))
                        {
                            inv.AccumulateConsumable("rupee", 5);
                        } else {
                            inv.ToggleUnlockable("Bow", true);
                        }
                    }
                    if (identifier == "Boomerang")
                    {
                        inv.ToggleUnlockable("Boomerang", true);
                    }
                    if (identifier == "TorchFace") inv.ToggleUnlockable("TorchFace", true);
                    //inv.ToggleUnlockable(identifier, true);
                }

                // Make the appropriate modification
                // TODO Should probably have a coroutine to call in the player if this is an unlockable rather than a collectible

                // Destroy the pickup
                Destroy(gameObject);
            }
        }
    }
}
