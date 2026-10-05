using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using SQZL.World;

namespace SQZL.Entity {
    
    /// <summary>
    /// A simple class similar to Rigidbody2D representing tile based bodies that interact with tilemap colliders and box colliders. This class provides a suite
    /// of functions for detecting and resolving on the tilemap in a way that is fixed to the grid.
    /// Some limitations of this are that Tilebody2D only interacts with tilemap and box colliders. This is working under the assumption that in a grid based system
    /// we are only working with tilemaps.
    /// </summary>
    public class Tilebody2D : MonoBehaviour
    {
        #region CONSTS
        /// <summary>
        /// Dictates the maximum number of tile collisions. We can pretty much assume this is constant between every TileBody2D
        /// and adjust it as we need for the whole codebase
        /// </summary>
        protected const int MAX_TILE_COLLISIONS = 8;

        /// <summary>
        /// This constant represents the maaximum bounds we can collect in order to resolve. Tweak this as needed for collision
        /// detection, resolution, and callbacks to resolve as necessary.
        /// </summary>
        protected const int MAX_BOUNDS_TO_RESOLVE = 16;

        /// <summary>
        /// Dictates the threshold of overlap allowed before collision is resolved
        /// </summary>
        protected const float OVERLAP_THRESHOLD = 0.0625f;
        
        /// <summary>
        /// When checking for tile overlaps/collision, we add this buffer to the computation to ensure we are not making checks along the edges of
        /// tiles
        /// </summary>
        protected const float TILE_COLLISION_EPSILON = 1e-1f;

        /// <summary>
        /// The minimum threshold of movement for us to determine if we moved cardinally or diagonally
        /// </summary>
        protected const float DIRECTION_DETECTION_EPSILON = 1e-1f;
        
        /// <summary>
        /// In case the tag for world objects that handle tilebody collisions changes.
        /// </summary>
        protected const string COLLISION_HANDLER_TAG = "HandlesTilebodyCollisions";
        #endregion
        #region FIELDS
        [Header("Motion Config")] [SerializeField] private float blockTestThreshold = 1.0e-5f;
        [SerializeField] private float positionSnapThreshold = 0.03125f; // 1/32 in float form
        [SerializeField] private float overlapOvershootMax = 0.2f;
        
        // References
        private BoxCollider2D _boxCollider2D;
        
        // Buffers
        private Collider2D[] _colliderBuffer = new Collider2D[MAX_TILE_COLLISIONS];
        private Bounds[] _boundsBuffer = new Bounds[MAX_BOUNDS_TO_RESOLVE];
        private int _boundsProcessIndex = 0; // This tracks how many bounds we have marked as processing, used to queue more bounds into the buffer
        
        #endregion
        #region EVENTS
        /// <summary>
        /// Invoked when this Tilebody OVERLAPS with the tilemap. Invoked each time we overlap for each tile cell we overlap
        /// with on the tilemap. Recommended to bind only in components on the same game object as this Tilebody for neatness/
        /// organization.
        /// </summary>
        public event Action<Tilemap, Vector3Int> OnTilebodyTilemapCollide;
        
        /// <summary>
        /// Invoked when this Tilebody OVERLAPS with a box collider. Recommended to bind only in components on the same game
        /// object as this Tilebody for neatness/organization.
        /// </summary>
        public event Action<Collider2D> OnTilebodyColliderCollide;
        #endregion
        
        // Properties
        public TilebodyDirection LastMovedDirection { protected set; get; } = TilebodyDirection.Down;

        /// <summary>
        /// This function must be overridden if TileBody2D is being extended and components need to be pulled 
        /// </summary>
        private void Awake()
        {
            // Get components needed by the TileBody2D in Awake
            _boxCollider2D = GetComponent<BoxCollider2D>(); // This code is optimized somewhat by the assumption that every entity will have a BOX collider
            
            #if DEBUG
            if (_boxCollider2D is null) Debug.LogError($"Could not find Collider2D component on {gameObject.name}");
            #endif
        }

