using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

namespace SQZL.Tool
{
    public class TileCoordVisualizer : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemapToVisualize;
        [SerializeField] private GameObject visual;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            StartCoroutine(LoadVisualization());
        }


        private IEnumerator LoadVisualization()
        {
            Vector3Int checkPos = new Vector3Int(0, 0, 0);
            for (int y = tilemapToVisualize.cellBounds.min.y; y < tilemapToVisualize.cellBounds.size.y; y++)
            for (int x = tilemapToVisualize.cellBounds.min.x; x < tilemapToVisualize.cellBounds.size.x; x++)
            {
                checkPos.x = x;
                checkPos.y = y;
                if (tilemapToVisualize.HasTile(checkPos))
                {
                    AsyncInstantiateOperation op = InstantiateAsync(visual, transform);
                    yield return op;
                    GameObject created = op.Result[0] as GameObject;
                    TextMeshProUGUI text = created.GetComponent<TextMeshProUGUI>();
                    text.text = $"({x},{y})";
                    created.transform.position = tilemapToVisualize.GetCellCenterWorld(checkPos);
                } 
            }
        }
       
    }
}
