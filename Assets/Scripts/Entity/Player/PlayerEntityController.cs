using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using SQZL.UI;
using SQZL.World;

namespace SQZL.Entity.Player
{
    public class PlayerEntityController : MonoBehaviour
    {
        
        [Header("Player Movement & Collision Config")] [SerializeField] private float moveSpeed = 5f;
        [SerializeField] [Range(0f, 1.0e-4f)] private float blockTestThreshold = 1.0e-5f;
        [SerializeField] private float motionBias = 0.1f;

        [Header("Collision Config")] [SerializeField] private ContactFilter2D contactFilter;

        [Header("Player Control Settings")] public bool playerInputFrozen = false;

        [Header("Sword Config")]
        [SerializeField] private GameObject swordHitbox;
        [SerializeField] private float swordOffset = 0.8f;

        [Header("Bow and Arrow Config")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private float arrowSpawnOffset = 0.5f;

        [Header("Knockback Config")] [SerializeField]
        private float knockbackTime = 0.5f;

        [SerializeField] private int knockbackTilesMax = 6;
        [SerializeField] private float iframeTime = 1f;

        [Header ("Sounds")]
        public AudioClip swordSound;
        public AudioClip hurtSound;

        [Header("Transition")] [SerializeField]
        private TransitionHandler _transitionHandler;
        
        // On Start actions
        private InputAction moveAction;
        private InputAction primaryAction;
        private InputAction secondaryAction;
        private InputAction godAction;
        private InputAction switchAction;
        
        // On Start components
        private BoxCollider2D _collider2D;
        private Animator _animator;
        private SpriteRenderer _sprite;
        private Tilebody2D _tilebody2D;
        private PlayerInventory _inventory;
        private Camera _camera2D;
        
        // Animator IDs
        private int _animMovingId, _animDirectionId;
        
        // State Variables
        private Vector3 lastPosition, deltaPosition;
        private bool vBlocked = false;
        private bool godModeActive = false;
        
        // Properties
        public TilebodyDirection Facing { private set; get; } = TilebodyDirection.Down;

        internal float IFrames { private set; get; } = 0f;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Get all components
            _collider2D = GetComponent<BoxCollider2D>();
            _animator = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();
            _tilebody2D = GetComponent<Tilebody2D>();
            _inventory = GetComponent<PlayerInventory>();
            _camera2D = Camera.main;
            
            // Get animator ids
            _animMovingId = Animator.StringToHash("Moving");
            _animDirectionId = Animator.StringToHash("Direction");
            
            // Actions
            moveAction = InputSystem.actions.FindAction("Move");
            primaryAction = InputSystem.actions.FindAction("PrimaryWeapon");
            secondaryAction = InputSystem.actions.FindAction("SecondaryWeapon");
            godAction = InputSystem.actions.FindAction("GodMode");
            switchAction = InputSystem.actions.FindAction("SwitchSecondary");
            
            // Save the current y position
            lastPosition = transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            if (playerInputFrozen) return;

            if (primaryAction.WasPressedThisFrame())
            {
                playerInputFrozen = true;
                StartCoroutine(Primary());
                return;
            }

            if (secondaryAction.WasPressedThisFrame())
            {
                playerInputFrozen = true;
                StartCoroutine(Secondary());
                return;
            }

            if (godAction != null && godAction.WasPressedThisFrame())
            {
                ToggleGodMode();
            }

            
        }
        
        void FixedUpdate()
        {
            // Evalute iframes in fixed update
            if (IFrames > 0f) IFrames -= Time.fixedDeltaTime;
            
            // Check if player is frozen, skip if so
            if (playerInputFrozen) return;

            // Get the movement input value and round it to integral values
            Vector2 moveInputValue = moveAction.ReadValue<Vector2>();
            int xIn = (int)Math.Round(moveInputValue.x, MidpointRounding.AwayFromZero);
            int yIn = (int)Math.Round(moveInputValue.y, MidpointRounding.AwayFromZero);
            
            // Apply input and move player in FixedUpdate if player input is not frozen
            // Be sure to freeze player input if plannign on moving elsewhere
            // Generally MovePlayer should only be called in FixedUpdate or after FixedUpdate frame
            MovePlayer(this.moveSpeed, xIn, yIn);
            
            SetAnimatorState(xIn != 0 || yIn != 0, Facing);
        }
        
