using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    [Header("Crafting Slots Configuration")]
    public int craftingStartIndex = 36; // Start index for crafting slots in inventory system (hotbar + main inventory)
    public int craftingSlotCount = 4; // 2x2 crafting grid
    public int resultSlotIndex = 40; // Index for result slot
    
    private UnifiedPlayerInventory playerInventory;
    private ItemStack currentResult;
    private bool isUpdatingRecipe = false;
    
    void Start()
    {
        // Try multiple methods to find UnifiedPlayerInventory
        playerInventory = FindFirstObjectByType<UnifiedPlayerInventory>();
        
        if (playerInventory == null)
        {
            // Try finding it on the Player GameObject specifically
            var playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerInventory = playerObj.GetComponent<UnifiedPlayerInventory>();
                Debug.Log($"CraftingManager.Start: Found UnifiedPlayerInventory on Player GameObject: {playerInventory != null}");
            }
        }
        
        if (playerInventory == null)
        {
            // Last resort - try again with include inactive
            playerInventory = FindFirstObjectByType<UnifiedPlayerInventory>(FindObjectsInactive.Include);
            Debug.Log($"CraftingManager.Start: Found UnifiedPlayerInventory (including inactive): {playerInventory != null}");
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
        
        playerInventory = FindFirstObjectByType<UnifiedPlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += CheckForValidRecipe;
            Debug.Log("CraftingManager: Successfully connected to UnifiedPlayerInventory after delay!");
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
        
        // Debug: Log the pattern
        Debug.Log($"CraftingManager: Pattern = [{currentPattern[0,0]}, {currentPattern[1,0]}], [{currentPattern[0,1]}, {currentPattern[1,1]}]");
        
        // Check for multiple recipes
        ItemStack recipeResult = CheckRecipes(currentPattern);
        
        Debug.Log($"CraftingManager.CheckForValidRecipe: Recipe result = {recipeResult.blockType}, count = {recipeResult.count}, isEmpty = {recipeResult.IsEmpty}");
        
        // Special handling: Don't treat our special markers as empty
        if (!recipeResult.IsEmpty || (recipeResult.blockType == BlockType.Air && recipeResult.count == 999))
        {
            Debug.Log($"CraftingManager.CheckForValidRecipe: Setting result slot with {recipeResult.blockType}, count {recipeResult.count}");
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
            Debug.Log("CraftingManager: Wood plank recipe detected! Returning 4 wood planks");
            return new ItemStack(BlockType.WoodPlanks, 4);
        }
        
        // Recipe 2: Stick Recipe (2 wood planks = 4 sticks, vertical arrangement)
        if (CheckStickRecipe(pattern))
        {
            Debug.Log("CraftingManager.CheckRecipes: Stick recipe matched, returning special marker");
            // Special handling for item results - return a placeholder that indicates success
            return new ItemStack(BlockType.Air, 999); // Special marker for item recipes (using 999 instead of -1)
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
                    Debug.Log($"CheckSingleItemRecipe: Failed - found {pattern[x, y]} at [{x},{y}], expected {requiredItem} or Air");
                    return false; // Other items present
                }
            }
        }
        
        Debug.Log($"CheckSingleItemRecipe: {requiredItem} count = {itemCount}, required = {requiredCount}, match = {itemCount == requiredCount}");
        return itemCount == requiredCount;
    }
    
    bool CheckStickRecipe(BlockType[,] pattern)
    {
        // Debug: Print current crafting pattern
        Debug.Log($"CraftingManager.CheckStickRecipe: Pattern = [{pattern[0,0]}, {pattern[1,0]}] / [{pattern[0,1]}, {pattern[1,1]}]");
        
        // Check for vertical arrangement: two wood planks vertically aligned
        // Pattern should be: WoodPlanks on (0,0) and (0,1) OR (1,0) and (1,1)
        
        // Check left column (x=0)
        if (pattern[0, 0] == BlockType.WoodPlanks && pattern[0, 1] == BlockType.WoodPlanks &&
            pattern[1, 0] == BlockType.Air && pattern[1, 1] == BlockType.Air)
        {
            Debug.Log("CraftingManager.CheckStickRecipe: Found stick recipe in left column!");
            return true;
        }
        
        // Check right column (x=1)  
        if (pattern[1, 0] == BlockType.WoodPlanks && pattern[1, 1] == BlockType.WoodPlanks &&
            pattern[0, 0] == BlockType.Air && pattern[0, 1] == BlockType.Air)
        {
            Debug.Log("CraftingManager.CheckStickRecipe: Found stick recipe in right column!");
            return true;
        }
        
        Debug.Log("CraftingManager.CheckStickRecipe: No stick recipe found");
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
            var entry = playerInventory.GetSlot(craftingStartIndex + craftingIndex);
            // Convert InventoryEntry to ItemStack for crafting logic
            if (entry.IsEmpty) return new ItemStack();
            if (entry.entryType == InventoryEntryType.Block)
                return new ItemStack(entry.blockType, entry.count);
            else
                return new ItemStack(); // Items in crafting grid are not supported in current recipes
        }
        return new ItemStack();
    }
    
    public void SetCraftingSlot(int craftingIndex, ItemStack stack)
    {
        if (playerInventory != null)
        {
            // Convert ItemStack to InventoryEntry
            InventoryEntry entry;
            if (stack.IsEmpty)
            {
                entry = InventoryEntry.Empty;
            }
            else
            {
                entry = new InventoryEntry
                {
                    entryType = InventoryEntryType.Block,
                    blockType = stack.blockType,
                    itemType = ItemType.Stick, // Default, not used for blocks
                    count = stack.count
                };
            }
            playerInventory.SetSlot(craftingStartIndex + craftingIndex, entry);
        }
    }
    
    public ItemStack GetResultSlot()
    {
        if (playerInventory != null)
        {
            var entry = playerInventory.GetSlot(resultSlotIndex);
            // Convert InventoryEntry to ItemStack
            if (entry.IsEmpty) return new ItemStack();
            if (entry.entryType == InventoryEntryType.Block)
                return new ItemStack(entry.blockType, entry.count);
            else
                return new ItemStack(); // Special marker for items will be handled separately
        }
        return new ItemStack();
    }
    
    public void SetResultSlot(ItemStack stack)
    {
        if (playerInventory != null)
        {
            // Convert ItemStack to InventoryEntry
            InventoryEntry entry;
            if (stack.IsEmpty)
            {
                entry = InventoryEntry.Empty;
            }
            else if (stack.blockType == BlockType.Air && stack.count == 999)
            {
                // Special marker for stick result - convert to actual stick item
                entry = new InventoryEntry
                {
                    entryType = InventoryEntryType.Item,
                    itemType = ItemType.Stick,
                    blockType = BlockType.Air,
                    count = 4 // Stick recipe gives 4 sticks
                };
            }
            else
            {
                entry = new InventoryEntry
                {
                    entryType = InventoryEntryType.Block,
                    blockType = stack.blockType,
                    itemType = ItemType.Stick, // Default, not used for blocks
                    count = stack.count
                };
            }
            playerInventory.SetSlot(resultSlotIndex, entry);
        }
    }
    
    public bool TryCollectResult()
    {
        if (currentResult.IsEmpty || playerInventory == null)
            return false;
        
        bool success = false;
        
        // Check if this is a special item recipe result (marked with count = 999)
        if (currentResult.blockType == BlockType.Air && currentResult.count == 999)
        {
            // Handle special item recipes
            success = TryCollectItemResult();
        }
        else
        {
            // Normal block result
            success = playerInventory.AddBlock(currentResult.blockType, currentResult.count);
        }
        
        if (success)
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
    
    private bool TryCollectItemResult()
    {
        // Check what item recipe this is based on the current crafting pattern
        BlockType[,] pattern = GetCurrentCraftingPattern();
        
        if (CheckStickRecipe(pattern))
        {
            // Give 4 sticks - we need to work with UnifiedPlayerInventory if available, or add to regular inventory as special case
            var unifiedInventory = playerInventory.GetComponent<UnifiedPlayerInventory>();
            if (unifiedInventory != null)
            {
                return unifiedInventory.AddItem(ItemType.Stick, 4);
            }
            else
            {
                Debug.LogWarning("CraftingManager: UnifiedPlayerInventory not found, cannot craft items. Please add UnifiedPlayerInventory component.");
                return false;
            }
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