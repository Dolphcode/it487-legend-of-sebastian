using System;
using UnityEngine;

namespace SQZL.World
{
    public class RoomSpawnManager : MonoBehaviour
    {
        [Header("Initial Config")] [SerializeField]
        private bool startingRoom;

        [Header("Positional Config")] [SerializeField]
        private Transform cameraPosition;

        [SerializeField] private Transform warpPosition;
       
        public event Action<RoomSpawnManager> OnLoaded;
        public event Action<RoomSpawnManager> OnUnloaded;

        private void Start()
        {
            if (RoomManager._Instance?.CurrentRoomReference is null && startingRoom)
                RoomManager._Instance.SetInitialRoom(this);
        }
        
        internal void TriggerLoad()
        {
            Debug.Log($"How many things connected to load?");
            OnLoaded?.Invoke(this);
        }

        internal void TriggerUnload()
        {
            OnUnloaded?.Invoke(this);
        }
    }
}
