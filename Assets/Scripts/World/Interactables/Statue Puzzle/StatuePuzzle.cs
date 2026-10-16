using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SQZL.World.Interactable
{
    public class StatuePuzzle : MonoBehaviour
    {
        [SerializeField] private List<LockedGate.TileSwapEntry> _tileSwapeEntries; // THIS IS SO SPAGHETTI : (
        [SerializeField] private List<StatueEntry> _solutionTable;
        [SerializeField] private Tilemap _targetTilemap;

        private bool solved = false;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Setup solution checking
            foreach (var entry in _solutionTable)
            {
                entry.statueReference.OnStatueFaceUpdate += CheckSolution;
            }
        }

        void CheckSolution()
        {
            if (solved) return;
            
            // Check solution
            int correctCount = 0;
            foreach (var entry in _solutionTable)
            {
                if (entry.faceRequired == entry.statueReference.CurrentFace)
                {
                    if ((entry.faceRequired == StatueObjectFace.TORCH && entry.statueReference.hasTorchFace) || entry.faceRequired != StatueObjectFace.TORCH)
                        correctCount++;
                }
            } 
            
            // Perform swap if we got them all
            if (correctCount == _solutionTable.Count)
            {
                solved = true;
                foreach (var entry in _tileSwapeEntries)
                    if (entry.clearTile) _targetTilemap.SetTile(entry.tileToSwap, null);
                    else _targetTilemap.SetTile(entry.tileToSwap, entry.swapTo);
            }
        }

        [System.Serializable]
        struct StatueEntry
        {
            [SerializeField] internal StatueObjectFace faceRequired;
            [SerializeField] internal StatueObject statueReference;
        }
    }
}
