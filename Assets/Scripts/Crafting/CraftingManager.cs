using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    [Header("Crafting Slots Configuration")]
    public int craftingStartIndex = 36; // Start index for crafting slots in inventory system (hotbar + main inventory)
    public int craftingSlotCount = 4; // 2x2 crafting grid
    public int resultSlotIndex = 40; // Index for result slot
    
    private PlayerInventory playerInventory;
    private ItemStack currentResult;
    private bool isUpdatingRecipe = false;
    
    void Start()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        InitializeDefaultRecipes();
        
        // Subscribe to inventory changes to check for valid recipes
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += CheckForValidRecipe;
        }
    }
    
    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= CheckForValidRecipe;
        }
    }
    
    void InitializeDefaultRecipes()
    {
        // For now, we'll use a simple hardcoded check instead of ScriptableObject recipes
        // This avoids compilation issues while we test the basic functionality
    }
    
    void CheckForValidRecipe()
    {
        if (isUpdatingRecipe) return; // Prevent circular calls
        isUpdatingRecipe = true;
        
        BlockType[,] currentPattern = GetCurrentCraftingPattern();
        
        // Simple hardcoded recipe check: 1 log = 4 wood planks
        bool hasLog = false;
        int logCount = 0;
        
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (currentPattern[x, y] == BlockType.Log)
                {
                    hasLog = true;
                    logCount++;
                }
                else if (currentPattern[x, y] != BlockType.Air)
                {
                    // If there's any other material, recipe doesn't match
                    hasLog = false;
                    break;
                }
            }
            if (!hasLog && logCount > 0) break; // Early exit if pattern doesn't match
        }
        
        if (hasLog && logCount == 1)
        {
            currentResult = new ItemStack(BlockType.WoodPlanks, 4);
            SetResultSlot(currentResult);
        }
        else
        {
            currentResult = new ItemStack();
            SetResultSlot(currentResult);
        }
        
        isUpdatingRecipe = false; // Reset flag
    }
    
    BlockType[,] GetCurrentCraftingPattern()
    {
        BlockType[,] pattern = new BlockType[2, 2];
        
        for (int i = 0; i < craftingSlotCount; i++)
        {
            int x = i % 2;
            int y = i / 2;
            var stack = GetCraftingSlot(i);
            pattern[x, y] = stack.IsEmpty ? BlockType.Air : stack.blockType;
        }
        
        return pattern;
    }
    
    public ItemStack GetCraftingSlot(int craftingIndex)
    {
        if (playerInventory != null)
        {
            return playerInventory.GetSlot(craftingStartIndex + craftingIndex);
        }
        return new ItemStack();
    }
    
    public void SetCraftingSlot(int craftingIndex, ItemStack stack)
    {
        if (playerInventory != null)
        {
            playerInventory.SetSlot(craftingStartIndex + craftingIndex, stack);
        }
    }
    
    public ItemStack GetResultSlot()
    {
        if (playerInventory != null)
        {
            return playerInventory.GetSlot(resultSlotIndex);
        }
        return new ItemStack();
    }
    
    public void SetResultSlot(ItemStack stack)
    {
        if (playerInventory != null)
        {
            playerInventory.SetSlot(resultSlotIndex, stack);
        }
    }
    
    public bool TryCollectResult()
    {
        if (currentResult.IsEmpty || playerInventory == null)
            return false;
        
        // Try to add the result to the main inventory
        if (playerInventory.AddBlock(currentResult.blockType, currentResult.count))
        {
            ConsumeCraftingMaterials();
            
            // Clear result and recalculate
            currentResult = new ItemStack();
            SetResultSlot(currentResult);
            CheckForValidRecipe();
            return true;
        }
        
        return false;
    }
    
    void ConsumeCraftingMaterials()
    {
        for (int i = 0; i < craftingSlotCount; i++)
        {
            var stack = GetCraftingSlot(i);
            if (!stack.IsEmpty)
            {
                stack.count--;
                if (stack.count <= 0)
                {
                    stack = new ItemStack(); // Empty stack
                }
                SetCraftingSlot(i, stack); // Update the actual slot
            }
        }
    }
    
    public void ClearCraftingGrid()
    {
        if (playerInventory == null) return;
        
        for (int i = 0; i < craftingSlotCount; i++)
        {
            var stack = GetCraftingSlot(i);
            if (!stack.IsEmpty)
            {
                playerInventory.AddBlock(stack.blockType, stack.count);
                SetCraftingSlot(i, new ItemStack());
            }
        }
        
        CheckForValidRecipe();
    }
}