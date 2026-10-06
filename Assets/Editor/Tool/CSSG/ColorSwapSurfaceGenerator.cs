using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

public class ColorSwapSurfaceGenerator : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private Image spriteDisplay;
    private uint[] colorHashTableIndex;
    private Label binsLabel;
    
    [MenuItem("Window/SQZL/Color Swap Surface Generator")]
    public static void ShowExample()
    {
        ColorSwapSurfaceGenerator wnd = GetWindow<ColorSwapSurfaceGenerator>();
        wnd.titleContent = new GUIContent("Color Swap Surface Generator");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.CloneTree();
        root.Add(labelFromUXML);
        
        #region INIT_LEFT_PANE
        // Create the sprites
        var allObjectGUIDs = AssetDatabase.FindAssetGUIDs("a:assets t:Sprite");
        var allObjects = new List<Sprite>();
        ListView listView = root.Q<ListView>("left-pane");
        foreach (var guid in allObjectGUIDs)
        {
            allObjects.Add(
                AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid))
                );
        }

        listView.makeItem = GenerateSpriteEntry;
        listView.bindItem = (item, index) =>
        {
            Image icon = item.Q<Image>("list-entry-icon");
            Label name = item.Q<Label>("list-entry-name");
            icon.image = allObjects[index].texture;
            name.text = allObjects[index].name;
        };
        listView.itemsSource = allObjects;

        // Assign the sprite for sprite selection changing
        spriteDisplay = rootVisualElement.Q<Image>("sprite-preview");
        binsLabel = rootVisualElement.Q<Label>("bin-count-label");
        
        listView.selectionChanged += OnSpriteSelectionChanged;
        #endregion

        #region BUTTON_BINDING

        Button generateButton = rootVisualElement.Q<Button>("generate");
        generateButton.clicked += OnGenerateButtonPressed;

        #endregion
    }

    private void OnGenerateButtonPressed()
    {
        if (colorHashTableIndex is null || colorHashTableIndex.Length == 0)
        {
            return;
        }

        string outPath = EditorUtility.SaveFilePanel("Choose a location", "Assets", "gen_tex", "png");
        Texture2D output = new(1, colorHashTableIndex.Length, TextureFormat.ARGB32, false);
        Color32[] texPixels = new Color32[colorHashTableIndex.Length];
        for (int i = 0; i < colorHashTableIndex.Length; i++)
        {
            uint color = colorHashTableIndex[i];
            uint r = 0xFF & (color >> 16);
            uint g = 0xFF & (color >> 8);
            uint b = 0xFF & color;
            Color32 color32 = new Color32((byte)r, (byte)g, (byte)b, 255);
            
            texPixels[i] = color32;
        }

        Array.Reverse(texPixels);
        output.SetPixels32(texPixels);
        output.Apply();
        byte[] pngBin = output.EncodeToPNG();
        File.WriteAllBytes(outPath, pngBin);
    }

    private void OnSpriteSelectionChanged(IEnumerable<object> selectedItems)
    {
        var enumerator = selectedItems.GetEnumerator();
        if (enumerator.MoveNext())
        {
            Sprite selectedSprite = enumerator.Current as Sprite;
            if (!(selectedSprite is null))
            {
                spriteDisplay.image = selectedSprite.texture;
                colorHashTableIndex = GenerateUniqueBins(selectedSprite);
                binsLabel.text = $"Min Bins: {colorHashTableIndex.Length}";
            } 
        }
    }
    
    private VisualElement GenerateSpriteEntry()
    {
        VisualElement entry = new();
        entry.style.flexDirection = FlexDirection.Row;
        entry.style.flexGrow = 1;
        
        Image icon = new();
        icon.name = "list-entry-icon";
        StyleLength maxWidthLength = new StyleLength();
        maxWidthLength.value = Length.Percent(20f);
        icon.style.maxWidth = maxWidthLength;

        Label name = new();
        name.name = "list-entry-name";
        
        entry.Add(icon);
        entry.Add(name);
        return entry;
    }

    private uint[] GenerateUniqueBins(Sprite s)
    {
        Texture2D tGPU = s.texture;
        Texture2D t = new Texture2D(tGPU.width, tGPU.height, tGPU.graphicsFormat, tGPU.mipmapCount, TextureCreationFlags.None);
        Graphics.CopyTexture(tGPU, t);
        Debug.Log($"Testing if texture is readable {t.isReadable}");
        
        // Filter unique colors to remove fully transparent pixels and remove transparency
        // Assuming color swapping is essentially only applying to the RGB component not the A component
        List<uint> uniqueColors = (
            from Color32 c in t.GetPixels32()
            where (c.a > 0)
            select (((uint)c.r << 16) | ((uint)c.g << 8) | ((uint)c.b))
            ).Distinct().ToList();
        Debug.Log($"Unique colors = {uniqueColors.Count}");
        // Get the minimum bin count
        uint bins = (uint)uniqueColors.Count;
        List<uint> binIndices = new();
        while (true) // What the fuck????
        {
            Debug.Log($"Testing bin count {bins}");
            binIndices.Clear();
            foreach (uint color in uniqueColors)
            {
                uint index = color % bins;
                if (binIndices.Contains(index))
                {
                    goto CONTINUE_LOOP;
                }
                binIndices.Add(index);
            }
            goto EXIT_LOOP;
            CONTINUE_LOOP: bins++;
        }
        EXIT_LOOP: uint[] colorHashTable = new uint[bins];

        foreach (uint color in uniqueColors)
        {
            colorHashTable[color % bins] = color;
        }

        return colorHashTable;
    }
}