        #region MOVEMENT
        private void MovePlayer(float moveSpeed, int xIn, int yIn, bool lockFacing = false)
        {
            /*
             * 1. Vertical movement evaluates before horizontal. If holding UP/LEFT you will go all the way up first and then when you collide with the wall you will start going left.
             * 2. When switching from moving horizontally to vertically or vice versa, Link will snap to the closest whole number edge of the original axis
             *    We apply a motion bias to bias link to move in the direction of wherever he was moving originally if an input was being pressed
             *    before the horizontal to vertical or vertical to horizontal input switch
             *
             *  This took a lot of work to get right. I had to give up on using Rigidbody2D (which in retrospect didn't make much sense to use in
             *  the first place). I sincerely doubt I could write this out ever again, but you know, who knows I guess...
             */
            
            // Save the current y position and shift the y position from fixed update at the previous frame
            // The purpose of this is to take the change in y from the previous frame to the current after
            // collision is resolved (which presumably occurs after FixedUpdate? may need to verify this
            // We also compute the delta position between frames to apply a motion bias and save the last position at this frame
            deltaPosition = transform.position - lastPosition;
            lastPosition = transform.position;

            // Evaluate vertical movement first
            // Skip to horizontal if we are being blocked vertically by a wall essentially and both a vertical and horizontal input are being applied
            // We only perform the block test if we were pressing a y input in the previous frame of course
            // I realized I needed to distinguish between being blocked up or down
            Vector2 newPosition = transform.position;
            float delta = moveSpeed * Time.fixedDeltaTime;
            if (yIn != 0)
            {
                // Check if we are blocked in the direction we are trying to go in?
                // Had to modify this function to account for tilemap
                Collider2D[] vblockCheckArray = new Collider2D[4];
                Bounds blockCheckBox = new Bounds(
                    new Vector2(transform.position.x, transform.position.y + delta * yIn) + _collider2D.offset * transform.localScale,
                    (_collider2D.size - new Vector2(0.05f, 0.05f)) * transform.localScale);
                int vBlockCheckCount = Physics2D.OverlapBox(blockCheckBox.center, 
                    blockCheckBox.size,
                    0f,
                    contactFilter,
                    vblockCheckArray);
                
                for (int i = 0; i < vBlockCheckCount; i++)
                {
                    if (vblockCheckArray[i] is TilemapCollider2D)
                    {
                        Tilemap tmap = vblockCheckArray[i].gameObject.GetComponent<Tilemap>();
                        bool detected = _tilebody2D.DetectTileOverlap(blockCheckBox, 
                            (yIn == 1) ? TilebodyDirection.Up : TilebodyDirection.Down, 
                            tmap);
                        if (detected)
                        {
                            vBlocked = true;
                            break;
                        }
                    }
                    else
                    {
                        vBlocked = true;
                        break;
                    }
                }
                
                // If we are not blocked we can perform vertical motion as usual
                if (!vBlocked || xIn == 0)
                {
                    float a = Mathf.Round(transform.position.x + motionBias * Mathf.Sign(deltaPosition.x)) -
                              transform.position.x;
                    float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * yIn;

                    // The correction component (a component) for grid snapping
                    float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);

                    // Update position
                    newPosition = transform.position + new Vector3(remainderMove, b, 0f);
                    
                    // NOTE: Set facing (0 is down, 1 is right, 2 is up, 3 is left)
                    if (!lockFacing)
                        if (Mathf.Abs(b) > 0f) Facing = (TilebodyDirection)(2 * ((b < 0f) ? 0 : 1));
                        else Facing = (TilebodyDirection)(1 + 2 * ((remainderMove > 0f) ? 0 : 1));
                }
            } 
            
