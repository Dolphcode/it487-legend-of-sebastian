using Player;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LockedGate : MonoBehaviour
{
    [SerializeField] private Tilemap wallLayer;
        
        // Like there is 100% a better way to handle this
    [SerializeField] private List<TileSwapEntry> swapTiles;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other);
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerInventory playerInventory = other.gameObject.GetComponent<PlayerInventory>();
            
            // TODO: Maybe not hardcode this to make this code reusable? idk
            if (playerInventory.GetConsumableAmount("key") > 0)
            {
                playerInventory.AccumulateConsumable("key", -1);
                foreach (TileSwapEntry swapEntry in swapTiles)
                {
                    wallLayer.SetTile(swapEntry.tileToSwap, swapEntry.swapTo);
                }

                Destroy(gameObject);
            }
        }
    }

    [System.Serializable]
    internal struct TileSwapEntry
    {
        [SerializeField] internal Vector3Int tileToSwap;
        [SerializeField] internal Tile swapTo;
    }
}
