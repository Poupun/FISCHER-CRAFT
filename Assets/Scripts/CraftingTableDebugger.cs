using UnityEngine;

public class CraftingTableDebugger : MonoBehaviour
{
    [Header("Debug Info - Updated in Inspector")]
    [SerializeField] private bool craftingTableFound = false;
    [SerializeField] private bool craftingTableHasSprite = false;
    [SerializeField] private string debugInfo = "";
    [SerializeField] private int totalBlocksInManager = 0;
    
    [Header("Manual Actions")]
    [SerializeField] private bool runDebugCheck = false;
    [SerializeField] private bool forceRefreshManager = false;
    
    void Start()
    {
        // Run debug check on start
        RunDebugCheck();
    }
    
    void Update()
    {
        // Check for manual triggers in Inspector
        if (runDebugCheck)
        {
            runDebugCheck = false;
            RunDebugCheck();
        }
        
        if (forceRefreshManager)
        {
            forceRefreshManager = false;
            ForceRefreshBlockManager();
        }
    }
    
    public void RunDebugCheck()
    {
        Debug.Log("=== CRAFTING TABLE DEBUG CHECK ===");
        
        try
        {
            // Check BlockManager
            var blockManager = FindFirstObjectByType<BlockManager>();
            if (blockManager == null)
            {
                debugInfo = "❌ No BlockManager found in scene!";
                Debug.LogError(debugInfo);
                return;
            }
            
            // Get total blocks count using reflection
            var allBlocksField = typeof(BlockManager).GetField("allBlocks", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (allBlocksField != null)
            {
                var allBlocks = allBlocksField.GetValue(blockManager) as System.Collections.Generic.List<BlockConfiguration>;
                totalBlocksInManager = allBlocks?.Count ?? 0;
                
                int nullCount = 0;
                if (allBlocks != null)
                {
                    foreach (var block in allBlocks)
                    {
                        if (block == null) nullCount++;
                    }
                }
                
                Debug.Log($"BlockManager has {totalBlocksInManager} total slots, {nullCount} are null");
            }
            
            // Test CraftingTable configuration
            var craftingTableConfig = BlockManager.GetBlockConfiguration(BlockType.CraftingTable);
            craftingTableFound = (craftingTableConfig != null);
            
            if (!craftingTableFound)
            {
                debugInfo = "❌ CraftingTable configuration NOT FOUND - this causes white squares!";
                Debug.LogError(debugInfo);
                Debug.LogError("Solution: The CraftingTableBlock.asset is not loaded in BlockManager");
            }
            else
            {
                Debug.Log($"✅ CraftingTable configuration found: {craftingTableConfig.displayName}");
                
                // Test sprite generation
                var sprite = BlockManager.GetBlockSprite(BlockType.CraftingTable);
                craftingTableHasSprite = (sprite != null);
                
                if (!craftingTableHasSprite)
                {
                    debugInfo = "❌ CraftingTable sprite generation FAILED - white squares!";
                    Debug.LogError(debugInfo);
                    
                    if (craftingTableConfig.mainTexture == null)
                    {
                        Debug.LogError("   Reason: mainTexture is null in BlockConfiguration");
                    }
                    else
                    {
                        Debug.LogError($"   mainTexture exists ({craftingTableConfig.mainTexture.name}) but sprite creation failed");
                    }
                }
                else
                {
                    debugInfo = "✅ CraftingTable working properly - no white squares!";
                    Debug.Log($"✅ CraftingTable sprite: {sprite.name}");
                }
            }
        }
        catch (System.Exception e)
        {
            debugInfo = $"❌ Error during debug check: {e.Message}";
            Debug.LogError(debugInfo);
        }
        
        Debug.Log("=== DEBUG CHECK COMPLETE ===");
    }
    
    public void ForceRefreshBlockManager()
    {
        Debug.Log("=== FORCE REFRESH BLOCK MANAGER ===");
        
        try
        {
            var blockManager = FindFirstObjectByType<BlockManager>();
            if (blockManager == null)
            {
                Debug.LogError("No BlockManager found!");
                return;
            }
            
            // Force refresh using reflection
            var initMethod = typeof(BlockManager).GetMethod("InitializeBlockRegistry", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (initMethod != null)
            {
                Debug.Log("Calling InitializeBlockRegistry...");
                initMethod.Invoke(blockManager, null);
                Debug.Log("✅ BlockManager refresh completed");
                
                // Run debug check after refresh
                RunDebugCheck();
            }
            else
            {
                Debug.LogError("Could not find InitializeBlockRegistry method");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during refresh: {e.Message}");
        }
    }
    
    [ContextMenu("Fix White Squares")]
    public void FixWhiteSquares()
    {
        RunDebugCheck();
        
        if (!craftingTableFound)
        {
            Debug.Log("Attempting to fix by refreshing BlockManager...");
            ForceRefreshBlockManager();
        }
    }
    
    [ContextMenu("List All Block Types")]
    public void ListAllBlockTypes()
    {
        Debug.Log("=== ALL BLOCK TYPES STATUS ===");
        
        var blockTypes = System.Enum.GetValues(typeof(BlockType));
        foreach (BlockType blockType in blockTypes)
        {
            if (blockType == BlockType.Air) continue;
            
            var config = BlockManager.GetBlockConfiguration(blockType);
            var sprite = BlockManager.GetBlockSprite(blockType);
            
            string status = "❌ Missing";
            if (config != null && sprite != null)
                status = "✅ Working";
            else if (config != null)
                status = "⚠️ Config only";
            
            Debug.Log($"{status} {blockType}");
        }
        
        Debug.Log("=== LIST COMPLETE ===");
    }
}
