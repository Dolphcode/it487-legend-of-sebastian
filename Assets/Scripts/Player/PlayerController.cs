using System.Collections;
using Math = System.Math;
using MidpointRounding = System.MidpointRounding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Player
{
    /// <summary>
    /// PlayerController represents an emulation of the player controller for Link in the NES version of The Legend of Zelda. The goal
    /// of this script is to closely emulate the grid-based movement of Link in TLoZ. This includes some of the fun little movement quirks
    /// which are described in the FixedUpdate function of this class. Essentially custom collision resolution and tile-based movement
    /// is implemented in order to make this function as closely to the original as possible. This is purely a PlayerController, and does not
    /// at all represent other data/constructs suc has health or inventory. This will be implemented in a separate module.
    /// TODO: A lot of this code could probably be repeated for enemies. The difference being that Player is controlled by input while
    ///       enemies are controlled by an enemy brain. THUS, it might be helpful to extract some of these functions and fields to
    ///       an abstract class which PlayerController and EnemyController might extend. Something like an EntityController base class?
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// TODO: Convert any references to facing or direction to PlayerDirection type for easier readability
        /// </summary>
        public enum PlayerDirection
        {
            DOWN,
            RIGHT,
            UP,
            LEFT
        }

        [Header("Movement Config")] [SerializeField] private float moveSpeed = 5f;
        [SerializeField] [Range(0f, 1.0e-4f)] private float blockTestThreshold = 1.0e-5f;
        [SerializeField] private float positionSnapThreshold = 1f/32f;
        [SerializeField] private float motionBias = 0.1f;
        [SerializeField] private float overlapOvershootMax = 0.2f;

        [Header("Collision Config")] [SerializeField] private ContactFilter2D contactFilter;

        [Header("Player Control Settings")] public bool playerInputFrozen = false;

        [Header("Sword Config")]
        [SerializeField] private GameObject swordHitbox;
        [SerializeField] private float swordOffset = 0.8f;

        [Header("Bow and Arrow Config")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private float arrowSpawnOffset = 0.5f;
        
        // On Start actions
        private InputAction moveAction;
        private InputAction primaryAction;
        private InputAction secondaryAction;
        private InputAction godAction;
        
        // On Start components
        private BoxCollider2D collider2D;
        private Animator animator;
        private SpriteRenderer sprite;
        private PlayerInventory inventory;
        
        // Animator IDs
        private int _animMovingId, _animDirectionId;
        
        // State Variables
        private float currY = 0f, expectedY = 0f;
        private int prevYIn = 0;
        private bool snappedToGridFlag = false;
        private Vector3 lastPosition, deltaPosition;
        private bool vBlocked = false;
        public int facing = 0; // 0 = down, 1 = right, 2 = up, 3 = left
        private bool godModeActive = false;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Get all components
            collider2D = GetComponent<BoxCollider2D>();
            animator = GetComponent<Animator>();
            sprite = GetComponent<SpriteRenderer>();
            inventory = GetComponent<PlayerInventory>();
            
            // Get animator ids
            _animMovingId = Animator.StringToHash("Moving");
            _animDirectionId = Animator.StringToHash("Direction");
            
            
            // Actions
            moveAction = InputSystem.actions.FindAction("Move");
            primaryAction = InputSystem.actions.FindAction("PrimaryWeapon");
            secondaryAction = InputSystem.actions.FindAction("SecondaryWeapon");
            godAction = InputSystem.actions.FindAction("GodMode");
            
            // Save the current y position
            currY = transform.position.y;
            lastPosition = transform.position;
            expectedY = transform.position.y;
        }

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
        // Fixed Update is called once per physics frame
        void FixedUpdate()
        {
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
            
            SetAnimatorState(xIn != 0 || yIn != 0, (PlayerDirection)facing);

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
        /// <returns>IEnumerator to pass into <c>StartCoroutine</c></returns>
        public IEnumerator ForcePlayerCoroutine(int tiles, float time, PlayerDirection direction, bool snapToGrid)
        {
            // Back out if 0 time provided
            if (time <= 0f)
            {
                Debug.LogWarning("Called ForcePlayerCoroutine for 0 seconds, breaking out of coroutine now");
                yield break;
            }
            
            // Back out if player is not frozen
            if (!playerInputFrozen)
            {
                Debug.LogWarning("Called ForcePlayerCoroutine without freezing player, breaking out of coroutine now");
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
                    case PlayerDirection.DOWN:
                    case PlayerDirection.UP:
                        int dirVFactor = (int)direction - 1;
                        float initY = transform.position.y;
                        float snappedY = Mathf.Round(initY + dirVFactor * tiles);
                        actualMovementTotal = Mathf.Abs(snappedY - initY);
                        break;
                    case PlayerDirection.LEFT:
                    case PlayerDirection.RIGHT:
                    default:
                        int dirHFactor = -((int)direction - 2);
                        float initX = transform.position.x;
                        float snappedX = Mathf.Round(initX + dirHFactor * tiles);
                        actualMovementTotal = Mathf.Abs(snappedX - initX);
                        break;
                }
            }
            
            float compMoveSpeed = actualMovementTotal / time;
            int xIn, yIn; // NOTE: Defaults to right if an invalid direction is provided
            switch (direction)
            {
                case PlayerDirection.DOWN:
                    yIn = -1;
                    xIn = 0;
                    break;
                case PlayerDirection.UP:
                    yIn = 1;
                    xIn = 0;
                    break;
                case PlayerDirection.LEFT:
                    yIn = 0;
                    xIn = -1;
                    break;
                case PlayerDirection.RIGHT:
                default:
                    yIn = 0;
                    xIn = 1;
                    break;
            }
            
            // Set animator state
            SetAnimatorState(true, direction);

            // Every physics frame
            for (; time > 0f; time -= Time.fixedDeltaTime)
            {
                MovePlayer(compMoveSpeed, xIn, yIn);
                yield return new WaitForFixedUpdate();
            }
            
            // End idle
            SetAnimatorState(false, direction);
            
            // TODO: REMOVE LATER
            playerInputFrozen = false;
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
            
        IEnumerator Primary()
        {
            Debug.Log("Primary Attack Started!");
            ExecutePrimaryAttack((PlayerDirection)facing);
            yield return null;
            float attackLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(attackLength);
            if (swordHitbox != null)
            {
                swordHitbox.SetActive(false);
            }
            SetAnimatorState(false, ((PlayerDirection)facing));
            playerInputFrozen = false;
        }

        private void ExecutePrimaryAttack(PlayerDirection dir)
        {
            if (swordHitbox != null)
            {
                Vector3 offsetVector = Vector3.zero;
                animator.SetTrigger("Attack");
                animator.SetInteger("Direction", facing);
                switch (dir)
                {
                    case PlayerDirection.DOWN:
                        offsetVector = Vector3.down * swordOffset;
                        break;
                    case PlayerDirection.RIGHT:
                        offsetVector = Vector3.right * swordOffset;
                        break;
                    case PlayerDirection.LEFT:
                        offsetVector = Vector3.left * swordOffset;
                        break;
                    case PlayerDirection.UP:
                        offsetVector = Vector3.up * swordOffset;
                        break;
                }

                swordHitbox.transform.localPosition = offsetVector;
                swordHitbox.SetActive(true);
            }
        }
        IEnumerator Secondary()
        {
            Debug.Log("Secondary Attack Started!");
            ExecuteSecondaryAttack((PlayerDirection)facing);
            playerInputFrozen = false;
            yield return null;
        }

        private void ExecuteSecondaryAttack(PlayerDirection dir)
        {
            if (arrowPrefab == null) return;

            if (inventory.GetConsumableAmount("rupee") > 0)
                inventory.AccumulateConsumable("rupee", -1);
            else
                return;
            
            Vector2 fireDirection = Vector2.down;
            float zRotation = 0f;

            switch (dir)
            {
                case PlayerDirection.DOWN:
                    fireDirection = Vector2.down;
                    zRotation = 180f;
                    break;
                case PlayerDirection.RIGHT:
                    fireDirection = Vector2.right;
                    zRotation = -90f;
                    break;
                case PlayerDirection.LEFT:
                    fireDirection = Vector2.left;
                    zRotation = 90f;
                    break;
                case PlayerDirection.UP:
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
            Arrow arrowScript = arrowObj.GetComponent<Arrow>();
            if (arrowScript != null)
            {
                arrowScript.Initialize(fireDirection);
            }

        }
        private void MovePlayer(float moveSpeed, int xIn, int yIn)
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
            currY = transform.position.y;

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
                    new Vector2(transform.position.x, transform.position.y + delta * yIn) + collider2D.offset * transform.localScale,
                    (collider2D.size - new Vector2(0.05f, 0.05f)) * transform.localScale);
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
                        bool detected = DetectTileCollision(blockCheckBox, yIn, tmap);
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
                if (!vBlocked)
                {
                    float a = Mathf.Round(transform.position.x + motionBias * Mathf.Sign(deltaPosition.x)) -
                              transform.position.x;
                    float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * yIn;

                    // The correction component (a component) for grid snapping
                    float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);

                    // Update position
                    newPosition = transform.position + new Vector3(remainderMove, b, 0f);
                    transform.position = newPosition;
                    
                    // NOTE: Set facing (0 is down, 1 is right, 2 is up, 3 is left)
                    if (Mathf.Abs(b) > 0f) facing = 2 * ((b < 0f) ? 0 : 1);
                    else facing = 1 + 2 * ((remainderMove > 0f) ? 0 : 1);
                }
            } 
            
            if (xIn != 0 && (yIn == 0 || vBlocked)) // Then handle horizontal input (if we aren't pressing vertical input or vertical is blocked)
            {
                float a = Mathf.Round(transform.position.y + motionBias * Mathf.Sign(deltaPosition.y)) - transform.position.y;
                float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * xIn;
                float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);
                
                newPosition = transform.position + new Vector3(b, remainderMove, 0f);
                transform.position = newPosition;
                
                // NOTE: Set facing (0 is down, 1 is right, 2 is up, 3 is left)
                if (Mathf.Abs(b) > 0f) facing = 1 + 2 * ((b > 0f) ? 0 : 1);
                else facing = 2 * ((remainderMove < 0f) ? 0 : 1);
            }
            
            // Save what the y should be without collision resolution
            expectedY = transform.position.y;
            
            // Save this yIn as the previous yIn
            prevYIn = yIn;
           
            // Reset vblocked
            vBlocked = false;
            
            /* COLLISION SOLVING */
            Collider2D[] colliders = new Collider2D[4];
            Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + collider2D.offset * transform.localScale,
                (collider2D.size) * transform.localScale);
            
            int overlapCount = Physics2D.OverlapBox(ours.center, 
                ours.size,
                0f,
                contactFilter,
                colliders);
            for (int i = 0; i < overlapCount; i++)
            {
                // Specify other
                Collider2D other = colliders[i];

                if (other is TilemapCollider2D) ResolveTileCollision(other.gameObject.GetComponent<Tilemap>());
                else ResolveBasicCollision(other);
                
            }
        }

        private void ResolveTileCollision(Tilemap other)
        {
            Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + collider2D.offset * transform.localScale,
                collider2D.size * transform.localScale);
            Vector2[] boundCorners = new Vector2[4];
            // Ordered so i can select corners to check based on distance
            boundCorners[0] = ours.min;
            boundCorners[2] = ours.max;
            boundCorners[3] = new Vector2(ours.min.x, ours.max.y);
            boundCorners[1] = new Vector2(ours.max.x, ours.min.y);
            
            // Perform resolution for edge based on distance
            int pointA = facing, pointB = (facing + 1) % 4; // 0 -> 0,1, 1 -> 1,2, 2 -> 2,3, 3 -> 3,0
            // Get grid tiles
            Vector2 direction = Vector2.zero;
            switch (facing)
            {
                case 0:
                    direction = Vector2.down; break;
                case 1:
                    direction = Vector2.right; break;
                case 2:
                    direction = Vector2.up; break;
                case 3:
                    direction = Vector2.left; break;
            }

            // TODO: Convert this to a serialize field
            float epsilon = 1e-1f;
            
            // So to compute world to cell, all Unity does is a simple floor operation
            // So presumably, the way this works is because everything is scaled up 2x, we take the bound corners we're trying to convert, divide by 2
            // then floor. So theoretically, adding 1 is equivalent to dividing by 2, adding 0.5, and then computing the floor. Note that
            // floor(x + 0.5) is functionally equivalent to round(x). So floor(0.5(2x + 1)) -> round(x) which is probably what we want for
            // collisions to function correctly. Otherwise we got some weeeeeird jank : (
            Vector3Int tileA = other.WorldToCell(boundCorners[pointA] + direction * epsilon + Vector2.one), tileB = other.WorldToCell(boundCorners[pointB] + direction * epsilon + Vector2.one);
            if (tileA.x == tileB.x) // Vertical
            {
                for (int i = Mathf.Min(tileA.y, tileB.y); i <= Mathf.Max(tileA.y, tileB.y); i++)
                {
                    Vector3Int checkLoc = new Vector3Int(tileA.x, i, 0);
                    ResolveTileCollision(checkLoc, other);
                }
            }
            else if (tileA.y == tileB.y) // Assume horizontal
            {
                for (int i = Mathf.Min(tileA.x, tileB.x); i <= Mathf.Max(tileA.x, tileB.x); i++)
                {
                    Vector3Int checkLoc = new Vector3Int(i, tileA.y, 0);
                    ResolveTileCollision(checkLoc, other);
                }
            }
            else
            {
                for (int i = Mathf.Min(tileA.x, tileB.x); i <= Mathf.Max(tileA.x, tileB.x); i++)
                for (int j = Mathf.Min(tileA.y, tileB.y); j <= Mathf.Max(tileA.y, tileB.y); j++)
                {
                    Vector3Int checkLoc = new Vector3Int(i, j, 0);
                    ResolveTileCollision(checkLoc, other);
                }
            }

        }

        private void ResolveBasicCollision(Collider2D other)
        {
            // Grab bounds and determine signed overlap vector
            Bounds others = other.bounds;
            Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + collider2D.offset * transform.localScale,
                collider2D.size * transform.localScale);
            
            ResolveBoundsCollision(others, ours); 
        }

        private void ResolveTileCollision(Vector3Int checkLoc, Tilemap tmap)
        {
            if (tmap.HasTile(checkLoc))
            {
                Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + collider2D.offset * transform.localScale,
                    collider2D.size * transform.localScale);
                Bounds others;
                
                // Check if tile has special collision data
                Tile t = tmap.GetTile<Tile>(checkLoc);
                if (t is CollidingTile)
                {
                    CollidingTile c = t as CollidingTile;
                    Bounds collisionExtents = c.GetBounds();
                    collisionExtents.center = Vector3.Scale(collisionExtents.center, tmap.transform.localScale);
                    collisionExtents.extents = Vector3.Scale(collisionExtents.extents, tmap.transform.localScale);
                    others = new Bounds(tmap.GetCellCenterWorld(checkLoc) + Vector3.Scale(tmap.cellSize, collisionExtents.center), Vector3.Scale(tmap.cellSize, collisionExtents.size));
                } else others = new Bounds(tmap.GetCellCenterWorld(checkLoc), Vector3.Scale(tmap.cellSize, tmap.transform.localScale));
                
                ResolveBoundsCollision(others, ours);
            }
        }
        
        private bool DetectTileCollision(Bounds ours, int dir, Tilemap tmap)
        {
            Vector2[] boundCorners = new Vector2[4];
            
            // Ordered so i can select corners to check based on distance
            boundCorners[0] = ours.min;
            boundCorners[2] = ours.max;
            boundCorners[3] = new Vector2(ours.min.x, ours.max.y);
            boundCorners[1] = new Vector2(ours.max.x, ours.min.y);
            int pointA = dir, pointB = (facing + 1) % 4; // 0 -> 0,1, 1 -> 1,2, 2 -> 2,3, 3 -> 3,0
            if (dir == 1)
            {
                pointA = 2;
                pointB = 3;
            }
            else
            {
                pointA = 0;
                pointB = 1;
            }
            
            // TODO: Convert this to a serialize field
            float epsilon = 1e-1f;
            
            // So to compute world to cell, all Unity does is a simple floor operation
            // So presumably, the way this works is because everything is scaled up 2x, we take the bound corners we're trying to convert, divide by 2
            // then floor. So theoretically, adding 1 is equivalent to dividing by 2, adding 0.5, and then computing the floor. Note that
            // floor(x + 0.5) is functionally equivalent to round(x). So floor(0.5(2x + 1)) -> round(x) which is probably what we want for
            // collisions to function correctly. Otherwise we got some weeeeeird jank : (
            Vector3Int tileA = tmap.WorldToCell(boundCorners[pointA] + dir * epsilon * Vector2.up + Vector2.one), tileB = tmap.WorldToCell(boundCorners[pointB] + dir * epsilon * Vector2.up + Vector2.one);
            for (int i = Mathf.Min(tileA.x, tileB.x); i <= Mathf.Max(tileA.x, tileB.x); i++)
            {
                Vector3Int checkLoc = new Vector3Int(i, tileA.y, 0);
                
                if (tmap.HasTile(checkLoc))
                {
                    // Bounds for the tile we are checking at this moment
                    Bounds others;
                
                    // Check if tile has special collision data
                    Tile t = tmap.GetTile<Tile>(checkLoc);
                    if (t is CollidingTile)
                    {
                        CollidingTile c = t as CollidingTile;
                        Bounds collisionExtents = c.GetBounds();
                        collisionExtents.center = Vector3.Scale(collisionExtents.center, tmap.transform.localScale);
                        collisionExtents.extents = Vector3.Scale(collisionExtents.extents, tmap.transform.localScale);
                        others = new Bounds(tmap.GetCellCenterWorld(checkLoc) + Vector3.Scale(tmap.cellSize, collisionExtents.center), Vector3.Scale(tmap.cellSize, collisionExtents.size));
                    } else others = new Bounds(tmap.GetCellCenterWorld(checkLoc), Vector3.Scale(tmap.cellSize, tmap.transform.localScale));
                    
                    // Compute overlaps
                    float overlapX = Mathf.Min(ours.max.x, others.max.x) - Mathf.Max(ours.min.x, others.min.x);
                    float overlapY = Mathf.Min(ours.max.y, others.max.y) - Mathf.Max(ours.min.y, others.min.y);
            
                    // Cut short if neither is overlapping, continue
                    if (overlapX <= 0.1f || overlapY <= 0.1f) continue;
                    else return true;
                }
            }

            // No tile collision detected
            return false;
        }

        private void ResolveBoundsCollision(Bounds others, Bounds ours)
        {
            // Compute overlaps
            float overlapX = Mathf.Min(ours.max.x, others.max.x) - Mathf.Max(ours.min.x, others.min.x);
            float overlapY = Mathf.Min(ours.max.y, others.max.y) - Mathf.Max(ours.min.y, others.min.y);
            
            // Cut short if neither is overlapping
            if (overlapX <= 0.1f || overlapY <= 0.1f) return;
            
            // Resolve the smaller overlap
            // But prioritize horizontal over vertical
            if (overlapX <= overlapY && overlapX > 0.1f)
            {
                float correctionDir = -Mathf.Sign(others.center.x - ours.center.x);
                transform.position += Vector3.right * correctionDir * overlapX;
            }
            else
            {
                float correctionDir = -Mathf.Sign(others.center.y - ours.center.y);
                transform.position += Vector3.up * correctionDir * overlapY;
            }
        }

        private void SetAnimatorState(bool walking, PlayerDirection direction)
        {
            animator.SetInteger(_animDirectionId, (int)direction);
            animator.SetBool(_animMovingId, walking);
        }

        public Vector2Int GetFacingDirection()
        {
            switch ((PlayerDirection)facing)
            {
                case PlayerDirection.DOWN: return Vector2Int.down;
                case PlayerDirection.UP: return Vector2Int.up;
                case PlayerDirection.LEFT: return Vector2Int.left;
                case PlayerDirection.RIGHT: return Vector2Int.right;
                default: return Vector2Int.zero;
            }
        }

        public void GameOver()
        {
            Debug.Log("Game Over!");
            playerInputFrozen = true;
            //animator.Play("link_die", 0, 0f);
        }

    }
}
