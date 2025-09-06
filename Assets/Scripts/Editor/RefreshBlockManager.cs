using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class RefreshBlockManager : EditorWindow
{
    [MenuItem("Tools/Block System/Refresh BlockManager Now")]
    public static void ShowWindow()
    {
        RefreshBlockManagerAssets();
    }
    
    static void RefreshBlockManagerAssets()
    {
        Debug.Log("=== REFRESHING BLOCKMANAGER ===");
        
        BlockManager blockManager = Object.FindObjectOfType<BlockManager>();
        if (blockManager == null)
        {
            Debug.LogError("No BlockManager found in scene!");
            return;
        }
        
        // Find all BlockConfiguration assets (including the new CraftingTable)
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        var blockConfigs = new List<BlockConfiguration>();
        
        Debug.Log($"Found {assetGUIDs.Length} BlockConfiguration assets:");
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                blockConfigs.Add(blockConfig);
                Debug.Log($"   • {blockConfig.blockType} - {blockConfig.displayName}");
            }
        }
        
        // Sort by block type for consistent ordering
        blockConfigs.Sort((a, b) => a.blockType.CompareTo(b.blockType));
        
        // Update BlockManager using SerializedObject
        SerializedObject serializedBlockManager = new SerializedObject(blockManager);
        SerializedProperty allBlocksProperty = serializedBlockManager.FindProperty("allBlocks");
        
        // Clear and set new array
        allBlocksProperty.arraySize = blockConfigs.Count;
        
        for (int i = 0; i < blockConfigs.Count; i++)
        {
            SerializedProperty elementProperty = allBlocksProperty.GetArrayElementAtIndex(i);
            elementProperty.objectReferenceValue = blockConfigs[i];
        }
        
        // Apply changes
        serializedBlockManager.ApplyModifiedProperties();
        
        Debug.Log($"✅ BlockManager refreshed with {blockConfigs.Count} assets");
        
        // Check if CraftingTable is included
        bool hasCraftingTable = blockConfigs.Any(b => b.blockType == BlockType.CraftingTable);
        if (hasCraftingTable)
        {
            Debug.Log("✅ CraftingTable asset is properly loaded!");
        }
        else
        {
            Debug.LogError("❌ CraftingTable asset is missing from BlockManager!");
        }
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(blockManager.gameObject.scene);
    }
}
