using UnityEngine;
using UnityEditor;

public class FinalCraftingTableFix : EditorWindow
{
    [MenuItem("Tools/Block System/Final CraftingTable Fix")]
    public static void ShowWindow()
    {
        CompleteFix();
    }
    
    static void CompleteFix()
    {
        Debug.Log("=== FINAL CRAFTING TABLE FIX ===");
        
        // 1. Ensure directory exists
        string blocksPath = "Assets/Data/Blocks";
        if (!AssetDatabase.IsValidFolder(blocksPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Blocks");
        }
        
        // 2. Delete any existing meta file
        string metaPath = "Assets/Data/Blocks/CraftingTableBlock.asset.meta";
        if (System.IO.File.Exists(metaPath))
        {
            AssetDatabase.DeleteAsset(metaPath);
        }
        
        // 3. Create fresh CraftingTable asset
        string assetPath = "Assets/Data/Blocks/CraftingTableBlock.asset";
        AssetDatabase.DeleteAsset(assetPath); // Remove if exists
        
        var asset = ScriptableObject.CreateInstance<BlockConfiguration>();
        asset.blockType = BlockType.CraftingTable;
        asset.displayName = "Crafting Table";
        asset.tintColor = Color.white;
        asset.hardness = 0.8f;
        asset.hasMultipleSides = true;
        asset.isUnbreakable = false;
        asset.isPlaceable = true;
        asset.isCraftable = true;
        asset.isMineable = true;
        
        // Load textures
        asset.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
        asset.topTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_top.png");
        asset.sideTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        asset.bottomTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_planks.png");
        
        // New face-specific textures
        asset.frontTexture = asset.mainTexture; // Front is the main texture
        asset.leftTexture = asset.sideTexture;
        asset.rightTexture = asset.sideTexture;
        asset.backTexture = asset.sideTexture;
        
        // Create the asset
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ Created CraftingTableBlock.asset");
        Debug.Log($"   • Main: {asset.mainTexture?.name}");
        Debug.Log($"   • Top: {asset.topTexture?.name}");
        Debug.Log($"   • Side: {asset.sideTexture?.name}");
        Debug.Log($"   • Bottom: {asset.bottomTexture?.name}");
        
        // 4. Update BlockManager
        UpdateBlockManager();
        
        Debug.Log("✅ FINAL FIX COMPLETE!");
        Debug.Log("   Crafting table should now appear properly in inventory!");
    }
    
    static void UpdateBlockManager()
    {
        BlockManager blockManager = Object.FindObjectOfType<BlockManager>();
        if (blockManager == null)
        {
            Debug.LogWarning("No BlockManager found in scene");
            return;
        }
        
        // Load all BlockConfiguration assets
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        var blockConfigs = new System.Collections.Generic.List<BlockConfiguration>();
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                blockConfigs.Add(blockConfig);
            }
        }
        
        // Sort by block type
        blockConfigs.Sort((a, b) => a.blockType.CompareTo(b.blockType));
        
        // Update BlockManager
        SerializedObject serializedBlockManager = new SerializedObject(blockManager);
        SerializedProperty allBlocksProperty = serializedBlockManager.FindProperty("allBlocks");
        
        allBlocksProperty.arraySize = blockConfigs.Count;
        for (int i = 0; i < blockConfigs.Count; i++)
        {
            SerializedProperty elementProperty = allBlocksProperty.GetArrayElementAtIndex(i);
            elementProperty.objectReferenceValue = blockConfigs[i];
        }
        
        serializedBlockManager.ApplyModifiedProperties();
        
        Debug.Log($"✅ Updated BlockManager with {blockConfigs.Count} blocks including CraftingTable");
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(blockManager.gameObject.scene);
    }
}
