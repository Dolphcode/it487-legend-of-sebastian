using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using SQZL.Entity.Enemy;

namespace SQZL.Entity.Editor
{
    [CustomEditor(typeof(BaseEnemyController), true)]
    public class BaseEnemyControllerEditor : UnityEditor.Editor
    {
        public VisualTreeAsset inspectorUXML;

        private VisualElement drawArea;
        
        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();
            
            //VisualElement myInspector = new();
            //myInspector.Add(new Label("FUCK"));
            VisualElement inspector = new();
            if (!(inspectorUXML is null))
            {
                VisualElement uxmlContent = inspectorUXML.CloneTree();
                inspector.Add(uxmlContent);
            }

            VisualElement defaultArea = inspector.Q<VisualElement>("default_inspector");
            InspectorElement.FillDefaultInspector(defaultArea, serializedObject, this);
            
            drawArea = inspector.Q<VisualElement>("grid_draw_area");
            drawArea.generateVisualContent += DrawCachedGrid;
            
            Button cacheButton = inspector.Q<Button>("cache_button");
            cacheButton.clicked += CacheGrid;

            Label warnText = inspector.Q<Label>("warn");
            warnText.style.display = (serializedObject.FindProperty("preCached").boolValue) ?  DisplayStyle.None : DisplayStyle.Flex;
            
            return inspector;
        }

        void DrawCachedGrid(MeshGenerationContext ctx)
        {
            // Check if we have even cached a grid
            bool cached = serializedObject.FindProperty("preCached").boolValue;
            if (!cached) return;
            
            // Get the painter
            Painter2D painter = ctx.painter2D;
            
            VisualElement v = ctx.visualElement;
            Rect bounds = v.contentRect;

            Vector2Int regionSize = serializedObject.FindProperty("walkableRegion").vector2IntValue;
            SerializedProperty arr = serializedObject.FindProperty("validTiles");
            int x, y;
            float gridX, gridY, gridTileSize = Mathf.Min(bounds.width / (float)regionSize.x, bounds.height / (float)regionSize.y);
            float gridCenterXOffset = (bounds.width - (gridTileSize * regionSize.x)) / 2f;
            float gridCenterYOffset = (bounds.height - (gridTileSize * regionSize.y)) / 2f;
            
            for (int i = 0; i < arr.arraySize; i++)
            {
                // Get the grid coordinates
                y = i / regionSize.x;
                x = i % regionSize.x;
                gridX = (float)x * gridTileSize + gridCenterXOffset;
                gridY = (float)y * gridTileSize + gridCenterYOffset;
                
                painter.BeginPath();
                painter.MoveTo(new Vector2(gridX, gridY));
                painter.LineTo(new Vector2(gridX + gridTileSize, gridY));
                painter.LineTo(new Vector2(gridX + gridTileSize, gridY + gridTileSize));
                painter.LineTo(new Vector2(gridX, gridY + gridTileSize));
                painter.ClosePath();
                if (arr.GetArrayElementAtIndex(i).boolValue)
                    painter.fillColor = (Color.blue);
                else
                    painter.fillColor = (Color.red);
                painter.Fill();
            }
        }

        void CacheGrid()
        {
            Debug.Log("Caching the grid!");
            BaseEnemyController e = serializedObject.targetObject as BaseEnemyController;
            if (e is null)
            {
                Debug.LogError("Caching grid on null entity");
                return;
            }
            
            bool[] arr = e.GetValidTiles();
            SerializedProperty cachedGrid = serializedObject.FindProperty("validTiles");
            if (!(cachedGrid is null) && cachedGrid.isArray)
            {
                cachedGrid.arraySize = arr.Length;
                SerializedProperty nextElem;
                for (int i = 0; i < arr.Length; i++)
                {
                    nextElem = cachedGrid.GetArrayElementAtIndex(i);
                    nextElem.boolValue = arr[i];
                }
            }
            
            serializedObject.FindProperty("preCached").boolValue = true;
            serializedObject.ApplyModifiedProperties();
            drawArea.MarkDirtyRepaint();
        }
    }
}