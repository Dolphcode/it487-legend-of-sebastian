using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.Entity;

namespace SQZL.Entity.Enemy
{
    public class TileEnemyController : BaseEnemyController
    {
        [Header("Knockback Config")]
        [Tooltip("The entity does not need to use these values, only if they are able to be knocked back for the sake of providing common functionality across tilemap enemies")]
        [SerializeField] protected int knockbackTilesMax = 10;
        [SerializeField] protected float knockbackSpeed = 20f;
        
        // State Variables
        protected Vector3 lastPosition, deltaPosition;
       
        // Walking state
        protected float moveAmount = 0f;
        protected float walkTimeLeft = 0f;
        protected Vector2Int moveDirection = Vector2Int.zero;
        
        // Properties
        public TilebodyDirection Facing { protected set; get; } = TilebodyDirection.Down;

        protected override void Awake()
        {
            base.Awake();
            lastPosition = transform.position;
        }

        #region MOVEMENT_FUNCS
        protected void MoveEntity(float moveSpeed, int xIn, int yIn, bool lockFacing = true)
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
            
            if (xIn != 0) // Then handle horizontal input (if we aren't pressing vertical input or vertical is blocked)
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
        public IEnumerator ForceEntityCoroutine(int tiles, float time, TilebodyDirection direction, bool snapToGrid, bool endOnBlock, bool lockFacing)
        {
            // Back out if 0 time provided
            if (time <= 0f)
            {
#if DEBUG
                Debug.LogWarning("Called ForceEntity for 0 seconds, breaking out of coroutine now");
#endif
                yield break;
            }

            // Back out if player is not frozen
            if (ControllerIsActive)
            {
#if DEBUG
                Debug.LogWarning("Called ForceEntity without freezing controller, breaking out of coroutine now");
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
                
                
                MoveEntity(compMoveSpeed, xIn, yIn, lockFacing);
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Call this coroutine to knock back the entity
        /// Required by Goriya and Stalfos specifically iirc?
        /// </summary>
        public IEnumerator KnockbackCoroutine()
        {
            ControllerIsActive = false;
            iframes = iframeTime;
            yield return StartCoroutine(ForceEntityCoroutine(
                knockbackTilesMax,
                knockbackSpeed,
                (TilebodyDirection)(((int)Facing + 2) % 4),
                true,
                true,
                true
            ));
            ControllerIsActive = true;
            PickDirection();
        }

        /// <summary>
        /// A common pick direction functionality
        /// TODO: hasMaxTiles and maxTiles do nothing atm
        /// </summary>
        protected void PickDirection()
        {
            Vector2Int currentTilePosition = new Vector2Int(Mathf.RoundToInt(transform.position.x - walkableRegionOffset.x),
                Mathf.RoundToInt(transform.position.y - walkableRegionOffset.y)) / 2;
            List<TilebodyDirection> validDirections = new List<TilebodyDirection>() {TilebodyDirection.Left, TilebodyDirection.Down, TilebodyDirection.Right, TilebodyDirection.Up};
            
            // Check alignment to restrict movement
            bool xAligned = Mathf.RoundToInt((transform.position.x - walkableRegionOffset.x) / 2f) * 2
                            == Mathf.RoundToInt(transform.position.x - walkableRegionOffset.x);

            bool yAligned = Mathf.RoundToInt((transform.position.y - walkableRegionOffset.y) / 2f) * 2
                            == Mathf.RoundToInt(transform.position.y - walkableRegionOffset.y);
            if (!xAligned)
            {
                validDirections.Remove(TilebodyDirection.Up);
                validDirections.Remove(TilebodyDirection.Down);
            }
            else if (!yAligned)
            {
                validDirections.Remove(TilebodyDirection.Left);
                validDirections.Remove(TilebodyDirection.Right);
            }
            
            Vector2Int chosenDirection = Vector2Int.zero;
            while (validDirections.Count > 0)
            {
                TilebodyDirection direction = validDirections[Random.Range(0, validDirections.Count)];
                Vector2Int directionTest = Vector2Int.zero;
                switch (direction)
                {
                    case TilebodyDirection.Left: directionTest = Vector2Int.left; break;
                    case TilebodyDirection.Right: directionTest = Vector2Int.right; break;
                    case TilebodyDirection.Down: directionTest = Vector2Int.down; break;
                    case TilebodyDirection.Up: directionTest = Vector2Int.up; break;
                }

                Vector2Int positionTest = directionTest + currentTilePosition;
                int index = positionTest.y * walkableRegion.x + positionTest.x;
                if (positionTest.x < 0 || positionTest.x >= walkableRegion.x || positionTest.y < 0 ||
                    positionTest.y >= walkableRegion.y || !validTiles[index])
                {
                    validDirections.Remove(direction);
                }
                else
                {
                    chosenDirection = directionTest;
                    Facing = direction;
                    break;
                }
            }

            if (chosenDirection == Vector2Int.zero)
            {
                Debug.LogError("Enemy is starting outside of its boundary region");
                walkTimeLeft = 10f;
                moveDirection = chosenDirection;
                return;
            }

            Vector2Int targetPositionTest = currentTilePosition + chosenDirection;
            int maxDist;
            for (maxDist = 1;
                 targetPositionTest.x >= 0 && targetPositionTest.x < walkableRegion.x && targetPositionTest.y >= 0 &&
                 targetPositionTest.y < walkableRegion.y && 
                 validTiles[targetPositionTest.y * walkableRegion.x + targetPositionTest.x];
                 targetPositionTest = currentTilePosition + chosenDirection * ++maxDist) ;
            maxDist--;

            moveAmount = Random.Range(1, maxDist) * 2;

            float position = chosenDirection.x != 0
                ? transform.position.x - walkableRegionOffset.x
                : transform.position.y - walkableRegionOffset.y;

            float remainder = Mathf.Repeat(position, 2f);

            if (remainder > 0.001f)
            {
                float correction = 2f - remainder;
                moveAmount += correction;
                moveAmount -= 2f;
            }
            
            moveDirection = chosenDirection;
            walkTimeLeft = moveAmount / moveSpeed;
        }
        
        #endregion
    }
}