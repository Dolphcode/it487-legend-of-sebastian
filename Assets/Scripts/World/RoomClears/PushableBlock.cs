using System.Collections;
using System.Collections.Generic;
using SQZL.Entity;
using SQZL.Entity.Player;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.World.Interactable;

namespace SQZL.World.RoomClears
{
    public class PushableBlock : BaseRoomClearHandler, ITilebodyCollisionHandler
    {
        public const float STOP_MOVEMENT_THRESHOLD = 1e-2f;
        
        [SerializeField] private TilebodyDirection pushDirection;
        [SerializeField] private List<TilebodyDirection> pushableDirections;
        [SerializeField] private int unitsToMove;
        [SerializeField] private float timeToMove;

        [Header("Tile Swap Config")] [SerializeField]
        private bool swapsTiles;
        [SerializeField] private Tilemap wallLayer;
        [SerializeField] private TileSwapEntry[] tileSwapEntries;

        [Header("Reset Config")] [SerializeField]
        private bool resetWithTransition;

        [SerializeField] private RoomTransitionTrigger trigger;
        
        private bool triggered = false;
        private bool canBeTriggered = false;
        private Vector3 originalPosition;

        protected override void Start()
        {
            if (!transform.parent.TryGetComponent<RoomEntityTracker>(out tracker))
            {
                // Set can be triggered to true by default, no room clear req
                canBeTriggered = true;
            }
            else
            {
                tracker.OnRoomClearedEvent += OnRoomClear;
            }
            

            originalPosition = transform.position;
            if (resetWithTransition)
            {
                trigger.PreTransition += ResetTile;
            }
        }

        private void ResetTile()
        {
            transform.position = originalPosition;
        }
        
        public override void OnRoomClear()
        {
            Debug.Log("BLOCK: Slimes have been cleared!!!");
            canBeTriggered = true;
        }

        public void OnCollideWithTilebody(Tilebody2D body)
        {
            Debug.Log("BLOCK: I am colliding with something?");
            if (canBeTriggered && !triggered && 
                body.gameObject.CompareTag("Player"))
            {
                PlayerEntityController e = body.gameObject.GetComponent<PlayerEntityController>();
                foreach (TilebodyDirection dir in pushableDirections)
                {
                    bool test;
                    switch (dir)
                    {
                        case TilebodyDirection.Up:
                            test = e.transform.position.y < transform.position.y &&
                                   body.LastMovedDirection == TilebodyDirection.Up;
                            break;
                        case TilebodyDirection.Down:
                            test = e.transform.position.y > transform.position.y &&
                                    body.LastMovedDirection == TilebodyDirection.Down;
                            break;
                        case TilebodyDirection.Left:
                            test = e.transform.position.x > transform.position.x &&
                                  body.LastMovedDirection == TilebodyDirection.Left;
                            break;
                        case TilebodyDirection.Right:
                            test = e.transform.position.x < transform.position.x &&
                                   body.LastMovedDirection == TilebodyDirection.Right;
                            break;
                        default:
                            test = false;
                            break;
                    }

                    if (test)
                    {
                        triggered = true;
                        e.WaitForTime(timeToMove);
                        StartCoroutine(PushCoroutine(body.LastMovedDirection));
                        break;
                    }
                }
            }
        }

        private IEnumerator PushCoroutine(TilebodyDirection pushDirection)
        {
            float speed = (float)unitsToMove / timeToMove;
            Vector3 direction = Vector3.zero;
            switch (pushDirection)
            {
                case TilebodyDirection.Down:
                    direction = Vector3.down;
                    break;
                case TilebodyDirection.Left:
                    direction = Vector3.left;
                    break;
                case TilebodyDirection.Right:
                    direction = Vector3.right;
                    break;
                case TilebodyDirection.Up:
                default:
                    direction = Vector3.up;
                    break;
            }
            for (float timeLeft = timeToMove; timeLeft > STOP_MOVEMENT_THRESHOLD; timeLeft -= Time.fixedDeltaTime)
            {
                transform.position += direction * speed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            transform.position = new Vector3(Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),transform.position.z);
            
            if (swapsTiles) 
            foreach (TileSwapEntry swapEntry in tileSwapEntries)
            {
                if (swapEntry.clearTile) wallLayer.SetTile(swapEntry.tileToSwap, null);
                else wallLayer.SetTile(swapEntry.tileToSwap, swapEntry.swapTo);
            }
        }
        
        [System.Serializable]
        internal struct TileSwapEntry
        {
            [SerializeField] internal Vector3Int tileToSwap;
            [SerializeField] internal Tile swapTo;
            [SerializeField] internal bool clearTile;
        }
    }
}
