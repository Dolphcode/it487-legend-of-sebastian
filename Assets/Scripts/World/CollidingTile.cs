using UnityEngine;
using UnityEngine.Tilemaps;

namespace SQZL.World
{
    [CreateAssetMenu(fileName = "New Colliding Tile", menuName = "Tiles/Colliding Tile")]
    public class CollidingTile : Tile
    {
        [SerializeField] private Bounds bounds;

        public Bounds GetBounds()
        {
            return bounds;
        }
    }
}
