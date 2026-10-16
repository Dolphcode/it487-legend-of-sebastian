using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.World.Interactable;

namespace SQZL.World.RoomClears
{
    public class TrapResetter : BaseRoomClearHandler
    {

        [Header("Trap Set Config")] [SerializeField]
        private List<RoomTransitionTrigger> trapSetters;
        [SerializeField] private List<TileSwapEntry> setSwapEntries;
        
        [Header("Trap Reset Config")]
        [SerializeField] private List<RoomTransitionTrigger> trapResetters;
        [SerializeField] private RoomTransitionTrigger trapToReset;
        [SerializeField] private List<TileSwapEntry> swapEntries;
        [SerializeField] private Tilemap wallTiles;

        private bool trapBroken = false;

        private void Start()
        {
            base.Start();
            foreach (RoomTransitionTrigger resetter in trapResetters)
                resetter.PreTransition += ResetTrap;
            foreach (RoomTransitionTrigger resetter in trapSetters)
                resetter.PostTransition += TrySetTrap;
        }

        public void TrySetTrap()
        {
            if (trapBroken) return; 
            // Swap tiles
            foreach (TileSwapEntry swapEntry in setSwapEntries)
            {
                wallTiles.SetTile(swapEntry.tileToSwap, swapEntry.swapTo);
            }
        }

        private void ResetTrap()
        {
            if (trapBroken) return;
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

        public override void OnRoomClear()
        {
            trapBroken = true;
            foreach (RoomTransitionTrigger trigger in trapSetters)
                trigger.trapSettable = false;
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
