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
        count = Mathf.Clamp(amount, 0, MaxStackSize);
    }

    public bool IsEmpty => blockType == BlockType.Air || count <= 0;

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
}
