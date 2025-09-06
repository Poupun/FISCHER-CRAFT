using UnityEngine;
using UnityEditor;

public class SimpleBlockSystemTest
{
    [MenuItem("Tools/Block System/Simple Test")]
    public static void SimpleTest()
    {
        Debug.Log("=== SIMPLE BLOCK SYSTEM TEST ===");
        
        // Test CraftingTable configuration
        var craftingTableConfig = BlockManager.GetBlockConfiguration(BlockType.CraftingTable);
        if (craftingTableConfig == null)
        {
            Debug.LogError("❌ CraftingTable configuration not found! This explains the white squares.");
            
            // Check if BlockManager exists
            BlockManager blockManager = Object.FindObjectOfType<BlockManager>();
            if (blockManager == null)
            {
                Debug.LogError("❌ No BlockManager found in scene!");
            }
            else
            {
                Debug.Log("✅ BlockManager found in scene");
                
                // Check if auto-load is enabled
                SerializedObject so = new SerializedObject(blockManager);
                var autoLoadProp = so.FindProperty("autoLoadBlockAssets");
                if (autoLoadProp != null && autoLoadProp.boolValue)
                {
                    Debug.Log("✅ Auto-load is enabled");
                }
                else
                {
                    Debug.LogWarning("⚠️ Auto-load is disabled");
                }
            }
        }
        else
        {
            Debug.Log($"✅ CraftingTable configuration found: {craftingTableConfig.displayName}");
            
            // Test sprite generation
            var sprite = BlockManager.GetBlockSprite(BlockType.CraftingTable);
            if (sprite == null)
            {
                Debug.LogError("❌ Failed to generate CraftingTable sprite - this causes white squares!");
                if (craftingTableConfig.mainTexture == null)
                {
                    Debug.LogError("   Reason: mainTexture is null");
                }
            }
            else
            {
                Debug.Log("✅ CraftingTable sprite generated successfully - should fix white squares!");
            }
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    [MenuItem("Tools/Block System/Force Refresh")]
    public static void ForceRefresh()
    {
        Debug.Log("=== FORCE REFRESH BLOCK SYSTEM ===");
        
        BlockManager blockManager = Object.FindObjectOfType<BlockManager>();
        if (blockManager == null)
        {
            Debug.LogError("No BlockManager found!");
            return;
        }
        
        // Force refresh by calling InitializeBlockRegistry
        var initMethod = typeof(BlockManager).GetMethod("InitializeBlockRegistry", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (initMethod != null)
        {
            initMethod.Invoke(blockManager, null);
            Debug.Log("✅ Forced BlockManager refresh");
        }
        
        // Test result
        var config = BlockManager.GetBlockConfiguration(BlockType.CraftingTable);
        if (config != null)
        {
            Debug.Log("✅ CraftingTable configuration loaded successfully!");
        }
        else
        {
            Debug.LogError("❌ CraftingTable still not found after refresh");
        }
    }
}
