using SQZL.World.RoomClears;
using UnityEngine;

namespace SQZL.World.RoomClears
{
    public abstract class BaseRoomClearHandler : MonoBehaviour
    {
        protected RoomEntityTracker tracker;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {
#if DEBUG
            if (!transform.parent.TryGetComponent<RoomEntityTracker>(out tracker))
            {
                Debug.LogError("Parent object does not have an entity tracker. This will not work");
                return;
            }
#else
            tracker = transform.parent.GetComponent<RoomEntityTracker>();
#endif   

            tracker.OnRoomClearedEvent += OnRoomClear;
        }

        public abstract void OnRoomClear();
    }
}
