using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class BlockHardnessOverride
{
    [Header("Block Hardness Override")]
    public BlockType blockType;
    [Range(0f, 10f)]
    [Tooltip("Mining hardness (higher = slower to mine). 0 = instant break")]
    public float hardness = 1f;
    [Tooltip("Cannot be broken by any means")]
    public bool isUnbreakable = false;
    [Tooltip("Display name for this block")]
    public string displayName;
    
    public BlockHardnessOverride(BlockType type, float hardnessValue, string name = "")
    {
        blockType = type;
        hardness = hardnessValue;
        displayName = string.IsNullOrEmpty(name) ? type.ToString() : name;
        isUnbreakable = false;
    }
}

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }
    
    [Header("Block Configuration")]
    [SerializeField] private BlockConfiguration[] allBlocks;
    [Header("Auto-Load Settings")]
    [SerializeField] private bool autoLoadBlockAssets = true;
    [SerializeField] private string blockAssetsPath = "Assets/Data/Blocks";
    
    [Header("Hardness Tweaking")]
    [Tooltip("Enable this to show hardness tweaking controls in the inspector")]
    [SerializeField] private bool showHardnessTweaker = true;
    [Tooltip("Override hardness values at runtime (does not save to assets)")]
    [SerializeField] private BlockHardnessOverride[] hardnessOverrides;
    
    private Dictionary<BlockType, BlockConfiguration> blockRegistry;
    private Dictionary<BlockType, Sprite> spriteCache;
    private Dictionary<BlockType, Material> materialCache;
    private Dictionary<BlockType, BlockHardnessOverride> hardnessOverrideCache;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Move to root if not already there for DontDestroyOnLoad
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
        
        InitializeBlockRegistry();
    }
    
    void InitializeBlockRegistry()
    {
        blockRegistry = new Dictionary<BlockType, BlockConfiguration>();
        spriteCache = new Dictionary<BlockType, Sprite>();
        materialCache = new Dictionary<BlockType, Material>();
        hardnessOverrideCache = new Dictionary<BlockType, BlockHardnessOverride>();
        
        // Auto-load block assets if enabled and allBlocks is empty or has null entries
        if (autoLoadBlockAssets && (allBlocks == null || allBlocks.Length == 0 || System.Array.Exists(allBlocks, x => x == null)))
        {
            LoadBlockAssetsFromPath();
        }
        
        if (allBlocks != null)
        {
            foreach (var block in allBlocks)
            {
                if (block != null && block.blockType != BlockType.Air)
                {
                    blockRegistry[block.blockType] = block;
                    Debug.Log($"BlockManager: Registered {block.blockType} - {block.displayName}");
                }
            }
        }
        
        Debug.Log($"BlockManager: Initialized with {blockRegistry.Count} blocks");
        
        // Initialize hardness overrides
        InitializeHardnessOverrides();
    }
    
    void InitializeHardnessOverrides()
    {
        // Initialize hardness overrides with default values if empty
        if (hardnessOverrides == null || hardnessOverrides.Length == 0)
        {
            CreateDefaultHardnessOverrides();
        }
        
        // Cache hardness overrides for fast lookup
        hardnessOverrideCache.Clear();
        if (hardnessOverrides != null)
        {
            foreach (var hardnessOverride in hardnessOverrides)
            {
                if (hardnessOverride != null)
                {
                    hardnessOverrideCache[hardnessOverride.blockType] = hardnessOverride;
                }
            }
        }
        
        Debug.Log($"BlockManager: Loaded {hardnessOverrideCache.Count} hardness overrides");
    }
    
    void CreateDefaultHardnessOverrides()
    {
        // Create default hardness values for all known block types
        var defaultOverrides = new List<BlockHardnessOverride>
        {
            new BlockHardnessOverride(BlockType.Air, 0f, "Air"),
            new BlockHardnessOverride(BlockType.Grass, 0.3f, "Grass Block"),
            new BlockHardnessOverride(BlockType.Dirt, 0.25f, "Dirt"),
            new BlockHardnessOverride(BlockType.Stone, 0.75f, "Stone"),
            new BlockHardnessOverride(BlockType.Sand, 0.25f, "Sand"),
            new BlockHardnessOverride(BlockType.Gravel, 0.3f, "Gravel"),
            new BlockHardnessOverride(BlockType.Log, 1.0f, "Oak Log"),
            new BlockHardnessOverride(BlockType.Leaves, 0.1f, "Oak Leaves"),
            new BlockHardnessOverride(BlockType.Coal, 1.5f, "Coal Ore"),
            new BlockHardnessOverride(BlockType.Iron, 1.5f, "Iron Ore"),
            new BlockHardnessOverride(BlockType.Gold, 1.5f, "Gold Ore"),
            new BlockHardnessOverride(BlockType.Diamond, 1.5f, "Diamond Ore"),
            new BlockHardnessOverride(BlockType.WoodPlanks, 1.0f, "Oak Planks"),
            new BlockHardnessOverride(BlockType.CraftingTable, 1.25f, "Crafting Table"),
            new BlockHardnessOverride(BlockType.Water, 0f, "Water"),
        };
        
        // Set bedrock as unbreakable
        var bedrockOverride = new BlockHardnessOverride(BlockType.Bedrock, 5f, "Bedrock");
        bedrockOverride.isUnbreakable = true;
        defaultOverrides.Add(bedrockOverride);
        
        hardnessOverrides = defaultOverrides.ToArray();
        Debug.Log($"BlockManager: Created {hardnessOverrides.Length} default hardness overrides");
    }
    
    void LoadBlockAssetsFromPath()
    {
#if UNITY_EDITOR
        // Load all BlockConfiguration assets from the specified path
        string[] assetGUIDs = UnityEditor.AssetDatabase.FindAssets("t:BlockConfiguration", new[] { blockAssetsPath });
        var blockList = new System.Collections.Generic.List<BlockConfiguration>();
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                blockList.Add(blockConfig);
                Debug.Log($"BlockManager: Found and loaded {blockConfig.blockType} from {assetPath}");
            }
            else
            {
                Debug.LogWarning($"BlockManager: Failed to load BlockConfiguration from {assetPath}");
            }
        }
        
        allBlocks = blockList.ToArray();
        Debug.Log($"BlockManager: Auto-loaded {allBlocks.Length} block configurations from {blockAssetsPath}");
