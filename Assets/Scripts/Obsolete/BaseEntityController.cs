using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using SQZL.World.Interactable;
using SQZL.World;

[Obsolete("This class is obsolete, see SQZL.Entity.Enemy for new enemy implementation")]
public class BaseEntityController : MonoBehaviour
{
    public enum PlayerDirection
    {
        DOWN,
        RIGHT,
        UP,
        LEFT
    }

    [Header("Movement Config")] [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField] [Range(0f, 1.0e-4f)] private float blockTestThreshold = 1.0e-5f;
    [SerializeField] private float positionSnapThreshold = 1f / 32f;
    [SerializeField] private float motionBias = 0.1f;
    [SerializeField] private float overlapOvershootMax = 0.2f;

    [Header("Collision Config")] [SerializeField]
    private ContactFilter2D contactFilter;

    [Header("Spawn Config")] [SerializeField]
    private List<RoomTransitionTrigger> gates;

    [Header("Health Config")] [SerializeField]
    private int hp = 2;

    [SerializeField] private int knockbackAmount = 10;
    [SerializeField] private float knockbackSpeed = 20f;

    // On Start components
    private BoxCollider2D collider2D;
    private Animator animator;
    private SpriteRenderer sprite;

    // State Variables
    private float currY = 0f, expectedY = 0f;
    private int prevYIn = 0;
    private bool snappedToGridFlag = false;
    private Vector3 lastPosition, deltaPosition;
    private bool vBlocked = false;
    private int facing = 0; // 0 = down, 1 = right, 2 = up, 3 = left
    
    // Saved start position
    private Vector3 startPosition;
    
    [Header("Pathing Config")] [SerializeField]
    private Vector2Int walkableRegion;

    [SerializeField] private Vector2Int walkableRegionOffset;
    [SerializeField] private List<Tilemap> wallTilemaps;

    // Walktable region map
    private bool[] validTiles;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collider2D = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        // Save the current y position
        currY = transform.position.y;
        lastPosition = transform.position;
        expectedY = transform.position.y;
        
        // Record the stalfos start position
        startPosition = transform.position;
        
