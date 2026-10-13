using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.World.Interactable;

namespace SQZL.World.RoomClears
{
    public class TrapResetter : BaseRoomClearHandler
    {
        // TODO : May need to make a list for teleporters too? idek
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
        }

        private void ResetTrap()
        {
            if (trapBroken) return;
            if (trapToReset.trapTriggers <= 0) trapToReset.trapTriggers++;
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
