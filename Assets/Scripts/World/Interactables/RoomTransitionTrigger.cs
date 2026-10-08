using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SQZL.Entity.Player;
using SQZL.Entity;
using UnityEngine.Tilemaps;

namespace SQZL.World.Interactable
{
    public class RoomTransitionTrigger : MonoBehaviour
    {
        [Header("Layout Config")]
        [Tooltip("Ensure that your tile size matches the general project tile size/scaling and tilemap tile size")]
        [SerializeField]
        private float tileSize = 2f;

        [SerializeField] private Vector2Int roomSize = new Vector2Int(16, 11);
        [SerializeField] private RoomSpawnManager associatedRoom;
        [SerializeField] private bool vertical = false;

        [Tooltip(
            "If Initial Position Flag is set to false, it will be left in horizontal mode, and down in vertical mode")]
        [SerializeField]
        private bool initialPositionFlag = false;

         // Ensure that the object is placed in the center (vertically or horizontally) of the two tiles marking a gateway. The collider will be positioned automatically accordingly")]

         [Header("Trap Door Config")]
         [SerializeField] private int trapTriggers;

         [SerializeField] private int playerMoveTilesAdditional = 1;
         [SerializeField] private Tilemap wallTilemap;
         [SerializeField] private List<TileSwapEntry> tilesToSwap;
         
        [Header("Transition Config")]
        [SerializeField]
        private float playerMoveTime = 1f;

        [SerializeField] private int playerMoveTiles = 2;

        [SerializeField] private float cameraMoveTime = 2f;

        [Header("Connected Rooms")] [SerializeField] [Tooltip("The room to the left or below")]
        private RoomSpawnManager aRoom;

        [SerializeField] [Tooltip("The room to the right or above")]
        private RoomSpawnManager bRoom;
        
        // Components
        private BoxCollider2D boxCollider2D;

        private Camera mainCamera2D;
        private bool positionFlag;


        private void Start()
        {
            // Get important components
            boxCollider2D = GetComponent<BoxCollider2D>();
            mainCamera2D = Camera.main;

            // Setup
            positionFlag = initialPositionFlag;

            // Configure size of collider
            boxCollider2D.size = new Vector2(tileSize, tileSize);

            // Set the initial position of the collider
            if (vertical)
            {
                if (initialPositionFlag) boxCollider2D.offset = new Vector2(0f, tileSize * 0.5f);
                else boxCollider2D.offset = new Vector2(0f, -tileSize * 0.5f);
            }
            else
            {
                if (initialPositionFlag) boxCollider2D.offset = new Vector2(tileSize * 0.5f, 0f);
                else boxCollider2D.offset = new Vector2(-tileSize * 0.5f, 0f);
            }

        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerEntityController player = other.gameObject.GetComponent<PlayerEntityController>();
                if (player.playerInputFrozen) return; // If player is being moved right now don't do anything
                if (RoomManager._Instance.CurrentRoomReference != associatedRoom) return;
                player.playerInputFrozen = true;
                RoomManager._Instance.UnloadRoom();
                StartCoroutine(CameraTransitionCoroutine(player));
            }
        }

        private void ToggleTriggerPosition()
        {
            // TODO MAKE THIS SHIT BETTER PLEASE
            //positionFlag = !positionFlag; (Basically don't toggle the position flag)
            if (vertical)
            {
                if (positionFlag) boxCollider2D.offset = new Vector2(0f, tileSize * 0.5f);
                else boxCollider2D.offset = new Vector2(0f, -tileSize * 0.5f);
            }
            else
            {
                if (positionFlag) boxCollider2D.offset = new Vector2(tileSize * 0.5f, 0f);
                else boxCollider2D.offset = new Vector2(-tileSize * 0.5f, 0f);
            }
        }

        private IEnumerator CameraTransitionCoroutine(PlayerEntityController player)
        {
            // Set up starting and end position of camera
            Vector3 currPos = mainCamera2D.transform.position;
            Vector3 nextPos = currPos;
            float
                direction = ((positionFlag)
                    ? 1f
                    : -1f); // TRUE = collider is on right so player is moving right OR collider is up so player is moving up, either way TRUE = POSITIVE delta
            if (vertical) nextPos.y += tileSize * roomSize.y * direction;
            else nextPos.x += tileSize * roomSize.x * direction;

            Vector3 delta = nextPos - currPos;
            delta /= cameraMoveTime;
            yield return null; // wait for next update frame
            for (float currTime = cameraMoveTime; currTime >= 0f; currTime -= Time.deltaTime)
            {
                mainCamera2D.transform.position += new Vector3(delta.x, delta.y) * Time.deltaTime;
                yield return null;
            }

            // Snap to next pos if we overshot, we should be pretty close I think?
            mainCamera2D.transform.position = nextPos;

            // Now move the player
            TilebodyDirection dir = TilebodyDirection.Down;
            if (vertical && positionFlag) dir = TilebodyDirection.Up;
            else if (!vertical && positionFlag) dir = TilebodyDirection.Right;
            else if (!vertical && !positionFlag) dir = TilebodyDirection.Left;
            if (trapTriggers <= 0) yield return player.ForcePlayerCoroutine(playerMoveTiles, playerMoveTime, dir, true, false, false);
            else
            {
                yield return player.ForcePlayerCoroutine(playerMoveTiles + playerMoveTilesAdditional, playerMoveTime, dir, true, false, false);
                trapTriggers--;
                
                // Swap tiles
                foreach (TileSwapEntry swapEntry in tilesToSwap)
                {
                    wallTilemap.SetTile(swapEntry.tileToSwap, swapEntry.swapTo);
                }
            }
            // And toggle the trigger's position        
            ToggleTriggerPosition();

            // And unfreeze the player
            player.playerInputFrozen = false;

            // Animation has ended
            if (positionFlag) RoomManager._Instance.LoadRoom(bRoom);
            else RoomManager._Instance.LoadRoom(aRoom);
        }
        
        [System.Serializable]
        internal struct TileSwapEntry
        {
            [SerializeField] internal Vector3Int tileToSwap;
            [SerializeField] internal Tile swapTo;
        }
    }
}
