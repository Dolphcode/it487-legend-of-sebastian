using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Colliding Tile", menuName = "Tiles/Colliding Tile")]
public class CollidingTile : Tile
{
    [SerializeField] private Bounds bounds;

    public Bounds GetBounds()
    {
        return bounds;
    }
}
