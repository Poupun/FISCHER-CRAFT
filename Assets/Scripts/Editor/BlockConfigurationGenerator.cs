using UnityEngine;
using UnityEditor;
using System.IO;

public class BlockConfigurationGenerator
{
    [MenuItem("Tools/Generate Block Configurations")]
    public static void GenerateAllBlockConfigurations()
    {
        string folderPath = "Assets/Data/Blocks";
        
        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Data/Blocks"))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Blocks");
        }
        
        // Get all block types from enum
        var blockTypes = System.Enum.GetValues(typeof(BlockType));
        
        foreach (BlockType blockType in blockTypes)
        {
            if (blockType == BlockType.Air) continue; // Skip Air block
            
            string assetPath = $"{folderPath}/{blockType}Block.asset";
            
            // Skip if asset already exists
            if (AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath) != null)
            {
                Debug.Log($"BlockConfiguration for {blockType} already exists, skipping.");
                continue;
            }
            
            // Create new BlockConfiguration
            var blockConfig = ScriptableObject.CreateInstance<BlockConfiguration>();
            blockConfig.blockType = blockType;
            blockConfig.displayName = GetDisplayName(blockType);
            blockConfig.tintColor = Color.white;
            
            // Set properties based on block type
            SetBlockProperties(blockConfig, blockType);
            
            // Create asset
            AssetDatabase.CreateAsset(blockConfig, assetPath);
            Debug.Log($"Created BlockConfiguration for {blockType} at {assetPath}");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Block Configuration generation complete!");
    }
    
    static string GetDisplayName(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.WoodPlanks: return "Wood Planks";
            case BlockType.CraftingTable: return "Crafting Table";
            default: return blockType.ToString();
        }
    }
    
    static void SetBlockProperties(BlockConfiguration config, BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Grass:
                config.hasMultipleSides = true;
                config.hardness = 0.3f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Dirt:
                config.hardness = 0.25f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Stone:
                config.hardness = 0.75f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Sand:
                config.hardness = 0.25f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Coal:
                config.hardness = 1.5f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Log:
                config.hardness = 1.0f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = true;
                config.isMineable = true;
                break;
                
            case BlockType.Leaves:
                config.hardness = 0.1f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Bedrock:
                config.hardness = 0f;
                config.isUnbreakable = true;
                config.isPlaceable = false;
                config.isCraftable = false;
                config.isMineable = false;
                break;
                
            case BlockType.Gravel:
                config.hardness = 0.3f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Iron:
                config.hardness = 1.5f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Gold:
                config.hardness = 1.5f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Diamond:
                config.hardness = 1.5f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
                
            case BlockType.Water:
                config.hardness = 0f;
                config.isUnbreakable = true;
                config.isPlaceable = false;
                config.isCraftable = false;
                config.isMineable = false;
                break;
                
            case BlockType.WoodPlanks:
                config.hardness = 0.8f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = true;
                config.isMineable = true;
                break;
                
            // Note: Stick is now ItemType.Stick, not BlockType.Stick
                
            case BlockType.CraftingTable:
                config.hasMultipleSides = true;
                config.hardness = 0.8f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = true;
                config.isMineable = true;
                break;
                
            default:
                // Default values for any new blocks
                config.hardness = 1.0f;
                config.isUnbreakable = false;
                config.isPlaceable = true;
                config.isCraftable = false;
                config.isMineable = true;
                break;
        }
    }
}