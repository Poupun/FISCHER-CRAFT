using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines block categories for tool effectiveness
/// </summary>
public enum BlockCategory
{
    Stone,      // Stone, Coal, Iron, Gold, Diamond - best with pickaxe
    Wood,       // Log, Leaves - best with axe  
    Dirt,       // Dirt, Grass, Sand, Gravel - best with shovel
    Other       // Everything else - no specific tool bonus
}

/// <summary>
/// Tool effectiveness multipliers for different block categories
/// </summary>
[System.Serializable]
public class ToolEffectivenessData
{
    [Header("Tool Information")]
    public ItemType toolType;
    public string toolName;
    
    [Header("Effectiveness Multipliers")]
    [Tooltip("Mining speed multiplier for optimal block types")]
    public float optimalMultiplier = 3.0f;
    
    [Tooltip("Mining speed multiplier for sub-optimal block types")]
    public float subOptimalMultiplier = 1.0f;
    
    [Tooltip("Block categories this tool is most effective on")]
    public BlockCategory[] optimalCategories;
    
    [Header("Tool Tier")]
    [Tooltip("Tool material tier - higher tiers are faster")]
    public int tierLevel = 1; // 1 = Wood, 2 = Stone, 3 = Iron, etc.
    
    [Tooltip("Tier speed bonus multiplier")]
    public float tierMultiplier = 1.0f;
    
    public ToolEffectivenessData(ItemType tool, string name, float optimal, BlockCategory[] categories, int tier = 1)
    {
        toolType = tool;
        toolName = name;
        optimalMultiplier = optimal;
        optimalCategories = categories;
        tierLevel = tier;
        tierMultiplier = 1.0f + (tier - 1) * 0.5f; // Wood=1.0x, Stone=1.5x, Iron=2.0x
    }
}

/// <summary>
/// Static system for managing tool effectiveness and mining speed calculations
/// </summary>
public static class ToolEffectivenessSystem
{
    private static Dictionary<ItemType, ToolEffectivenessData> toolData;
    private static Dictionary<BlockType, BlockCategory> blockCategories;
    
    static ToolEffectivenessSystem()
    {
        InitializeSystem();
    }
    
    private static void InitializeSystem()
    {
        InitializeToolData();
        InitializeBlockCategories();
    }
    
    private static void InitializeToolData()
    {
        toolData = new Dictionary<ItemType, ToolEffectivenessData>();
        
        // Define tool effectiveness data
        var tools = new ToolEffectivenessData[]
        {
            // Pickaxes - best for stone-type blocks
            new ToolEffectivenessData(ItemType.WoodPickaxe, "Wooden Pickaxe", 4.0f, new[] { BlockCategory.Stone }, 1),
            new ToolEffectivenessData(ItemType.StonePickaxe, "Stone Pickaxe", 4.0f, new[] { BlockCategory.Stone }, 2),
            new ToolEffectivenessData(ItemType.IronPickaxe, "Iron Pickaxe", 4.0f, new[] { BlockCategory.Stone }, 3),
            
            // Axes - best for wood-type blocks
            new ToolEffectivenessData(ItemType.WoodAxe, "Wooden Axe", 4.0f, new[] { BlockCategory.Wood }, 1),
            new ToolEffectivenessData(ItemType.StoneAxe, "Stone Axe", 4.0f, new[] { BlockCategory.Wood }, 2),
            new ToolEffectivenessData(ItemType.IronAxe, "Iron Axe", 4.0f, new[] { BlockCategory.Wood }, 3),
            
            // Shovels - best for dirt-type blocks  
            new ToolEffectivenessData(ItemType.WoodShovel, "Wooden Shovel", 4.0f, new[] { BlockCategory.Dirt }, 1),
            new ToolEffectivenessData(ItemType.StoneShovel, "Stone Shovel", 4.0f, new[] { BlockCategory.Dirt }, 2),
            new ToolEffectivenessData(ItemType.IronShovel, "Iron Shovel", 4.0f, new[] { BlockCategory.Dirt }, 3)
        };
        
        foreach (var tool in tools)
        {
            // Calculate tier multiplier: Wood=1.0x, Stone=1.5x, Iron=2.0x
            tool.tierMultiplier = 1.0f + (tool.tierLevel - 1) * 0.5f;
            toolData[tool.toolType] = tool;
        }
        
        Debug.Log($"ToolEffectivenessSystem: Initialized {toolData.Count} tools");
    }
    
