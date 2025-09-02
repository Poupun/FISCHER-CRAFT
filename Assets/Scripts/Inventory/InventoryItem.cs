using UnityEngine;

[System.Serializable]
public struct ItemStack
{
    public BlockType blockType;
    public int count;
    public const int MaxStackSize = 64; // Minecraft-like stack size

    public ItemStack(BlockType type, int amount)
    {
        blockType = type;
        // Allow special markers (like crafting result indicators) to bypass stack size limit
        if (type == BlockType.Air && amount == 999)
        {
            count = amount; // Don't clamp special markers
        }
        else
        {
            count = Mathf.Clamp(amount, 0, MaxStackSize);
        }
    }

    public bool IsEmpty => (blockType == BlockType.Air && count != 999) || count <= 0;

    public int Add(int amount)
    {
        int space = MaxStackSize - count;
        int toAdd = Mathf.Min(space, amount);
        count += toAdd;
        return amount - toAdd; // remainder
    }

    public int Remove(int amount)
    {
        int removed = Mathf.Min(count, amount);
        count -= removed;
        if (count <= 0)
        {
            blockType = BlockType.Air;
            count = 0;
        }
        return removed;
    }
    
    // Conversion methods for migration
    public static implicit operator InventoryEntry(ItemStack itemStack)
    {
        if (itemStack.IsEmpty)
            return InventoryEntry.Empty;
            
        return InventoryEntry.CreateBlock(itemStack.blockType, itemStack.count);
    }
    
    public static explicit operator ItemStack(InventoryEntry entry)
    {
        if (entry.entryType == InventoryEntryType.Block)
        {
            return new ItemStack(entry.blockType, entry.count);
        }
        else
        {
            // For migration: if item can be placed as block, convert
            if (ItemManager.CanBePlaced(entry.itemType))
            {
                BlockType placementBlock = ItemManager.GetPlacementBlock(entry.itemType);
                return new ItemStack(placementBlock, entry.count);
            }
            else
            {
                // Cannot convert item to ItemStack, return empty
                return new ItemStack(BlockType.Air, 0);
            }
        }
    }
}
