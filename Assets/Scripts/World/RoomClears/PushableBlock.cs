using System.Collections;
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
            base.Start();
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
                body.LastMovedDirection == pushDirection &&
                body.gameObject.CompareTag("Player"))
            {
                PlayerEntityController e = body.gameObject.GetComponent<PlayerEntityController>();
                triggered = true;
                e.WaitForTime(timeToMove);
                StartCoroutine(PushCoroutine());
            }
        }

        private IEnumerator PushCoroutine()
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
