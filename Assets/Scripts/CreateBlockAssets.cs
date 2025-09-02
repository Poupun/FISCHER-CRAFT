using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CreateBlockAssets : MonoBehaviour
{
    [ContextMenu("Create All Block Assets")]
    public void CreateAllBlockAssets()
    {
#if UNITY_EDITOR
        // Ensure directory exists
        string assetPath = "Assets/Data/Blocks";
        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Blocks");
        }

        CreateBlockAsset(BlockType.Grass, "Grass", 0.3f, true, false, true, false, true);
        CreateBlockAsset(BlockType.Dirt, "Dirt", 0.25f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Stone, "Stone", 0.75f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Sand, "Sand", 0.25f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Coal, "Coal", 1.5f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Log, "Log", 1.0f, true, false, true, true, true);
        CreateBlockAsset(BlockType.Leaves, "Leaves", 0.1f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Bedrock, "Bedrock", 0f, false, true, false, false, false);
        CreateBlockAsset(BlockType.Gravel, "Gravel", 0.3f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Iron, "Iron Ore", 1.5f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Gold, "Gold Ore", 1.5f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Diamond, "Diamond Ore", 1.5f, false, false, true, false, true);
        CreateBlockAsset(BlockType.Water, "Water", 0f, false, true, false, false, false);
        CreateBlockAsset(BlockType.WoodPlanks, "Wood Planks", 0.8f, false, false, true, true, true);
        // Note: Stick is now an ItemType, not BlockType
        CreateBlockAsset(BlockType.CraftingTable, "Crafting Table", 0.8f, true, false, true, true, true);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Created and saved all block assets!");
#else
        Debug.LogWarning("CreateBlockAssets only works in editor mode!");
#endif
    }
    
    void CreateBlockAsset(BlockType blockType, string displayName, float hardness, bool multiSides, bool unbreakable, bool placeable, bool craftable, bool mineable)
    {
#if UNITY_EDITOR
        string assetPath = $"Assets/Data/Blocks/{blockType}Block.asset";
        
        // Check if asset already exists
        var existingAsset = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
        if (existingAsset != null)
        {
            Debug.Log($"Asset already exists for {blockType}, skipping creation");
            return;
        }
        
        var asset = ScriptableObject.CreateInstance<BlockConfiguration>();
        asset.blockType = blockType;
        asset.displayName = displayName;
        asset.tintColor = Color.white;
        asset.hardness = hardness;
        asset.hasMultipleSides = multiSides;
        asset.isUnbreakable = unbreakable;
        asset.isPlaceable = placeable;
        asset.isCraftable = craftable;
        asset.isMineable = mineable;
        
        // Auto-assign textures based on naming conventions
        AssignTextures(asset);
        
        AssetDatabase.CreateAsset(asset, assetPath);
        Debug.Log($"Created BlockConfiguration asset for {blockType} - {displayName} at {assetPath}");
#endif
    }
    
#if UNITY_EDITOR
    void AssignTextures(BlockConfiguration config)
    {
        // Try to find textures based on block type
        string blockName = config.blockType.ToString().ToLower();
        
        // Check Resources folder first (for runtime loading compatibility)
        config.mainTexture = Resources.Load<Texture2D>($"Textures/{blockName}");
        
        // If not found in Resources, try direct asset loading
        if (config.mainTexture == null)
        {
            config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/{blockName}.png");
        }
        
        // Try alternative names for common blocks
        if (config.mainTexture == null)
        {
            switch (config.blockType)
            {
                case BlockType.Grass:
                    config.mainTexture = Resources.Load<Texture2D>("Textures/top_grass") ?? 
                                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass_block_top.png");
                    config.sideTexture = Resources.Load<Texture2D>("Textures/side_grass") ?? 
                                       AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass_block_side.png");
                    config.bottomTexture = Resources.Load<Texture2D>("Textures/dirt") ?? 
                                         AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/dirt.png");
                    break;
                case BlockType.Log:
                    config.mainTexture = Resources.Load<Texture2D>("Textures/log") ?? 
                                        Resources.Load<Texture2D>("Textures/oak_log") ??
                                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_log.png");
                    config.topTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_log_top.png");
                    config.sideTexture = config.mainTexture;
                    config.bottomTexture = config.topTexture;
                    break;
                case BlockType.CraftingTable:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
                    config.topTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_top.png");
                    config.sideTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
                    config.bottomTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_planks.png");
                    break;
                case BlockType.Stone:
                    config.mainTexture = Resources.Load<Texture2D>("Textures/stone") ?? 
                                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/stone.png");
                    break;
                case BlockType.Dirt:
                    config.mainTexture = Resources.Load<Texture2D>("Textures/dirt") ?? 
                                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/dirt.png");
                    break;
                case BlockType.Sand:
                    config.mainTexture = Resources.Load<Texture2D>("Textures/sand") ?? 
                                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sand.png");
                    break;
                case BlockType.Leaves:
                    config.mainTexture = Resources.Load<Texture2D>("Textures/leaves") ?? 
                                        AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_leaves.png");
                    break;
                case BlockType.WoodPlanks:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_planks.png");
                    break;
                case BlockType.Coal:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/coal_ore.png");
                    break;
                case BlockType.Iron:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/iron_ore.png");
                    break;
                case BlockType.Gold:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/gold_ore.png");
                    break;
                case BlockType.Diamond:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/diamond_ore.png");
                    break;
                case BlockType.Bedrock:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/bedrock.png");
                    break;
                case BlockType.Gravel:
                    config.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/gravel.png");
                    break;
                // Note: Stick texture is now handled by ItemManager, not BlockManager
            }
        }
        
        if (config.mainTexture != null)
        {
            Debug.Log($"Assigned texture for {config.blockType}: {config.mainTexture.name}");
        }
        else
        {
            Debug.LogWarning($"No texture found for {config.blockType}");
        }
    }
#endif
}