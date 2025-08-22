using UnityEngine;
using System;

// Simplified: removed namespace for easy direct reference
public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")] public int hotbarSize = 9;
    public ItemStack[] hotbar;
    public int selectedIndex = 0;

    public event Action OnInventoryChanged; // raised when contents or selection change

    void Awake()
    {
        if (hotbarSize <= 0) hotbarSize = 9;
        hotbar = new ItemStack[hotbarSize];
        for (int i = 0; i < hotbar.Length; i++)
        {
            hotbar[i] = new ItemStack { blockType = BlockType.Air, count = 0 };
        }
    }

    public bool AddBlock(BlockType type, int amount = 1)
    {
        if (type == BlockType.Air || amount <= 0) return false;
        int originalAmount = amount;
        // First pass: existing stacks of same type
        for (int i = 0; i < hotbar.Length && amount > 0; i++)
        {
            if (hotbar[i].blockType == type && hotbar[i].count < ItemStack.MaxStackSize)
            {
                int space = ItemStack.MaxStackSize - hotbar[i].count;
                int add = Mathf.Min(space, amount);
                hotbar[i].count += add;
                amount -= add;
            }
        }
        // Second pass: empty slot
        for (int i = 0; i < hotbar.Length && amount > 0; i++)
        {
            if (hotbar[i].IsEmpty)
            {
                int add = Mathf.Min(ItemStack.MaxStackSize, amount);
                hotbar[i].blockType = type;
                hotbar[i].count = add;
                amount -= add;
            }
        }
        if (originalAmount != amount) OnInventoryChanged?.Invoke();
        return amount == 0; // true if fully added
    }

    public bool HasBlockForPlacement(out BlockType type)
    {
        var slot = hotbar[selectedIndex];
        if (!slot.IsEmpty)
        {
            type = slot.blockType;
            return true;
        }
        type = BlockType.Air;
        return false;
    }

    public bool ConsumeOneFromSelected()
    {
        if (hotbar[selectedIndex].IsEmpty) return false;
        hotbar[selectedIndex].count--;
        if (hotbar[selectedIndex].count <= 0)
        {
            hotbar[selectedIndex].blockType = BlockType.Air;
            hotbar[selectedIndex].count = 0;
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void SetSelectedIndex(int index)
    {
        if (index < 0 || index >= hotbar.Length) return;
        if (selectedIndex == index) return;
        selectedIndex = index;
        OnInventoryChanged?.Invoke();
    }

    public void Cycle(int delta)
    {
        if (hotbar.Length == 0) return;
        selectedIndex = (selectedIndex + delta + hotbar.Length) % hotbar.Length;
        OnInventoryChanged?.Invoke();
    }
}
