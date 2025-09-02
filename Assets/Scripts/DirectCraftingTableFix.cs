using UnityEngine;

public class DirectCraftingTableFix : MonoBehaviour
{
    [ContextMenu("Fix CraftingTable White Squares")]
    public void FixCraftingTableWhiteSquares()
    {
        Debug.Log("=== DIRECT CRAFTING TABLE FIX ===");
        
        // Test if CraftingTable is working
        var config = BlockManager.GetBlockConfiguration(BlockType.CraftingTable);
        if (config == null)
        {
            Debug.LogError("❌ CraftingTable configuration not found!");
            Debug.LogError("This is why you see white squares in inventory.");
            
            // Try to force re-initialization
            var blockManager = FindObjectOfType<BlockManager>();
            if (blockManager != null)
            {
                Debug.Log("Attempting to force BlockManager refresh...");
                
                // Use reflection to call private InitializeBlockRegistry method
                var initMethod = typeof(BlockManager).GetMethod("InitializeBlockRegistry", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (initMethod != null)
                {
                    initMethod.Invoke(blockManager, null);
                    Debug.Log("✅ BlockManager refreshed");
                    
                    // Test again
                    config = BlockManager.GetBlockConfiguration(BlockType.CraftingTable);
                    if (config != null)
                    {
                        Debug.Log("✅ CraftingTable configuration now found after refresh!");
                    }
                    else
                    {
                        Debug.LogError("❌ CraftingTable still not found. Check if CraftingTableBlock.asset exists in Assets/Data/Blocks/");
                    }
                }
            }
        }
        else
        {
            Debug.Log($"✅ CraftingTable configuration found: {config.displayName}");
            
            // Test sprite generation
            var sprite = BlockManager.GetBlockSprite(BlockType.CraftingTable);
            if (sprite == null)
            {
                Debug.LogError("❌ Failed to generate sprite for CraftingTable");
                if (config.mainTexture == null)
                {
                    Debug.LogError("   Reason: mainTexture is null in the BlockConfiguration");
                }
                else
                {
                    Debug.LogError($"   mainTexture exists: {config.mainTexture.name}, but sprite creation failed");
                }
            }
            else
            {
                Debug.Log($"✅ CraftingTable sprite generated successfully: {sprite.name}");
                Debug.Log("✅ This should fix the white squares in inventory!");
            }
        }
        
        Debug.Log("=== FIX ATTEMPT COMPLETE ===");
    }
    
    [ContextMenu("List All Block Assets")]
    public void ListAllBlockAssets()
    {
        Debug.Log("=== ALL BLOCK ASSETS ===");
        
        var blockTypes = System.Enum.GetValues(typeof(BlockType));
        foreach (BlockType blockType in blockTypes)
        {
            if (blockType == BlockType.Air) continue;
            
            var config = BlockManager.GetBlockConfiguration(blockType);
            if (config == null)
            {
                Debug.LogWarning($"❌ {blockType}: No configuration found");
            }
            else
            {
                var sprite = BlockManager.GetBlockSprite(blockType);
                if (sprite == null)
                {
                    Debug.LogWarning($"⚠️ {blockType}: Config found but no sprite");
                }
                else
                {
                    Debug.Log($"✅ {blockType}: Working properly");
                }
            }
        }
        
        Debug.Log("=== LIST COMPLETE ===");
    }
}
