using UnityEngine;

[System.Serializable]
public enum InventoryEntryType
{
    Block,
    Item
}

[System.Serializable]
public struct InventoryEntry
{
    public InventoryEntryType entryType;
    public BlockType blockType;
    public ItemType itemType;
    public int count;

    // Static constructors for easy creation
    public static InventoryEntry CreateBlock(BlockType blockType, int count = 1)
    {
        return new InventoryEntry
        {
            entryType = InventoryEntryType.Block,
            blockType = blockType,
            itemType = default,
            count = count
        };
    }
    
    public static InventoryEntry CreateItem(ItemType itemType, int count = 1)
    {
        return new InventoryEntry
        {
            entryType = InventoryEntryType.Item,
            blockType = default,
            itemType = itemType,
            count = count
        };
    }
    
    public static InventoryEntry Empty => new InventoryEntry
    {
        entryType = InventoryEntryType.Block,
        blockType = BlockType.Air,
        itemType = default,
        count = 0
    };
    
    public bool IsEmpty => (entryType == InventoryEntryType.Block && blockType == BlockType.Air) || count <= 0;
    
    public int MaxStackSize
    {
        get
        {
            if (entryType == InventoryEntryType.Item)
                return ItemManager.GetMaxStackSize(itemType);
            else
                return 64; // Default for blocks
        }
    }
    
    public string DisplayName
    {
        get
        {
            if (entryType == InventoryEntryType.Item)
                return ItemManager.GetDisplayName(itemType);
            else
                return BlockManager.GetDisplayName(blockType);
        }
    }
    
    public Sprite GetSprite()
    {
        if (entryType == InventoryEntryType.Item)
            return ItemManager.GetItemSprite(itemType);
        else
            return BlockManager.GetBlockSprite(blockType);
    }
    
    public bool CanStackWith(InventoryEntry other)
    {
        if (IsEmpty || other.IsEmpty) return true;
        if (entryType != other.entryType) return false;
        
        if (entryType == InventoryEntryType.Block)
            return blockType == other.blockType;
        else
            return itemType == other.itemType;
    }
    
    public bool CanBePlaced()
    {
        if (entryType == InventoryEntryType.Block)
            return BlockManager.IsPlaceable(blockType);
        else
            return ItemManager.CanBePlaced(itemType);
    }
    
    public BlockType GetPlacementBlock()
    {
        if (entryType == InventoryEntryType.Block)
            return blockType;
        else
            return ItemManager.GetPlacementBlock(itemType);
    }
}