    private static void InitializeBlockCategories()
    {
        blockCategories = new Dictionary<BlockType, BlockCategory>();
        
        // Stone category - best mined with pickaxes
        var stoneBlocks = new[] { BlockType.Stone, BlockType.Coal, BlockType.Iron, BlockType.Gold, BlockType.Diamond };
        foreach (var block in stoneBlocks)
        {
            blockCategories[block] = BlockCategory.Stone;
        }
        
        // Wood category - best mined with axes
        var woodBlocks = new[] { BlockType.Log, BlockType.Leaves, BlockType.WoodPlanks };
        foreach (var block in woodBlocks)
        {
            blockCategories[block] = BlockCategory.Wood;
        }
        
        // Dirt category - best mined with shovels
        var dirtBlocks = new[] { BlockType.Dirt, BlockType.Grass, BlockType.Sand, BlockType.Gravel };
        foreach (var block in dirtBlocks)
        {
            blockCategories[block] = BlockCategory.Dirt;
        }
        
        // Other blocks default to Other category
        var otherBlocks = new[] { BlockType.Air, BlockType.Water, BlockType.Bedrock, BlockType.CraftingTable };
        foreach (var block in otherBlocks)
        {
            blockCategories[block] = BlockCategory.Other;
        }
        
        Debug.Log($"ToolEffectivenessSystem: Initialized {blockCategories.Count} block categories");
    }
    
    /// <summary>
    /// Calculate mining speed multiplier based on tool and block type
    /// </summary>
    public static float GetMiningSpeedMultiplier(ItemType toolType, BlockType blockType)
    {
        // Base multiplier (no tool)
        float multiplier = 1.0f;
        
        // Check if we have tool data
        if (!toolData.TryGetValue(toolType, out ToolEffectivenessData tool))
        {
            // Not a recognized tool, return base speed
            return multiplier;
        }
        
        // Get block category
        BlockCategory blockCategory = GetBlockCategory(blockType);
        
        // Check if tool is optimal for this block category
        bool isOptimal = System.Array.Exists(tool.optimalCategories, category => category == blockCategory);
        
        if (isOptimal)
        {
            // Apply optimal multiplier + tier bonus
            multiplier = tool.optimalMultiplier * tool.tierMultiplier;
            Debug.Log($"ToolEffectiveness: {tool.toolName} optimal for {blockType} ({blockCategory}) - {multiplier:F1}x speed");
        }
        else
        {
            // Apply sub-optimal multiplier + reduced tier bonus
            multiplier = tool.subOptimalMultiplier * (1.0f + (tool.tierMultiplier - 1.0f) * 0.5f);
            Debug.Log($"ToolEffectiveness: {tool.toolName} sub-optimal for {blockType} ({blockCategory}) - {multiplier:F1}x speed");
        }
        
        return multiplier;
    }
    
    /// <summary>
    /// Get mining speed multiplier for the currently held tool
    /// </summary>
    public static float GetMiningSpeedMultiplierForHeldTool(BlockType blockType)
    {
        // Find the player's inventory
        var playerInventory = UnityEngine.Object.FindFirstObjectByType<UnifiedPlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("ToolEffectivenessSystem: No UnifiedPlayerInventory found");
            return 1.0f;
        }
        
        // Get currently selected item
        var selectedEntry = playerInventory.GetSelectedEntry();
        if (selectedEntry.IsEmpty || selectedEntry.entryType != InventoryEntryType.Item)
        {
            // No tool held, return base speed
            return 1.0f;
        }
        
        return GetMiningSpeedMultiplier(selectedEntry.itemType, blockType);
    }
    
    /// <summary>
    /// Get the category of a block type
    /// </summary>
    public static BlockCategory GetBlockCategory(BlockType blockType)
    {
        if (blockCategories.TryGetValue(blockType, out BlockCategory category))
        {
            return category;
        }
        
        // Default to Other category for unknown blocks
        return BlockCategory.Other;
    }
    
    /// <summary>
    /// Check if a tool is optimal for a block type
    /// </summary>
    public static bool IsToolOptimalForBlock(ItemType toolType, BlockType blockType)
    {
        if (!toolData.TryGetValue(toolType, out ToolEffectivenessData tool))
        {
            return false;
        }
        
        BlockCategory blockCategory = GetBlockCategory(blockType);
        return System.Array.Exists(tool.optimalCategories, category => category == blockCategory);
    }
    
    /// <summary>
    /// Get tool information for UI display
    /// </summary>
    public static ToolEffectivenessData GetToolData(ItemType toolType)
    {
        toolData.TryGetValue(toolType, out ToolEffectivenessData data);
        return data;
    }
    
    /// <summary>
    /// Get all tools effective for a block category
    /// </summary>
    public static ItemType[] GetOptimalToolsForBlock(BlockType blockType)
    {
        BlockCategory category = GetBlockCategory(blockType);
        var optimalTools = new List<ItemType>();
        
        foreach (var kvp in toolData)
        {
            if (System.Array.Exists(kvp.Value.optimalCategories, cat => cat == category))
            {
                optimalTools.Add(kvp.Key);
            }
        }
        
        return optimalTools.ToArray();
    }
    
    /// <summary>
    /// Debug method to log effectiveness information
    /// </summary>
    public static void LogEffectivenessInfo(ItemType toolType, BlockType blockType)
    {
        float multiplier = GetMiningSpeedMultiplier(toolType, blockType);
        BlockCategory category = GetBlockCategory(blockType);
        bool isOptimal = IsToolOptimalForBlock(toolType, blockType);
        
        Debug.Log($"Tool Effectiveness: {toolType} on {blockType} ({category}) = {multiplier:F1}x speed (optimal: {isOptimal})");
    }
}