            if (xIn != 0 && (yIn == 0 || vBlocked)) // Then handle horizontal input (if we aren't pressing vertical input or vertical is blocked)
            {
                float a = Mathf.Round(transform.position.y + motionBias * Mathf.Sign(deltaPosition.y)) - transform.position.y;
                float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * xIn;
                float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);
                
                newPosition = transform.position + new Vector3(b, remainderMove, 0f);
                
                // NOTE: Set facing (0 is down, 1 is right, 2 is up, 3 is left)
                if (!lockFacing)
                    if (Mathf.Abs(b) > 0f) Facing = (TilebodyDirection)(1 + 2 * ((b > 0f) ? 0 : 1));
                    else Facing = (TilebodyDirection)(2 * ((remainderMove < 0f) ? 0 : 1));
            }
           
            // Reset vblocked
            vBlocked = false;
            
            /* COLLISION SOLVING */
            //Debug.Log($"Here's the new position im moving into {newPosition}");
            _tilebody2D.MoveAndCollide(newPosition, contactFilter);
        }

        /// <summary>
        /// Forces the player to move in a direction over a number of tiles within a set amount of time. NOTE that if the player
        /// is not frozen a warning will be issued and the coroutine will end immediately. It is generally advised not to try to
        /// force the player while also leaving them unfrozen.
        /// </summary>
        /// <param name="tiles">Tiles to move, must be positive (will be converted internally if not)</param>
        /// <param name="time">Time to move, must be a positive value (will be clamped to 0 or more)</param>
        /// <param name="direction">The direction in which the player will move over the span of this coroutine. Defaults to right if invalid direction is provided</param>
        /// <param name="snapToGrid">Toggle this if you would like to automatically compute movement such that the player ends on a whole number tile</param>
        /// <param name="endOnBlock">Prematurely ends the movement on block</param>
        /// <param name="lockFacing">Toggle this if you want the player to appear to face the same direction while moving</param>
        /// <returns>IEnumerator to pass into <c>StartCoroutine</c></returns>
        public IEnumerator ForcePlayerCoroutine(int tiles, float time, TilebodyDirection direction, bool snapToGrid, bool endOnBlock, bool lockFacing)
        {
            // Back out if 0 time provided
            if (time <= 0f)
            {
#if DEBUG
                Debug.LogWarning("Called ForcePlayerCoroutine for 0 seconds, breaking out of coroutine now");
#endif
                yield break;
            }

            // Back out if player is not frozen
            if (!playerInputFrozen)
            {
#if DEBUG
                Debug.LogWarning("Called ForcePlayerCoroutine without freezing player, breaking out of coroutine now");
#endif
                yield break;
            }

            // Convert tiles to positive
            if (tiles < 0) tiles = Mathf.Abs(tiles);

            // Compute move total (if needed), move speed, and determine directional parameters
            float actualMovementTotal = (float)tiles;
            if (snapToGrid)
            {
                switch (direction)
                {
                    case TilebodyDirection.Down:
                    case TilebodyDirection.Up:
                        int dirVFactor = (int)direction - 1;
                        float initY = transform.position.y;
                        float snappedY = Mathf.Round(initY + dirVFactor * tiles);
                        actualMovementTotal = Mathf.Abs(snappedY - initY);
                        break;
#if DEBUG
                    case TilebodyDirection.Diagonal:
                        Debug.LogWarning(
                            "WARNING: A player entity has been forced to move diagonally. Deferring to force horizontal movement");
                        goto case TilebodyDirection.Left;
#else
                    case TilebodyDirection.Diagonal:
#endif
                    case TilebodyDirection.Left:
                    case TilebodyDirection.Right:
                        int dirHFactor = -((int)direction - 2);
                        float initX = transform.position.x;
                        float snappedX = Mathf.Round(initX + dirHFactor * tiles);
                        actualMovementTotal = Mathf.Abs(snappedX - initX);
                        break;
                    default:
                        goto case TilebodyDirection.Diagonal;
                }
            }

            float compMoveSpeed = actualMovementTotal / time;
            int xIn, yIn; // NOTE: Defaults to right if an invalid direction is provided
            switch (direction)
            {
                case TilebodyDirection.Down:
                    yIn = -1;
                    xIn = 0;
                    break;
                case TilebodyDirection.Up:
                    yIn = 1;
                    xIn = 0;
                    break;
                case TilebodyDirection.Left:
                    yIn = 0;
                    xIn = -1;
                    break;
#if DEBUG
                case TilebodyDirection.Diagonal:
                    Debug.LogWarning(
                        "WARNING: A player entity has been forced to move diagonally or provided an invalid direction. Deferring the check on line 269 to Right");
                    goto case TilebodyDirection.Right;
#else
                case TilebodyDirection.Diagonal:
#endif
                case TilebodyDirection.Right:
                    yIn = 0;
                    xIn = 1;
                    break;
                default:
                    goto case TilebodyDirection.Diagonal;
            }

            // Set animator state
            SetAnimatorState(true, Facing);

            // Every physics frame
            for (; time > 0f; time -= Time.fixedDeltaTime)
            {
                // I'm too lazy to actually clean this up at all so
                if (endOnBlock)
                {
                    // Perform a check but for ALL directions
                    Vector2 newPosition = transform.position;
                    float delta = moveSpeed * Time.fixedDeltaTime;
                    Collider2D[] vblockCheckArray = new Collider2D[4];
                    Bounds blockCheckBox = new Bounds(
                        new Vector2(transform.position.x + delta * xIn, transform.position.y + delta * yIn) + _collider2D.offset * transform.localScale,
                        (_collider2D.size - new Vector2(0.05f, 0.05f)) * transform.localScale);
                    int vBlockCheckCount = Physics2D.OverlapBox(blockCheckBox.center, 
                        blockCheckBox.size,
                        0f,
                        contactFilter,
                        vblockCheckArray);
                    bool forceMoveBlocked = false;
                    for (int i = 0; i < vBlockCheckCount; i++)
                    {
                        if (vblockCheckArray[i] is TilemapCollider2D)
                        {
                            Tilemap tmap = vblockCheckArray[i].gameObject.GetComponent<Tilemap>();
                            bool detected = _tilebody2D.DetectTileOverlap(blockCheckBox, 
                                direction, 
                                tmap);
                            if (detected)
                            {
                                forceMoveBlocked = true;
                                break;
                            }
                        }
                        else
                        {
                            forceMoveBlocked = true;
                            break;
                        }
                    }

                    // If we are stopping
                    if (forceMoveBlocked) break;
                }
                
                
                MovePlayer(compMoveSpeed, xIn, yIn, lockFacing);
                yield return new WaitForFixedUpdate();
            }

            // End idle
            SetAnimatorState(false, Facing);

        }

        /// <summary>
        /// Call this coroutine to knock back the player
        /// </summary>
        public IEnumerator KnockbackCoroutine()
        {
            playerInputFrozen = true;
            yield return StartCoroutine(ForcePlayerCoroutine(
                knockbackTilesMax,
                knockbackTime,
                (TilebodyDirection)(((int)Facing + 2) % 4),
                true,
                true,
                true
                ));
            playerInputFrozen = false;
        }

        
        #endregion

        internal void TriggerIframes()
        {
            IFrames = iframeTime;
        }
        
        private void ToggleGodMode()
        {
            godModeActive = !godModeActive;

            if(godModeActive)
            {
                //Disable hurtbox and set items to max
                Debug.Log("God Mode Enabled");
            }
            else
            {
                //Reenable hurtbox
                Debug.Log("God Mode Disabled");
            }
        }

        #region EXTERNAL_CONTROL
        /// <summary>
        /// Used to teleport the player. Best used when the player controller is fully disabled
        /// </summary>
        public void TeleportPlayer(Vector3 position, Vector3 camPos)
        {
            transform.position = position;
            lastPosition = position;
            deltaPosition = Vector3.zero;
            _camera2D.gameObject.transform.position = new Vector3(camPos.x, camPos.y, _camera2D.gameObject.transform.position.z);
        }

        /// <summary>
        /// Used to set the player's full visual state. Best used when the player
        /// controller is fully disabled if being called externally
        /// </summary>
        public void SetAnimatorState(bool walking, TilebodyDirection direction)
        {
            _animator.SetInteger(_animDirectionId, (int)direction);
            _animator.SetBool(_animMovingId, walking);
        }

        public void WaitForTime(float waitTime)
        {
            StartCoroutine(FreezeEnumerator(waitTime));
        }

        private IEnumerator FreezeEnumerator(float waitTime)
        {
            playerInputFrozen = true;
            yield return new WaitForSeconds(waitTime);
            playerInputFrozen = false;
        }
        #endregion
        
        #region ATTACKS
        IEnumerator Primary()
        {
            ExecutePrimaryAttack(Facing);
            yield return null;
            float attackLength = _animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(attackLength);
            if (swordHitbox != null)
            {
                swordHitbox.SetActive(false);
            }
            SetAnimatorState(false, Facing);
            playerInputFrozen = false;
        }

        private void ExecutePrimaryAttack(TilebodyDirection dir)
        {
            if (swordHitbox != null)
            {
                // TODO: Cache this, suboptimal but lazy
                Hurtbox swordHurtbox = swordHitbox.GetComponent<Hurtbox>();
                
                Vector3 offsetVector = Vector3.zero;
                _animator.SetTrigger("Attack");
                _animator.SetInteger("Direction", (int)Facing);
                switch (dir)
                {
                    case TilebodyDirection.Down:
                        offsetVector = Vector3.down * swordOffset;
                        break;
                    case TilebodyDirection.Right:
                        offsetVector = Vector3.right * swordOffset;
                        break;
                    case TilebodyDirection.Left:
                        offsetVector = Vector3.left * swordOffset;
                        break;
                    case TilebodyDirection.Up:
                        offsetVector = Vector3.up * swordOffset;
                        break;
                }

                swordHurtbox.AttackDirection = dir;
                swordHitbox.transform.localPosition = offsetVector;
                swordHitbox.SetActive(true);
            }
        }
        IEnumerator Secondary()
        {
            Debug.Log("Secondary Attack Started!");
            ExecuteSecondaryAttack(Facing);
            playerInputFrozen = false;
            yield return null;
        }

        private void ExecuteSecondaryAttack(TilebodyDirection dir)
        {
            if (arrowPrefab == null) return;

            if (_inventory.GetConsumableAmount("rupee") > 0)
                _inventory.AccumulateConsumable("rupee", -1);
            else
                return;
            
            Vector2 fireDirection = Vector2.down;
            float zRotation = 0f;

            switch (dir)
            {
                case TilebodyDirection.Down:
                    fireDirection = Vector2.down;
                    zRotation = 180f;
                    break;
                case TilebodyDirection.Right:
                    fireDirection = Vector2.right;
                    zRotation = -90f;
                    break;
                case TilebodyDirection.Left:
                    fireDirection = Vector2.left;
                    zRotation = 90f;
                    break;
                case TilebodyDirection.Up:
                    fireDirection = Vector2.up;
                    zRotation = 0f;
                    break;
            }

            //Calculate spawn position in front of Link
            Vector3 spawnPos = transform.position + (Vector3)(fireDirection * arrowSpawnOffset);

            //Spawn arrow with correct rotation
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, zRotation);
            GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, spawnRotation);

            //
            PlayerArrow playerArrowScript = arrowObj.GetComponent<PlayerArrow>();
            if (playerArrowScript != null)
            {
                playerArrowScript.Initialize(fireDirection);
            }

        }
        #endregion
        
        #region TRANSITION

        private RoomSpawnManager tpRoom;
        public void WarpWithSlide(RoomSpawnManager to)
        {
            _transitionHandler.OnSlideTransitionEnd += OnSlideFinished;
            _transitionHandler.TriggerSlide();
            tpRoom = to;
            TeleportPlayer(to.WarpPosition.position, to.CameraWarpPosition.position); 
        }

        public void OnSlideFinished()
        {
            _transitionHandler.OnSlideTransitionEnd -= OnSlideFinished;
            RoomManager._Instance.LoadRoom(tpRoom);
            if (playerInputFrozen) playerInputFrozen = false;
        }
        #endregion
    }
}
