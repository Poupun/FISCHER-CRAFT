using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }
    
    [Header("Block Configuration")]
    [SerializeField] private BlockConfiguration[] allBlocks;
    [Header("Auto-Load Settings")]
    [SerializeField] private bool autoLoadBlockAssets = true;
    [SerializeField] private string blockAssetsPath = "Assets/Data/Blocks";
    
    private Dictionary<BlockType, BlockConfiguration> blockRegistry;
    private Dictionary<BlockType, Sprite> spriteCache;
    private Dictionary<BlockType, Material> materialCache;
    
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
    
    public static float GetHardness(BlockType blockType)
    {
        if (Instance == null)
        {
            // Fallback to legacy system or default values
            return BlockHardnessSystem.GetHardnessFromLegacySystem(blockType);
        }
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