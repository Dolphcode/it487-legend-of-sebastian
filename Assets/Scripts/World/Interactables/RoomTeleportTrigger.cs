using System;
using UnityEngine;
using SQZL.Entity.Player;

namespace SQZL.World.Interactable
{
    public class RoomTeleportTrigger : MonoBehaviour
    {
        [SerializeField] private RoomSpawnManager teleportTo;

        public event Action OnTransition;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerEntityController player = other.gameObject.GetComponent<PlayerEntityController>();
                if (player.playerInputFrozen) return; // If player is being moved right now don't do anything
                //if (RoomManager._Instance.CurrentRoomReference != associatedRoom) return;
                player.playerInputFrozen = true;
                OnTransition?.Invoke();
                RoomManager._Instance.UnloadRoom();
                player.WarpWithSlideInOut(teleportTo);
                //StartCoroutine(CameraTransitionCoroutine(player));
            }
        }
    }
}