using UnityEngine;
using System;

// Simplified: removed namespace for easy direct reference
public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")] 
    public int hotbarSize = 9;
    public int mainInventorySize = 27; // 3x9 grid like Minecraft
    
    public ItemStack[] hotbar;
    public ItemStack[] mainInventory;
    public int selectedIndex = 0;

    public event Action OnInventoryChanged; // raised when contents or selection change

    void Awake()
    {
        if (hotbarSize <= 0) hotbarSize = 9;
        if (mainInventorySize <= 0) mainInventorySize = 27;
        
        hotbar = new ItemStack[hotbarSize];
        mainInventory = new ItemStack[mainInventorySize];
        
        for (int i = 0; i < hotbar.Length; i++)
        {
            hotbar[i] = new ItemStack { blockType = BlockType.Air, count = 0 };
        }
        
        for (int i = 0; i < mainInventory.Length; i++)
        {
            mainInventory[i] = new ItemStack { blockType = BlockType.Air, count = 0 };
        }
        
        // Add some test items for development
        AddBlock(BlockType.Grass, 64);
        AddBlock(BlockType.Stone, 32);
        AddBlock(BlockType.Dirt, 16);
        AddBlock(BlockType.Log, 8);
    }

    public bool AddBlock(BlockType type, int amount = 1)
    {
        Debug.Log($"PlayerInventory.AddBlock: Called with {type} x{amount}");
        
        if (type == BlockType.Air || amount <= 0) 
        {
            Debug.Log($"PlayerInventory.AddBlock: Invalid type ({type}) or amount ({amount})");
            return false;
        }
        
        int originalAmount = amount;
        
        // First pass: existing stacks of same type in hotbar
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
        
        // Second pass: existing stacks of same type in main inventory
        for (int i = 0; i < mainInventory.Length && amount > 0; i++)
        {
            if (mainInventory[i].blockType == type && mainInventory[i].count < ItemStack.MaxStackSize)
            {
                int space = ItemStack.MaxStackSize - mainInventory[i].count;
                int add = Mathf.Min(space, amount);
                mainInventory[i].count += add;
                amount -= add;
            }
        }
        
        // Third pass: empty slots in hotbar
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
        
        // Fourth pass: empty slots in main inventory
        for (int i = 0; i < mainInventory.Length && amount > 0; i++)
        {
            if (mainInventory[i].IsEmpty)
            {
                int add = Mathf.Min(ItemStack.MaxStackSize, amount);
                mainInventory[i].blockType = type;
                mainInventory[i].count = add;
                amount -= add;
            }
        }
        
        bool fullyAdded = (amount == 0);
        Debug.Log($"PlayerInventory.AddBlock: Result - added {originalAmount - amount}/{originalAmount}, fully added: {fullyAdded}");
        
        if (originalAmount != amount) OnInventoryChanged?.Invoke();
        return fullyAdded; // true if fully added
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

    public bool IsHotbarSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < hotbarSize;
    }

    public bool IsMainInventorySlot(int slotIndex)
    {
        return slotIndex >= hotbarSize && slotIndex < (hotbarSize + mainInventorySize);
    }

    public ItemStack GetSlot(int slotIndex)
    {
        if (IsHotbarSlot(slotIndex))
        {
            return hotbar[slotIndex];
        }
        else if (IsMainInventorySlot(slotIndex))
        {
            return mainInventory[slotIndex - hotbarSize];
        }
        return new ItemStack { blockType = BlockType.Air, count = 0 };
    }

    public void SetSlot(int slotIndex, ItemStack stack)
    {
        if (IsHotbarSlot(slotIndex))
        {
            hotbar[slotIndex] = stack;
        }
        else if (IsMainInventorySlot(slotIndex))
        {
            mainInventory[slotIndex - hotbarSize] = stack;
        }
        OnInventoryChanged?.Invoke();
    }

    public void SwapSlots(int slotA, int slotB)
    {
        ItemStack stackA = GetSlot(slotA);
        ItemStack stackB = GetSlot(slotB);
        SetSlot(slotA, stackB);
        SetSlot(slotB, stackA);
    }

    public bool TryMergeSlots(int fromSlot, int toSlot)
    {
        ItemStack from = GetSlot(fromSlot);
        ItemStack to = GetSlot(toSlot);

        if (from.IsEmpty) return false;

        if (to.IsEmpty)
        {
            SetSlot(toSlot, from);
            SetSlot(fromSlot, new ItemStack { blockType = BlockType.Air, count = 0 });
            return true;
        }

        if (from.blockType == to.blockType && to.count < ItemStack.MaxStackSize)
        {
            int spaceAvailable = ItemStack.MaxStackSize - to.count;
            int amountToTransfer = Mathf.Min(from.count, spaceAvailable);

            to.count += amountToTransfer;
            from.count -= amountToTransfer;

            if (from.count <= 0)
            {
                from.blockType = BlockType.Air;
                from.count = 0;
            }

            SetSlot(fromSlot, from);
            SetSlot(toSlot, to);
            return true;
        }

        return false;
    }

    public int GetTotalSlotCount()
    {
        return hotbarSize + mainInventorySize;
    }

    public ItemStack GetSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < hotbar.Length)
        {
            return hotbar[selectedIndex];
        }
        return new ItemStack { blockType = BlockType.Air, count = 0 };
    }

    public bool AddItem(BlockType blockType, int quantity = 1)
    {
        return AddBlock(blockType, quantity);
    }

    public bool RemoveItem(BlockType blockType, int quantity = 1)
    {
        if (blockType == BlockType.Air || quantity <= 0) return false;
        
        int remaining = quantity;
        
        // First check hotbar
        for (int i = 0; i < hotbar.Length && remaining > 0; i++)
        {
            if (hotbar[i].blockType == blockType)
            {
                int toRemove = Mathf.Min(hotbar[i].count, remaining);
                hotbar[i].count -= toRemove;
                remaining -= toRemove;
                
                if (hotbar[i].count <= 0)
                {
                    hotbar[i].blockType = BlockType.Air;
                    hotbar[i].count = 0;
                }
            }
        }
        
        // Then check main inventory
        for (int i = 0; i < mainInventory.Length && remaining > 0; i++)
        {
            if (mainInventory[i].blockType == blockType)
            {
                int toRemove = Mathf.Min(mainInventory[i].count, remaining);
                mainInventory[i].count -= toRemove;
                remaining -= toRemove;
                
                if (mainInventory[i].count <= 0)
                {
                    mainInventory[i].blockType = BlockType.Air;
                    mainInventory[i].count = 0;
                }
            }
        }
        
        if (remaining != quantity) OnInventoryChanged?.Invoke();
        return remaining == 0; // true if fully removed
    }
}