        #region COLLISION

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="filter"></param>
        public void MoveAndCollide(Vector2 newPosition, ContactFilter2D filter)
        {
            // Attempt movement and compute facing direction
            // NOTE THIS CURRENTLY ONLY WORKS UNDER THE ASSUMPTION WE CAN ONLY MOVE IN ONE DIRECTION
            //Debug.Log($"pre collision {newPosition}");
            Vector2 directionMoved =
                new Vector2(newPosition.x - transform.position.x, newPosition.y - transform.position.y);
            Vector2 absDirectionMoved = new Vector2(Mathf.Abs(directionMoved.x), Mathf.Abs(directionMoved.y));
            if (absDirectionMoved.y < DIRECTION_DETECTION_EPSILON)
            {
                if (directionMoved.x > DIRECTION_DETECTION_EPSILON) LastMovedDirection = TilebodyDirection.Right;
                else if (directionMoved.x < -DIRECTION_DETECTION_EPSILON) LastMovedDirection = TilebodyDirection.Left;
            } else if (absDirectionMoved.x < DIRECTION_DETECTION_EPSILON)
            {
                if (directionMoved.y > DIRECTION_DETECTION_EPSILON) LastMovedDirection = TilebodyDirection.Up;
                else if (directionMoved.y < -DIRECTION_DETECTION_EPSILON) LastMovedDirection = TilebodyDirection.Down;
            }
            else LastMovedDirection = TilebodyDirection.Diagonal;
            transform.position = newPosition;
            
            // Define our bounds
            Bounds ours = new Bounds(new Vector2(transform.position.x, transform.position.y) + _boxCollider2D.offset * transform.localScale,
                (_boxCollider2D.size) * transform.localScale);
            //Debug.Log($"bounds of ours being used in the check are {ours} derived from {transform.position}");
            
            // Overlap our box with any other box based on the filter and collider buffer size
            int overlapCount = Physics2D.OverlapBox(ours.center, 
                ours.size,
                0f,
                filter,
                _colliderBuffer);
            //Debug.Log($"Overlapping with {overlapCount} things");

            // Iterate based on # of collisions
            Collider2D other;
            int i;
            for (other = _colliderBuffer[i = 0]; i < overlapCount; i++)
            {
                other = _colliderBuffer[i];
                //Debug.Log($"{other} and i is {i} and overlap count is {overlapCount}");
                if (other is TilemapCollider2D)
                    CollectTileCollision(ours,
                        other.gameObject.GetComponent<Tilemap>()); // If tilemap, get the tilemap and perform resolution
                else CollectBasicCollision(ours, other); // Otherwise resolve as basic collider
            }

            // After collecting, resolve
            // Resolve the smaller overlap
            // But prioritize horizontal over vertical
            float correctionDir, overlapX, overlapY;
            Vector3 correctionVector;
            Bounds others;
            for (i = 0; i < _boundsProcessIndex; i++)
            {
                others = _boundsBuffer[i];
                overlapX = Mathf.Min(ours.max.x, others.max.x) - Mathf.Max(ours.min.x, others.min.x);
                overlapY = Mathf.Min(ours.max.y, others.max.y) - Mathf.Max(ours.min.y, others.min.y);
                
                if (overlapX <= OVERLAP_THRESHOLD || overlapY <= OVERLAP_THRESHOLD) continue;
                
                if (overlapX <= overlapY && overlapX > OVERLAP_THRESHOLD)
                {
                    correctionDir = -Mathf.Sign(others.center.x - ours.center.x);
                    correctionVector = Vector3.right * correctionDir * overlapX;
                }
                else
                {
                    correctionDir = -Mathf.Sign(others.center.y - ours.center.y);
                    correctionVector = Vector3.up * correctionDir * overlapY;
                }

                transform.position += correctionVector;
                ours.center += correctionVector;
            }

            //Debug.Log($"post collision {transform.position}");
            // Reset process index
            _boundsProcessIndex = 0;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ours"></param>
        /// <param name="dir"></param>
        /// <param name="tmap"></param>
        /// <returns></returns>
        public bool DetectTileOverlap(Bounds ours, TilebodyDirection dir, Tilemap tmap)
        {
            // Get grid tiles
            Vector2 directionVector = Vector2.zero, pointA, pointB;
            switch (dir)
            {
                case TilebodyDirection.Down:
                default:
                    directionVector = Vector2.down;
                    pointA = ours.min;
                    pointB = new Vector2(ours.max.x, ours.min.y);
                    break;
                case TilebodyDirection.Right:
                    directionVector = Vector2.right;
                    pointA = new Vector2(ours.max.x, ours.min.y);
                    pointB = ours.max;
                    break;
                case TilebodyDirection.Up:
                    directionVector = Vector2.up;
                    pointA = ours.max;
                    pointB = new Vector2(ours.min.x, ours.max.y);
                    break;
                case TilebodyDirection.Left:
                    directionVector = Vector2.left;
                    pointA = new Vector2(ours.min.x, ours.max.y);
                    pointB = new Vector2(ours.max.x, ours.min.y);
                    break;
            }
            
            // So to compute world to cell, all Unity does is a simple floor operation
            // So presumably, the way this works is because everything is scaled up 2x, we take the bound corners we're trying to convert, divide by 2
            // then floor. So theoretically, adding 1 is equivalent to dividing by 2, adding 0.5, and then computing the floor. Note that
            // floor(x + 0.5) is functionally equivalent to round(x). So floor(0.5(2x + 1)) -> round(x) which is probably what we want for
            // collisions to function correctly. Otherwise we got some weeeeeird jank : (
            Vector3Int tileA = tmap.WorldToCell(pointA + directionVector * TILE_COLLISION_EPSILON + Vector2.one), 
                tileB = tmap.WorldToCell(pointB + directionVector * TILE_COLLISION_EPSILON + Vector2.one);
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
                    if (overlapX <= OVERLAP_THRESHOLD || overlapY <= OVERLAP_THRESHOLD) continue;
                    else return true;
                }
            }

