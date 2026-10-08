using System;
using System.Collections;
using UnityEngine;
using SQZL.Entity.Enemy;
using SQZL.Entity.Player;
using SQZL.World;

namespace SQZL.Entity.Enemy.Hazard
{
    public class WallmasterController : BaseEnemyController, IHazard
    {

        [Header("Movement Config")] [SerializeField] 
        [Tooltip("Specifies the direction this wallmaster moves towards upon entry")]
        private TilebodyDirection appearDirection;
        [SerializeField] [Tooltip("Moves CCW if false")] private bool movesClockwise;
        [SerializeField] private int exitDepthUnits;
        [SerializeField] private int travelLengthUnits;
        
        [Header("References")]
        [SerializeField] private PlayerEntityController playerRef;
        [SerializeField] private WallmasterGrabbox grabBox;
        [SerializeField] private RoomSpawnManager tpRoom;
        
        private bool tripped = false;
        private bool grabbed = false;
        private Vector3 exitDir, travelDir;

        private Animator _animator;
        private int _a_Damaged;

        protected override void Start()
        {
            base.Start();
            switch (appearDirection)
            {
                case TilebodyDirection.Down:
                    exitDir = Vector3.down;
                    break;
                case TilebodyDirection.Up:
                    exitDir = Vector3.up;
                    break;
                case TilebodyDirection.Left:
                    exitDir = Vector3.left;
                    break;
                case TilebodyDirection.Right:
                    default:
                        exitDir = Vector3.right;
                        break;
            }

            if (movesClockwise)
            {
                travelDir = new Vector2(exitDir.y, -exitDir.x);
            }
            else
            {
                travelDir = new Vector2(-exitDir.y, exitDir.x);
            }

            _animator = GetComponent<Animator>();
            _a_Damaged = Animator.StringToHash("Damaged");
        }
        
        public override void TryDamage(int amount, bool knockback, TilebodyDirection attackDir)
        {
            if (iframes <= 0f)
            {
                currentHP -= amount;
                if (currentHP <= 0f)
                {
                    base.DisconnectSpawnManager();
                    OnHazardDestroyed?.Invoke(this);
                    Destroy(gameObject);
                }
                else
                {
                    iframes = iframeTime;
                    _animator.SetBool(_a_Damaged, true);
                }
            }
        }

        public event Action<IHazard> OnHazardTripped;
        public event Action<IHazard> OnHazardDestroyed;

        void IHazard.TripHazard(HazardTripBox trigger)
        {
            TripHazard(trigger);
        }

        internal void TripHazard(HazardTripBox trigger)
        {
            if (tripped) return;
            OnHazardTripped?.Invoke(this);
            tripped = true;
            StartCoroutine(TripCoroutine());
        }

        internal void TriggerGrabPlayer()
        {
            if (playerRef.playerInputFrozen) return; // i.e. someone else grabbed the player alr
            // TODO: Make this reflect in a coroutine
            RoomManager._Instance.UnloadRoom();
            playerRef.playerInputFrozen = true;
            grabbed = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (iframes <= 0f) _animator.SetBool(_a_Damaged, false);
            if (grabbed)
            {
                playerRef.TeleportPlayer(transform.position, Camera.main.transform.position);
            }
        }
        
        
        
        private const float MOTION_END_THRESHOLD = 0.2f;
        private IEnumerator TripCoroutine()
        {
            // Make a rotation
            float lengthTime = travelLengthUnits / moveSpeed;
            float exitTime = exitDepthUnits / moveSpeed;

            // EXIT
            for (float x = exitTime; x > MOTION_END_THRESHOLD; x -= Time.fixedDeltaTime)
            {
                transform.position += exitDir * moveSpeed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            transform.position = new Vector3(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z
            );
            
            // TRAVEL
            for (float x = lengthTime; x > MOTION_END_THRESHOLD; x -= Time.fixedDeltaTime)
            {
                transform.position += travelDir * moveSpeed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            transform.position = new Vector3(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z
            );
            
            // ENTRY
            for (float x = exitTime; x > MOTION_END_THRESHOLD; x -= Time.fixedDeltaTime)
            {
                transform.position -= exitDir * moveSpeed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            transform.position = new Vector3(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z
            );
            
            // Check if we grabbed something, teleport accordingly
            if (grabbed)
            {
                grabbed = false; // Release here
                playerRef.WarpWithSlide(tpRoom);
            }
            //playerRef.TeleportPlayer(tpRoom.WarpPosition.position, tpRoom);
            //playerRef.playerInputFrozen = false;
            //RoomManager._Instance.LoadRoom(tpRoom);
            
            // RECHARGE
            for (float x = lengthTime; x > MOTION_END_THRESHOLD; x -= Time.fixedDeltaTime)
            {
                transform.position -= travelDir * moveSpeed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            transform.position = new Vector3(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z
            );

            tripped = false;

        }
    }
}
