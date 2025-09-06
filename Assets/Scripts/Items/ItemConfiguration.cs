using UnityEngine;

// Unified ItemConfiguration for the new Item vs Block system
[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item Configuration", order = 2)]
public class ItemConfiguration : ScriptableObject
{
    [Header("Basic Info")]
    public ItemType itemType;
    public string displayName;
    public ItemCategory category;
    
    [Header("Stacking")]
    public int maxStackSize = 64;
    
    [Header("Visual")]
    public Texture2D iconTexture;
    public Color tintColor = Color.white;
    
    [Header("Placement")]
    public bool canBePlaced = false;
    public BlockType placesAsBlock = BlockType.Air;
    
    [Header("Crafting")]
    public bool isCraftable = true;
    public bool isUsedInCrafting = true;
    
    void OnValidate()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = itemType.ToString();
        }
        
        // Ensure stick can't be placed
        if (itemType == ItemType.Stick)
        {
            canBePlaced = false;
            placesAsBlock = BlockType.Air;
        }
        
        // Crafting table can be placed
        if (itemType == ItemType.CraftingTable)
        {
            canBePlaced = true;
            placesAsBlock = BlockType.CraftingTable;
            category = ItemCategory.Placeable;
        }
    }
}