#else
        // In runtime builds, fall back to Resources loading
        var blockConfigs = Resources.LoadAll<BlockConfiguration>("Blocks");
        if (blockConfigs.Length > 0)
        {
            allBlocks = blockConfigs;
            Debug.Log($"BlockManager: Loaded {allBlocks.Length} block configurations from Resources/Blocks");
        }
        else
        {
            Debug.LogWarning("BlockManager: No block configurations found in Resources/Blocks. Make sure to move assets to Resources folder for runtime builds.");
        }
#endif
    }
    
    public static BlockConfiguration GetBlockConfiguration(BlockType blockType)
    {
        if (Instance == null)
        {
            Debug.LogWarning("BlockManager: Instance not found, cannot get block data");
            return null;
        }
        
        Instance.blockRegistry.TryGetValue(blockType, out BlockConfiguration data);
        return data;
    }
    
    public static Sprite GetBlockSprite(BlockType blockType)
    {
        if (blockType == BlockType.Air)
            return null;
            
        if (Instance == null)
        {
            Debug.LogWarning("BlockManager: Instance not found, using fallback sprite generation");
            return GetFallbackSprite(blockType);
        }
        
        if (Instance.spriteCache.TryGetValue(blockType, out Sprite cachedSprite))
        {
            return cachedSprite;
        }
        
        var blockData = GetBlockConfiguration(blockType);
        if (blockData == null)
        {
            Debug.LogWarning($"BlockManager: No data found for {blockType}");
            return null;
        }
        
        Texture2D texture = blockData.mainTexture;
        if (texture == null)
        {
            Debug.LogWarning($"BlockManager: No texture assigned for {blockType}");
            return null;
        }
        
        Sprite sprite = Sprite.Create(
            texture, 
            new Rect(0, 0, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f), 
            32f
        );
        sprite.name = $"{blockType}_Icon";
        
        Instance.spriteCache[blockType] = sprite;
        return sprite;
    }
    
    // Optional method to get 3D rendered icons for inventory display
    public static Sprite GetInventoryIcon(BlockType blockType)
    {
        // Try 3D icon first if renderer is available
        if (Simple3DIconRenderer.Instance != null)
        {
            var icon3D = Simple3DIconRenderer.Instance.Get3DIcon(blockType);
            if (icon3D != null) return icon3D;
        }
        
        // Fallback to regular flat sprite
        return GetBlockSprite(blockType);
    }
    
    public static float GetHardness(BlockType blockType)
    {
        if (Instance == null)
        {
            // Fallback to legacy system or default values
            return BlockHardnessSystem.GetHardnessFromLegacySystem(blockType);
        }
        
        // Check for hardness override first
        if (Instance.hardnessOverrideCache.TryGetValue(blockType, out BlockHardnessOverride hardnessOverride))
        {
            return hardnessOverride.hardness;
        }
        
        // Fallback to block configuration
        var blockData = GetBlockConfiguration(blockType);
        return blockData?.hardness ?? 1f;
    }
    
    public static bool IsUnbreakable(BlockType blockType)
    {
        if (Instance == null)
        {
            // Fallback to legacy system or default values
            return blockType == BlockType.Bedrock;
        }
        
        // Check for hardness override first
        if (Instance.hardnessOverrideCache.TryGetValue(blockType, out BlockHardnessOverride hardnessOverride))
        {
            return hardnessOverride.isUnbreakable;
        }
        
        // Fallback to block configuration
        var blockData = GetBlockConfiguration(blockType);
        return blockData?.isUnbreakable ?? false;
    }
    
    public static bool IsMineable(BlockType blockType)
    {
        if (Instance == null)
        {
            return blockType != BlockType.Air && blockType != BlockType.Bedrock;
        }
        var blockData = GetBlockConfiguration(blockType);
        return blockData?.isMineable ?? true;
    }
    
    public static bool IsPlaceable(BlockType blockType)
    {
        if (Instance == null)
        {
            return blockType != BlockType.Air;
        }
        var blockData = GetBlockConfiguration(blockType);
        return blockData?.isPlaceable ?? true;
    }
    
    public static string GetDisplayName(BlockType blockType)
    {
        if (Instance == null)
        {
            return blockType.ToString();
        }
        var blockData = GetBlockConfiguration(blockType);
        return blockData?.displayName ?? blockType.ToString();
    }
    
    public static Material GetBlockMaterial(BlockType blockType, BlockFace face = BlockFace.Front)
    {
        if (Instance == null)
            return null;
            
        var cacheKey = blockType; // Could extend to include face if needed
        
        if (Instance.materialCache.TryGetValue(cacheKey, out Material cachedMaterial))
        {
            return cachedMaterial;
        }
        
        var blockData = GetBlockConfiguration(blockType);
        if (blockData == null)
            return null;
            
        Material material;
        
        if (blockData.customMaterial != null)
        {
            material = blockData.customMaterial;
        }
        else
        {
            Texture2D texture = blockData.mainTexture; // Use mainTexture for now, not GetTextureForFace
            if (texture == null)
                return null;
                
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")); // Use URP shader
            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);
            material.color = Color.white;
            material.SetFloat("_Smoothness", 0.0f);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Cull", 0f);
            material.name = $"{blockType}_Material";
        }
        
        Instance.materialCache[cacheKey] = material;
        return material;
    }
    
    public static float GetMiningTime(BlockType blockType)
    {
        var blockData = GetBlockConfiguration(blockType);
        if (blockData == null || blockData.isUnbreakable || !blockData.isMineable)
            return float.MaxValue;
            
        float baseTime = 0.5f;
        return baseTime * Mathf.Max(0.1f, blockData.hardness);
    }
    
    public static BlockType[] GetAllBlockTypes()
    {
        if (Instance == null)
            return new BlockType[0];
            
        return Instance.blockRegistry.Keys.ToArray();
    }
    
    void OnValidate()
    {
        if (allBlocks != null)
        {
            for (int i = 0; i < allBlocks.Length; i++)
            {
                if (allBlocks[i] != null && string.IsNullOrEmpty(allBlocks[i].displayName))
                {
                    allBlocks[i].displayName = allBlocks[i].blockType.ToString();
                }
            }
        }
        
        // Refresh hardness overrides when values change in inspector
        if (Application.isPlaying && hardnessOverrideCache != null)
        {
            RefreshHardnessOverrides();
        }
    }
    
    public void RefreshHardnessOverrides()
    {
        if (hardnessOverrideCache == null)
        {
            hardnessOverrideCache = new Dictionary<BlockType, BlockHardnessOverride>();
        }
        
        hardnessOverrideCache.Clear();
        
        if (hardnessOverrides != null)
        {
            foreach (var hardnessOverride in hardnessOverrides)
            {
                if (hardnessOverride != null)
                {
                    // Update display name if empty
                    if (string.IsNullOrEmpty(hardnessOverride.displayName))
                    {
                        hardnessOverride.displayName = hardnessOverride.blockType.ToString();
                    }
                    hardnessOverrideCache[hardnessOverride.blockType] = hardnessOverride;
                }
            }
        }
        
        Debug.Log($"BlockManager: Refreshed {hardnessOverrideCache.Count} hardness overrides");
    }
    
    // Public methods for runtime hardness modification
    public static void SetBlockHardness(BlockType blockType, float hardness)
    {
        if (Instance == null) return;
        
        if (Instance.hardnessOverrideCache.TryGetValue(blockType, out BlockHardnessOverride hardnessOverride))
        {
            hardnessOverride.hardness = hardness;
        }
        else
        {
            var newOverride = new BlockHardnessOverride(blockType, hardness);
            Instance.hardnessOverrideCache[blockType] = newOverride;
        }
        
        Debug.Log($"BlockManager: Set hardness for {blockType} to {hardness}");
    }
    
    public static void SetBlockUnbreakable(BlockType blockType, bool unbreakable)
    {
        if (Instance == null) return;
        
        if (Instance.hardnessOverrideCache.TryGetValue(blockType, out BlockHardnessOverride hardnessOverride))
        {
            hardnessOverride.isUnbreakable = unbreakable;
        }
        else
        {
            var newOverride = new BlockHardnessOverride(blockType, 1f);
            newOverride.isUnbreakable = unbreakable;
            Instance.hardnessOverrideCache[blockType] = newOverride;
        }
        
        Debug.Log($"BlockManager: Set {blockType} unbreakable to {unbreakable}");
    }
    
    // Utility method to get all current hardness values for debugging
    public static string GetHardnessDebugInfo()
    {
        if (Instance == null) return "BlockManager instance not found";
        
        var info = new System.Text.StringBuilder();
        info.AppendLine("=== Block Hardness Values ===");
        
        foreach (BlockType blockType in System.Enum.GetValues(typeof(BlockType)))
        {
            float hardness = GetHardness(blockType);
            bool unbreakable = IsUnbreakable(blockType);
            string status = unbreakable ? " (UNBREAKABLE)" : "";
            info.AppendLine($"{blockType}: {hardness:F2}{status}");
        }
        
        return info.ToString();
    }
    
    static Sprite GetFallbackSprite(BlockType blockType)
    {
        // Fallback to legacy system when BlockManager is not available
        Texture2D tex = null;
        
        // Try BlockDatabase first
        if ((int)blockType < BlockDatabase.blockTypes.Length)
        {
            tex = BlockDatabase.blockTypes[(int)blockType].blockTexture;
        }
        
        // Try WorldGenerator as backup
        if (tex == null)
        {
            var worldGenerator = Object.FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
            if (worldGenerator != null)
            {
                switch (blockType)
                {
                    case BlockType.Grass: tex = worldGenerator.grassTexture; break;
                    case BlockType.Dirt: tex = worldGenerator.dirtTexture; break;
                    case BlockType.Stone: tex = worldGenerator.stoneTexture; break;
                    case BlockType.Sand: tex = worldGenerator.sandTexture; break;
                    case BlockType.Coal: tex = worldGenerator.coalTexture; break;
                    case BlockType.Log: tex = worldGenerator.logTexture; break;
                    case BlockType.Leaves: tex = worldGenerator.leavesTexture; break;
                    case BlockType.WoodPlanks: tex = worldGenerator.woodPlanksTexture; break;
                    case BlockType.CraftingTable: tex = worldGenerator.craftingTableTexture; break;
                }
            }
        }
        
        if (tex == null) 
        {
            Debug.LogWarning($"BlockManager: No texture found for {blockType} in fallback system");
            return null;
        }
        
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
        sprite.name = $"{blockType}_Fallback";
        
        return sprite;
    }
}