            // No tile collision detected
            return false;
        }
        
        protected void CollectBasicCollision(Bounds ours, Collider2D other)
        {
            // Grab bounds and determine signed overlap vector
            Bounds others = other.bounds;
            //Debug.Log($"Colliding with a basic object called {other.gameObject.name} with bounds {other.bounds} while ours are {ours}");
            if (CollectBoundsCollision(others, ours) && other.gameObject.CompareTag(COLLISION_HANDLER_TAG))
            {
                // TODO: Adapt this to work if multiple components implement ITilebodyCollisionHandler
                ITilebodyCollisionHandler handler;
                if (other.gameObject.TryGetComponent<ITilebodyCollisionHandler>(out handler))
                {
                    handler.OnCollideWithTilebody(this);
                    OnTilebodyColliderCollide?.Invoke(other);
                }
            } 
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        protected void CollectTileCollision(Bounds ours, Tilemap other)
        {
            // Get grid tiles
            Vector2 direction = Vector2.zero, pointA, pointB;
            switch (LastMovedDirection)
            {
                case TilebodyDirection.Down:
                    direction = Vector2.down;
                    pointA = ours.min;
                    pointB = new Vector2(ours.max.x, ours.min.y);
                    break;
                case TilebodyDirection.Right:
                    direction = Vector2.right;
                    pointA = new Vector2(ours.max.x, ours.min.y);
                    pointB = ours.max;
                    break;
                case TilebodyDirection.Up:
                    direction = Vector2.up;
                    pointA = ours.max;
                    pointB = new Vector2(ours.min.x, ours.max.y);
                    break;
                case TilebodyDirection.Left:
                    direction = Vector2.left;
                    pointA = new Vector2(ours.min.x, ours.max.y);
                    pointB = new Vector2(ours.max.x, ours.min.y);
                    break;
                default:
                    direction = Vector2.zero;
                    pointA = ours.min;
                    pointB = ours.max;
                    break;
            }
            
            // So to compute world to cell, all Unity does is a simple floor operation
            // So presumably, the way this works is because everything is scaled up 2x, we take the bound corners we're trying to convert, divide by 2
            // then floor. So theoretically, adding 1 is equivalent to dividing by 2, adding 0.5, and then computing the floor. Note that
            // floor(x + 0.5) is functionally equivalent to round(x). So floor(0.5(2x + 1)) -> round(x) which is probably what we want for
            // collisions to function correctly. Otherwise we got some weeeeeird jank : (
            Vector3Int tileA = other.WorldToCell(pointA + direction * TILE_COLLISION_EPSILON + Vector2.one), 
                tileB = other.WorldToCell(pointB + direction * TILE_COLLISION_EPSILON + Vector2.one);
            if (tileA.x == tileB.x && LastMovedDirection != TilebodyDirection.Diagonal) // Vertical
            {
                for (int i = Mathf.Min(tileA.y, tileB.y); i <= Mathf.Max(tileA.y, tileB.y); i++)
                {
                    Vector3Int checkLoc = new Vector3Int(tileA.x, i, 0);
                    CollectTileCollisionAtLocation(ours, checkLoc, other);
                }
            }
            else if (tileA.y == tileB.y && LastMovedDirection != TilebodyDirection.Diagonal) // Assume horizontal
            {
                for (int i = Mathf.Min(tileA.x, tileB.x); i <= Mathf.Max(tileA.x, tileB.x); i++)
                {
                    Vector3Int checkLoc = new Vector3Int(i, tileA.y, 0);
                    CollectTileCollisionAtLocation(ours, checkLoc, other);
                }
            }
            else
            {
                for (int i = Mathf.Min(tileA.x, tileB.x); i <= Mathf.Max(tileA.x, tileB.x); i++)
                for (int j = Mathf.Min(tileA.y, tileB.y); j <= Mathf.Max(tileA.y, tileB.y); j++)
                {
                    Vector3Int checkLoc = new Vector3Int(i, j, 0);
                    CollectTileCollisionAtLocation(ours, checkLoc, other);
                }
            }

        }
        
        /// <summary>
        /// Check 
        /// </summary>
        /// <param name="checkLoc"></param>
        /// <param name="tmap"></param>
        private void CollectTileCollisionAtLocation(Bounds ours, Vector3Int checkLoc, Tilemap tmap)
        {
            if (tmap.HasTile(checkLoc))
            {
                Bounds others;
                
                // Check if tile has special collision data
                // TODO: Maybe just use box colliders in place of special tile collision data, there's really only one case that utilizes this and this only works if the special collider is smaller than the box
                Tile t = tmap.GetTile<Tile>(checkLoc);
                if (t is CollidingTile)
                {
                    CollidingTile c = t as CollidingTile;
                    Bounds collisionExtents = c.GetBounds();
                    collisionExtents.center = Vector3.Scale(collisionExtents.center, tmap.transform.localScale);
                    collisionExtents.extents = Vector3.Scale(collisionExtents.extents, tmap.transform.localScale);
                    others = new Bounds(tmap.GetCellCenterWorld(checkLoc) + Vector3.Scale(tmap.cellSize, collisionExtents.center), Vector3.Scale(tmap.cellSize, collisionExtents.size));
                } else others = new Bounds(tmap.GetCellCenterWorld(checkLoc), Vector3.Scale(tmap.cellSize, tmap.transform.localScale));
                
                // Resolve the collision given the bounds to resolve with
                if (CollectBoundsCollision(others, ours)) OnTilebodyTilemapCollide?.Invoke(tmap, checkLoc);
            }
        }
        
        /// <summary>
        /// Collects collision given our bounds and the decided bounds of another collider. This will be resolved
        /// in another function. Returns true/false if a callback should be issued based on the type of collision.
        /// Handled in the function that generates the bounds. Will return false if we run out of space in the bounds
        /// processing buffer.
        /// </summary>
        /// <param name="others"></param>
        /// <param name="ours"></param>
        private bool CollectBoundsCollision(Bounds others, Bounds ours)
        {
            // Compute overlaps
            float overlapX = Mathf.Min(ours.max.x, others.max.x) - Mathf.Max(ours.min.x, others.min.x);
            float overlapY = Mathf.Min(ours.max.y, others.max.y) - Mathf.Max(ours.min.y, others.min.y);
            
            // Cut short if neither is overlapping
            //Debug.Log($"{overlapX}, {overlapY}, {OVERLAP_THRESHOLD}, just verifying something");
            if (overlapX <= OVERLAP_THRESHOLD || overlapY <= OVERLAP_THRESHOLD) return false;
            
            // Collect to the buffer
            if (_boundsProcessIndex < MAX_BOUNDS_TO_RESOLVE)
            {

                _boundsBuffer[_boundsProcessIndex++] = others;
                return true;
            }
#if DEBUG
            else
            {
                Debug.LogWarning(
                    $"WARNING: Object {gameObject.name} with Tilebody2D has run out of space this frame in the bounds buffer");
            }
#endif
            
            // If we fill the buffer return false
            return false;
        }
        #endregion
    }
    
    /// <summary>
    ///  An enumerator used to represent the direction of the tile body.
    ///  Diagonal is a special case used for certain enemy types. This is predominantly used
    ///  to optimize tile collision cehcks for entities that only move in the main cardinal directions.
    /// </summary>
    public enum TilebodyDirection
    {
        Down, Right, Up, Left, Diagonal
    }
}