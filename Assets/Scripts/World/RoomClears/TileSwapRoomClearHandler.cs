using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SQZL.World.RoomClears
{
    public class TileSwapRoomClearHandler : BaseRoomClearHandler
    {

        [SerializeField] private List<TileSwapEntry> swapEntries;
        [SerializeField] private Tilemap wallTiles;
        
        public override void OnRoomClear()
        {
            foreach (TileSwapEntry swapEntry in swapEntries)
            {
                if (swapEntry.clearTile)
                {
                    wallTiles.SetTile(swapEntry.tileToSwap, null);
                }
                else
                {
                    wallTiles.SetTile(swapEntry.tileToSwap, swapEntry.swapTo);
                }
            }
        }
        
        [System.Serializable]
        internal struct TileSwapEntry
        {
            [SerializeField] internal bool clearTile;
            [SerializeField] internal Vector3Int tileToSwap;
            [SerializeField] internal Tile swapTo;
        } 
    }
}
