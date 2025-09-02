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
        // Try multiple methods to find PlayerInventory
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        
        if (playerInventory == null)
        {
            // Try finding it on the Player GameObject specifically
            var playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerInventory = playerObj.GetComponent<PlayerInventory>();
                Debug.Log($"CraftingManager.Start: Found PlayerInventory on Player GameObject: {playerInventory != null}");
            }
        }
        
        if (playerInventory == null)
        {
            // Last resort - try again with include inactive
            playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            Debug.Log($"CraftingManager.Start: Found PlayerInventory (including inactive): {playerInventory != null}");
        }
        
        InitializeDefaultRecipes();
        
        Debug.Log($"CraftingManager.Start: Final PlayerInventory result: {playerInventory != null}");
        
        // Subscribe to inventory changes to check for valid recipes
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += CheckForValidRecipe;
            Debug.Log("CraftingManager.Start: Successfully subscribed to OnInventoryChanged event");
            
            // Force an initial recipe check
            CheckForValidRecipe();
        }
        else
        {
            Debug.LogWarning("CraftingManager.Start: PlayerInventory not found after all attempts!");
            // Try to connect later
            StartCoroutine(TryConnectLater());
        }
    }
    
    System.Collections.IEnumerator TryConnectLater()
    {
        Debug.Log("CraftingManager: Trying to connect to PlayerInventory later...");
        yield return new WaitForSeconds(1f);
        
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += CheckForValidRecipe;
            Debug.Log("CraftingManager: Successfully connected to PlayerInventory after delay!");
            CheckForValidRecipe();
        }
        else
        {
            Debug.LogError("CraftingManager: Still couldn't find PlayerInventory after delay!");
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
    
    public void CheckForValidRecipe()
    {
        if (isUpdatingRecipe) return; // Prevent circular calls
        isUpdatingRecipe = true;
        
        BlockType[,] currentPattern = GetCurrentCraftingPattern();
        
        
        // Check for multiple recipes
        ItemStack recipeResult = CheckRecipes(currentPattern);
        
        if (!recipeResult.IsEmpty)
        {
            currentResult = recipeResult;
            SetResultSlot(currentResult);
        }
        else
        {
            currentResult = new ItemStack();
            SetResultSlot(currentResult);
        }
        
        isUpdatingRecipe = false; // Reset flag
    }
    
    ItemStack CheckRecipes(BlockType[,] pattern)
    {
        // Recipe 1: Log -> Wood Planks (1 log = 4 wood planks)
        if (CheckSingleItemRecipe(pattern, BlockType.Log, 1))
        {
            return new ItemStack(BlockType.WoodPlanks, 4);
        }
        
        // Recipe 2: Stick Recipe (2 wood planks = 4 sticks, vertical arrangement)
        // Check for vertical stick pattern: wood planks on top of each other
        if (CheckStickRecipe(pattern))
        {
            return new ItemStack(BlockType.Stick, 4);
        }
        
        // Recipe 3: Crafting Table (4 wood planks = 1 crafting table, 2x2 square)
        if (CheckCraftingTableRecipe(pattern))
        {
            return new ItemStack(BlockType.CraftingTable, 1);
        }
        
        return new ItemStack(); // No valid recipe
    }
    
    bool CheckSingleItemRecipe(BlockType[,] pattern, BlockType requiredItem, int requiredCount)
    {
        int itemCount = 0;
        
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (pattern[x, y] == requiredItem)
                {
                    itemCount++;
                }
                else if (pattern[x, y] != BlockType.Air)
                {
                    return false; // Other items present
                }
            }
        }
        
        return itemCount == requiredCount;
    }
    
    bool CheckStickRecipe(BlockType[,] pattern)
    {
        // Check for vertical arrangement: two wood planks vertically aligned
        // Pattern should be: WoodPlanks on (0,0) and (0,1) OR (1,0) and (1,1)
        
        // Check left column (x=0)
        if (pattern[0, 0] == BlockType.WoodPlanks && pattern[0, 1] == BlockType.WoodPlanks &&
            pattern[1, 0] == BlockType.Air && pattern[1, 1] == BlockType.Air)
        {
            return true;
        }
        
        // Check right column (x=1)  
        if (pattern[1, 0] == BlockType.WoodPlanks && pattern[1, 1] == BlockType.WoodPlanks &&
            pattern[0, 0] == BlockType.Air && pattern[0, 1] == BlockType.Air)
        {
            return true;
        }
        
        return false;
    }
    
    bool CheckCraftingTableRecipe(BlockType[,] pattern)
    {
        // Check for 2x2 square of wood planks
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (pattern[x, y] != BlockType.WoodPlanks)
                {
                    return false;
                }
            }
        }
        return true;
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
    
    public void ConsumeCraftingMaterials()
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