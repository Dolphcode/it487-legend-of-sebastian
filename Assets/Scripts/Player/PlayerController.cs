using System;
using Math = System.Math;
using MidpointRounding = System.MidpointRounding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {

        [Header("Movement Config")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] [Range(0f, 1.0e-4f)] private float blockTestThreshold = 1.0e-5f;
        [SerializeField] private float positionSnapThreshold = 1f/32f;
        [SerializeField] private float motionBias = 0.1f;
        [SerializeField] private float overlapOvershootMax = 0.2f;

        [Header("Collision Config")] [SerializeField]
        private ContactFilter2D contactFilter;
        
        // On Start actions
        private InputAction moveAction;  
        
        // On Start components
        private BoxCollider2D collider2D;
        
        // State variables
        private float currY = 0f, expectedY = 0f;
        private int prevYIn = 0;
        private bool snappedToGridFlag = false;
        private Vector3 lastPosition, deltaPosition;
        private bool vBlocked = false;

        // 0 = down, 1 = right, 2 = up, 3 = left
        private int facing = 0;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Get all components
            collider2D = GetComponent<BoxCollider2D>();
            
            // Actions
            moveAction = InputSystem.actions.FindAction("Move");
            
            // Save the current y position
            currY = transform.position.y;
            lastPosition = transform.position;
            expectedY = transform.position.y;
        }

        // Fixed Update is called once per physics frame
        void FixedUpdate()
        {
            /*
             * 1. Vertical movement evaluates before horizontal. If holding UP/LEFT you will go all the way up first and then when you collide with the wall you will start going left.
             * 2. 
             */
            
            // Get the movement input value and round it to integral values
            Vector2 moveInputValue = moveAction.ReadValue<Vector2>();
            int xIn = (int)Math.Round(moveInputValue.x, MidpointRounding.AwayFromZero);
            int yIn = (int)Math.Round(moveInputValue.y, MidpointRounding.AwayFromZero);
            
            // Save the current y position and shift the y position from fixed update at the previous frame
            // The purpose of this is to take the change in y from the previous frame to the current after
            // collision is resolved (which presumably occurs after FixedUpdate? may need to verify this
            // We also compute the delta position between frames to apply a motion bias and save the last position at this frame
            deltaPosition = transform.position - lastPosition;
            Debug.Log(deltaPosition);
            lastPosition = transform.position;
            currY = transform.position.y;
            Debug.Log($"Position at start: {transform.position}");

            // Evaluate vertical movement first
            // Skip to horizontal if we are being blocked vertically by a wall essentially and both a vertical and horizontal input are being applied
            // We only perform the block test if we were pressing a y input in the previous frame of course
            // I realized I needed to distinguish between being blocked up or down
            Vector2 newPosition = transform.position;
            float delta = moveSpeed * Time.fixedDeltaTime;
            Debug.Log($"vblocked is {vBlocked} because we expect {expectedY}, got {currY} given input {prevYIn}");
            if (yIn != 0)
            {
                // Check if we are blocked in the direction we are trying to go in?
                Collider2D[] vblockCheckArray = new Collider2D[1];
                vBlocked = Physics2D.OverlapBox(new Vector2(transform.position.x, transform.position.y + delta * yIn) + collider2D.offset * transform.localScale,
                    (collider2D.size - new Vector2(0.05f, 0.05f)) * transform.localScale,
                    0f,
                    contactFilter,
                    vblockCheckArray) > 0;

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
                
                /*
                if (vBlocked)
                {
                    Vector2 overshotFrom = new Vector2(Mathf.Round(newPosition.x + 0.5f * -xIn), Mathf.Round(newPosition.y));
                    Vector2 boxCenter = overshotFrom + Vector2.down * 0.5f + Vector2.up * yIn;
                    Vector2 boxSize = new Vector2(2f - overlapOvershootMax, 0.95f);
                    
                    
                    Vector2 boxCornerTL = boxCenter - boxSize * 0.5f;
                    Vector2 boxCornerBR = boxCenter + boxSize * 0.5f;
                    Vector2 boxCornerTR = new Vector2(boxCornerTL.x + boxSize.x, boxCornerTL.y);
                    Vector2 boxCornerBL = new Vector2(boxCornerBR.x - boxSize.x, boxCornerBR.y);
                    Debug.DrawLine(boxCornerTL, boxCornerTR, Color.red, 0.5f, false);
                    Debug.DrawLine(boxCornerBL, boxCornerBR, Color.red, 0.5f, false);
                    Debug.DrawLine(boxCornerBL, boxCornerTL, Color.red, 0.5f, false);
                    Debug.DrawLine(boxCornerBR, boxCornerTR, Color.red, 0.5f, false);
                    
                    
                    //bool tileClear = Physics2D.OverlapBox(boxCenter, boxSize, 0f, ~LayerMask.GetMask("Player")) is null; 
                    //if (tileClear)
                    //    newPosition.x = Mathf.Round(newPosition.x);
                }*/
                
                transform.position = newPosition;
                
                // NOTE: Set facing (0 is down, 1 is right, 2 is up, 3 is left)
                if (Mathf.Abs(b) > 0f) facing = 1 + 2 * ((b > 0f) ? 0 : 1);
                else facing = 2 * ((remainderMove < 0f) ? 0 : 1);
            }
            Debug.Log($"0 down, 1 right, 2 up, 3 left, what are we facing? {facing}");
            
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
                Debug.Log($"We are colliding with {other.gameObject.name}");

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
            Debug.DrawLine(
                new Vector2(ours.min.x, ours.min.y),
                new Vector2(ours.max.x, ours.min.y),
                Color.green,
                1f
            ); 
            // Perform resolution for edge based on distance
            int pointA = facing, pointB = (facing + 1) % 4; // 0 -> 0,1, 1 -> 1,2, 2 -> 2,3, 3 -> 3,0
            Debug.Log($"Resolving tile collision? {boundCorners[pointA]}, {boundCorners[pointB]} given that my bounds are {ours}"); 
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

            float epsilon = 1e-1f;

            Debug.Log(
                $"Resolving tile collision, checking at {boundCorners[pointA] + direction * epsilon} and at {boundCorners[pointB] + direction * epsilon}");
            Vector3Int tileA = other.WorldToCell(boundCorners[pointA] + direction * epsilon + Vector2.one), tileB = other.WorldToCell(boundCorners[pointB] + direction * epsilon + Vector2.one);
            Debug.Log($"Resolving tile collision? checking tiles tile a {tileA}, and tileB {tileB}");
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
            Debug.Log($"Checking location {checkLoc}, {tmap.HasTile(checkLoc)}");
            if (tmap.HasTile(checkLoc))
            {
                Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + collider2D.offset * transform.localScale,
                    collider2D.size * transform.localScale);
                Bounds others = new Bounds(tmap.GetCellCenterWorld(checkLoc), Vector3.Scale(tmap.cellSize, tmap.transform.localScale));
                Debug.Log($"Resolving tile collision? The tile at {checkLoc} maps to {others}");
                ResolveBoundsCollision(others, ours);
            }
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
}
