using System;
using UnityEngine;
using SQZL.Entity.Player;
using SQZL.Core;

namespace SQZL.World.Interactable
{
    public class RoomTeleportTrigger : MonoBehaviour
    {
        [SerializeField] private RoomSpawnManager teleportTo;

        [Header("Music Config")] [SerializeField]
        private bool setsMusic = false;
        [SerializeField] private MusicManager music;

        [SerializeField] private AudioClip newClip;
        
        public event Action OnTransition;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerEntityController player = other.gameObject.GetComponent<PlayerEntityController>();
                if (player.playerInputFrozen) return; // If player is being moved right now don't do anything
                //if (RoomManager._Instance.CurrentRoomReference != associatedRoom) return;
                if (setsMusic && !(music is null)) music.StopMusic();
                player.playerInputFrozen = true;
                OnTransition?.Invoke();
                RoomManager._Instance.UnloadRoom();
                player.WarpWithSlideInOut(teleportTo);
                if (setsMusic && !(music is null)) music.SetMusic(newClip);
                //StartCoroutine(CameraTransitionCoroutine(player));
            }
        }
    }
}