        // Initialize trigger information
        foreach (RoomTransitionTrigger gate in gates)
        {
            InitializeGate(gate);
        }
        
               
        // Initialize the valid tiles
        validTiles = new bool[walkableRegion.x * walkableRegion.y];
        for (int y = 0; y < walkableRegion.y; y++)
        {
            for (int x = 0; x < walkableRegion.x; x++)
            {
                Vector3 testCell = new Vector3(x * 2 + walkableRegionOffset.x, y * 2 + walkableRegionOffset.y, 0f);
                foreach (Tilemap t in wallTilemaps)
                {
                    if (t.HasTile(t.WorldToCell(testCell)))
                    {
                        validTiles[y * walkableRegion.x + x] = false;
                        goto LOOPEND;
                    }
                }

                validTiles[y * walkableRegion.x + x] = true;
                LOOPEND: ;
            }
        }
    }


    private float moveAmount = 0f;
    private float walkTimeLeft = 0f;
    private Vector2Int moveDirection = Vector2Int.zero;

    private void PickDirection()
    {
        Vector2Int currentTilePosition = new Vector2Int(Mathf.RoundToInt(transform.position.x - walkableRegionOffset.x),
            Mathf.RoundToInt(transform.position.y - walkableRegionOffset.y)) / 2;
        List<PlayerDirection> validDirections = new List<PlayerDirection>() {PlayerDirection.LEFT, PlayerDirection.DOWN, PlayerDirection.RIGHT, PlayerDirection.UP};
        
        // Check alignment to restrict movement
        bool xAligned = Mathf.RoundToInt((transform.position.x - walkableRegionOffset.x) / 2f) * 2
                        == Mathf.RoundToInt(transform.position.x - walkableRegionOffset.x);

        bool yAligned = Mathf.RoundToInt((transform.position.y - walkableRegionOffset.y) / 2f) * 2
                        == Mathf.RoundToInt(transform.position.y - walkableRegionOffset.y);
        if (!xAligned)
        {
            validDirections.Remove(PlayerDirection.UP);
            validDirections.Remove(PlayerDirection.DOWN);
        }
        else if (!yAligned)
        {
            validDirections.Remove(PlayerDirection.LEFT);
            validDirections.Remove(PlayerDirection.RIGHT);
        }
        
        Vector2Int chosenDirection = Vector2Int.zero;
        while (validDirections.Count > 0)
        {
            PlayerDirection direction = validDirections[Random.Range(0, validDirections.Count)];
            Vector2Int directionTest = Vector2Int.zero;
            switch (direction)
            {
                case PlayerDirection.LEFT: directionTest = Vector2Int.left; break;
                case PlayerDirection.RIGHT: directionTest = Vector2Int.right; break;
                case PlayerDirection.DOWN: directionTest = Vector2Int.down; break;
                case PlayerDirection.UP: directionTest = Vector2Int.up; break;
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


    // Update is called once per frame
    void FixedUpdate()
    {
        if (isActive)
        {
            if (knockedBack)
            {
                if (knockbackTimeLeft <= 0f)
                {
                    transform.position = new Vector3(Mathf.Round(transform.position.x),
                        Mathf.Round(transform.position.y),
                        transform.position.z); // Grid snap
                    PickDirection(); // Select new direction
                    knockedBack = false;
                }
                MoveEntity(knockbackSpeed, moveDirection.x, moveDirection.y);
                knockbackTimeLeft -= Time.fixedDeltaTime;
            }
            else
            {
                if (walkTimeLeft <= 0f)
                {
                    transform.position = new Vector3(Mathf.Round(transform.position.x),
                        Mathf.Round(transform.position.y),
                        transform.position.z); // Grid snap
                    PickDirection(); // Select new direction
                }

                MoveEntity(moveSpeed, moveDirection.x, moveDirection.y);
                walkTimeLeft -= Time.fixedDeltaTime;
            }
        }
    }

    private float knockbackTimeLeft = 0f;
    private bool knockedBack = false;
    public void OnHit(Vector2Int direction, int damage)
    {
        if (knockedBack) return; // Still being knocked back so we still have iframes essentially
        hp -= damage;
        if (hp <= 0)
        {
            foreach(var gate in gates) DisconnectGate(gate);
            Destroy(gameObject);
        }
        else
        {
            knockedBack = true;
            moveDirection = direction;
            knockbackTimeLeft = (float)knockbackAmount / knockbackSpeed;
        }
    }


    private void InitializeGate(RoomTransitionTrigger trigger)
    {
        //trigger.OnTransitionBegin += OnGateTransitionBegin;
        //trigger.OnTransitionEnd += OnGateTransitionEnd;
    }

    private void DisconnectGate(RoomTransitionTrigger trigger)
    {
        //trigger.OnTransitionBegin -= OnGateTransitionBegin;
        //trigger.OnTransitionEnd -= OnGateTransitionEnd;
    }

    private bool isActive = false;
    
    private void OnGateTransitionBegin(RoomTransitionTrigger performer, bool vertical, bool positionFlag)
    {
        if (isActive)
        {
            isActive = false;
            sprite.enabled = false;
        }
    }

    private void OnGateTransitionEnd(RoomTransitionTrigger performer, bool vertical, bool positionFlag)
    {
        if (!isActive)
        {
            isActive = true;
            transform.position = startPosition;
            StartCoroutine(SpawnStalfos());
        }
    }

    private IEnumerator SpawnStalfos()
    {
        sprite.enabled = true;
        PickDirection();
        yield break;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("Player"))
        {
            //OnHit(Vector2Int.left, 1);
        }   
    }

private void MoveEntity(float moveSpeed, int xIn, int yIn)
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
}
