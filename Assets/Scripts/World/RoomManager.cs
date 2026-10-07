using UnityEngine;

namespace SQZL.World
{
    public class RoomManager : MonoBehaviour
    {

        public static RoomManager _Instance { private set; get; } = null;

        private void Awake()
        {
            if (_Instance is null)
                _Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            Debug.Log($"We are starting with {_currentRoom}");
            if (!(_currentRoom is null)) _currentRoom.TriggerLoad();
        }

        private RoomSpawnManager _currentRoom = null;
        public RoomSpawnManager CurrentRoomReference
        {
            private set { _currentRoom = value;  }
            get { return _currentRoom; }
        }

        /// <summary>
        /// Loads a room by object. Unloads the previously loaded room if one still exists/is active
        /// </summary>
        /// <param name="room">The room to load</param>
        public void LoadRoom(RoomSpawnManager room)
        {
            // Unload the current room
            if (!(_currentRoom is null))
            {
                _currentRoom.TriggerUnload();
                _currentRoom = null;
            }
            
            // Load the next room to load
            room.TriggerLoad();
            _currentRoom = room;
        }
        
        /// <summary>
        /// Unloads the currently active room
        /// </summary>
        public void UnloadRoom()
        {
            if (_currentRoom is null)
            {
               Debug.LogError("No room to unload"); 
            }
            else
            {
                _currentRoom.TriggerUnload();
                _currentRoom = null;
            }
        }

        /// <summary>
        /// Sets the initial room such that its load function will be called on start. Does not load yet.
        /// </summary>
        /// <param name="room">The room to load on Start</param>
        internal void SetInitialRoom(RoomSpawnManager room)
        {
            _currentRoom = room;
        }
    }
}
