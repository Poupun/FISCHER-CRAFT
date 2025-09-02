using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    
    [Header("Item Configuration")]
    [SerializeField] private ItemConfiguration[] allItems;
    [Header("Auto-Load Settings")]
    [SerializeField] private bool autoLoadItemAssets = true;
    [SerializeField] private string itemAssetsPath = "Assets/Data/Items";
    
    private Dictionary<ItemType, ItemConfiguration> itemRegistry;
    private Dictionary<ItemType, Sprite> spriteCache;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
        
        InitializeItemRegistry();
    }
    
    void InitializeItemRegistry()
    {
        itemRegistry = new Dictionary<ItemType, ItemConfiguration>();
        spriteCache = new Dictionary<ItemType, Sprite>();
        
        if (autoLoadItemAssets && (allItems == null || allItems.Length == 0 || System.Array.Exists(allItems, x => x == null)))
        {
            LoadItemAssetsFromPath();
        }
        
        if (allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item != null)
                {
                    itemRegistry[item.itemType] = item;
                    Debug.Log($"ItemManager: Registered {item.itemType} - {item.displayName}");
                }
            }
        }
        
        Debug.Log($"ItemManager: Initialized with {itemRegistry.Count} items");
    }
    
    void LoadItemAssetsFromPath()
    {
#if UNITY_EDITOR
        string[] assetGUIDs = UnityEditor.AssetDatabase.FindAssets("t:ItemConfiguration", new[] { itemAssetsPath });
        var itemList = new List<ItemConfiguration>();
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var itemConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemConfiguration>(assetPath);
            if (itemConfig != null)
            {
                itemList.Add(itemConfig);
            }
        }
        
        allItems = itemList.ToArray();
        Debug.Log($"ItemManager: Auto-loaded {allItems.Length} item configurations from {itemAssetsPath}");
#else
        var itemConfigs = Resources.LoadAll<ItemConfiguration>("Items");
        if (itemConfigs.Length > 0)
        {
            allItems = itemConfigs;
        }
#endif
    }
    
    public static ItemConfiguration GetItemConfiguration(ItemType itemType)
    {
        if (Instance == null)
            return null;
            
        Instance.itemRegistry.TryGetValue(itemType, out ItemConfiguration data);
        return data;
    }
    
    public static Sprite GetItemSprite(ItemType itemType)
    {
        if (Instance == null)
            return null;
            
        if (Instance.spriteCache.TryGetValue(itemType, out Sprite cachedSprite))
        {
            return cachedSprite;
        }
        
        var itemData = GetItemConfiguration(itemType);
        if (itemData?.iconTexture == null)
            return null;
            
        Sprite sprite = Sprite.Create(
            itemData.iconTexture,
            new Rect(0, 0, itemData.iconTexture.width, itemData.iconTexture.height),
            new Vector2(0.5f, 0.5f),
            32f
        );
        sprite.name = $"{itemType}_Icon";
        
        Instance.spriteCache[itemType] = sprite;
        return sprite;
    }
    
    public static string GetDisplayName(ItemType itemType)
    {
        var itemData = GetItemConfiguration(itemType);
        return itemData?.displayName ?? itemType.ToString();
    }
    
    public static int GetMaxStackSize(ItemType itemType)
    {
        var itemData = GetItemConfiguration(itemType);
        return itemData?.maxStackSize ?? 64;
    }
    
    public static bool CanBePlaced(ItemType itemType)
    {
        var itemData = GetItemConfiguration(itemType);
        return itemData?.canBePlaced ?? false;
    }
    
    public static BlockType GetPlacementBlock(ItemType itemType)
    {
        var itemData = GetItemConfiguration(itemType);
        return itemData?.placesAsBlock ?? BlockType.Air;
    }
    
    public static ItemType[] GetAllItemTypes()
    {
        if (Instance == null)
            return new ItemType[0];
            
        return Instance.itemRegistry.Keys.ToArray();
    }
}