using UnityEngine;

namespace SQZL.World.RoomClears
{
    public class InstantiateRoomClearHandler : BaseRoomClearHandler
    {
        [SerializeField] private GameObject toInstantiate;
        
        public override void OnRoomClear()
        {
            GameObject inst =  Instantiate(toInstantiate, transform.parent, true);
            inst.transform.position = this.transform.position;
        } 
    }
}
