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

                    newPosition = transform.position + new Vector3(remainderMove, b, 0f);
                    transform.position = newPosition;
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
            boundCorners[0] = ours.min;
            boundCorners[1] = ours.max;
            boundCorners[2] = new Vector2(ours.min.x, ours.max.y);
            boundCorners[3] = new Vector2(ours.max.x, ours.min.y);
            
            // Perform resolution for each corner
            foreach (Vector2 point in boundCorners)
            {
                // Check what tiles this point is overlapping
                Collider2D[] collidersOnCorner = new Collider2D[4];
                int colliderCount;
                if ((colliderCount = Physics2D.OverlapPoint(point, contactFilter, collidersOnCorner)) == 0) continue;
                
                // 
                fo
                
            }
        }

        private void ResolveBasicCollision(Collider2D other)
        {
            // Grab bounds and determine signed overlap vector
            Bounds others = other.bounds;
            Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + collider2D.offset * transform.localScale,
                collider2D.size * transform.localScale);
            
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
