using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    [Header("Player Crafting Slots Configuration (2x2)")]
    public int playerCraftingStartIndex = 36; // Start index for 2x2 player crafting slots
    public int playerCraftingSlotCount = 4; // 2x2 crafting grid
    public int playerResultSlotIndex = 40; // Index for 2x2 result slot
    
    [Header("Table Crafting Slots Configuration (3x3)")]
    public int tableCraftingStartIndex = 41; // Start index for 3x3 table crafting slots
    public int tableCraftingSlotCount = 9; // 3x3 crafting grid
    public int tableResultSlotIndex = 50; // Index for 3x3 result slot
    
    [Header("Current Mode")]
    [HideInInspector]
    public bool useTableCrafting = false; // Toggle between 2x2 and 3x3 mode
    
    private UnifiedPlayerInventory playerInventory;
    private ItemStack currentResult;
    private bool isUpdatingRecipe = false;
    
    // Helper properties for current mode
    private int CurrentCraftingStartIndex => useTableCrafting ? tableCraftingStartIndex : playerCraftingStartIndex;
    private int CurrentCraftingSlotCount => useTableCrafting ? tableCraftingSlotCount : playerCraftingSlotCount;
    private int CurrentResultSlotIndex => useTableCrafting ? tableResultSlotIndex : playerResultSlotIndex;
    private int CurrentGridSize => useTableCrafting ? 3 : 2;
    
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
    
    public void SetCraftingMode(bool useTable)
    {
        useTableCrafting = useTable;
        Debug.Log($"CraftingManager: Crafting mode set to {(useTable ? "3x3 Table" : "2x2 Player")}");
        CheckForValidRecipe(); // Recalculate recipe with new mode
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
        
        // 3x3 Table-only recipes (only available when using table crafting)
        if (useTableCrafting && CurrentGridSize == 3)
        {
            // Recipe 4: Chest Recipe (8 wood planks around edges, center empty)
            if (CheckChestRecipe(pattern))
            {
                Debug.Log("CraftingManager: Chest recipe detected!");
                return new ItemStack(BlockType.Log, 1); // Placeholder - should be chest when you have that block type
            }
        }
        
        return new ItemStack(); // No valid recipe
    }
    
    bool CheckSingleItemRecipe(BlockType[,] pattern, BlockType requiredItem, int requiredCount)
    {
        int itemCount = 0;
        int gridSize = CurrentGridSize;
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
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
        int gridSize = CurrentGridSize;
        
        // Debug: Print current crafting pattern
        if (gridSize == 2)
        {
            Debug.Log($"CraftingManager.CheckStickRecipe: 2x2 Pattern = [{pattern[0,0]}, {pattern[1,0]}] / [{pattern[0,1]}, {pattern[1,1]}]");
        }
        else
        {
            Debug.Log($"CraftingManager.CheckStickRecipe: 3x3 Pattern = [{pattern[0,0]}, {pattern[1,0]}, {pattern[2,0]}] / [{pattern[0,1]}, {pattern[1,1]}, {pattern[2,1]}] / [{pattern[0,2]}, {pattern[1,2]}, {pattern[2,2]}]");
        }
        
        // Check for vertical arrangement: two wood planks vertically aligned
        // For 3x3 grid, check all possible columns
        for (int col = 0; col < gridSize; col++)
        {
            // Check if this column has the stick pattern and all other slots are empty
            bool hasStickPattern = false;
            
            // Check for vertical stick pattern in this column
            for (int startRow = 0; startRow <= gridSize - 2; startRow++)
            {
                if (pattern[col, startRow] == BlockType.WoodPlanks && 
                    pattern[col, startRow + 1] == BlockType.WoodPlanks)
                {
                    // Found potential stick pattern, now check if all other slots are empty
                    bool allOtherSlotsEmpty = true;
                    
                    for (int x = 0; x < gridSize; x++)
                    {
                        for (int y = 0; y < gridSize; y++)
                        {
                            // Skip the two slots that should have wood planks
                            if (x == col && (y == startRow || y == startRow + 1))
                                continue;
                                
                            if (pattern[x, y] != BlockType.Air)
                            {
                                allOtherSlotsEmpty = false;
                                break;
                            }
                        }
                        if (!allOtherSlotsEmpty) break;
                    }
                    
                    if (allOtherSlotsEmpty)
                    {
                        Debug.Log($"CraftingManager.CheckStickRecipe: Found stick recipe in column {col}, rows {startRow}-{startRow + 1}!");
                        return true;
                    }
                }
            }
        }
        
        Debug.Log("CraftingManager.CheckStickRecipe: No stick recipe found");
        return false;
    }
    
    bool CheckCraftingTableRecipe(BlockType[,] pattern)
    {
        int gridSize = CurrentGridSize;
        
        // For 2x2 grid, check the entire grid
        if (gridSize == 2)
        {
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
        
        // For 3x3 grid, check all possible 2x2 positions
        for (int startX = 0; startX <= gridSize - 2; startX++)
        {
            for (int startY = 0; startY <= gridSize - 2; startY++)
            {
                // Check if this 2x2 area has all wood planks and all other slots are empty
                bool has2x2WoodPlanks = true;
                bool allOtherSlotsEmpty = true;
                
                // Check the 2x2 area
                for (int x = startX; x < startX + 2; x++)
                {
                    for (int y = startY; y < startY + 2; y++)
                    {
                        if (pattern[x, y] != BlockType.WoodPlanks)
                        {
                            has2x2WoodPlanks = false;
                            break;
                        }
                    }
                    if (!has2x2WoodPlanks) break;
                }
                
                if (has2x2WoodPlanks)
                {
                    // Check all other slots are empty
                    for (int x = 0; x < gridSize; x++)
                    {
                        for (int y = 0; y < gridSize; y++)
                        {
                            // Skip the 2x2 area we just checked
                            if (x >= startX && x < startX + 2 && y >= startY && y < startY + 2)
                                continue;
                                
                            if (pattern[x, y] != BlockType.Air)
                            {
                                allOtherSlotsEmpty = false;
                                break;
                            }
                        }
                        if (!allOtherSlotsEmpty) break;
                    }
                    
                    if (allOtherSlotsEmpty)
                    {
                        Debug.Log($"CraftingManager.CheckCraftingTableRecipe: Found crafting table recipe at position ({startX},{startY})!");
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    bool CheckChestRecipe(BlockType[,] pattern)
    {
        // Only works in 3x3 crafting
        if (CurrentGridSize != 3) return false;
        
        // Check for chest pattern: 8 wood planks around the edges, center empty
        // Pattern should be:
        // [WP][WP][WP]
        // [WP][  ][WP]
        // [WP][WP][WP]
        
        if (pattern[1, 1] != BlockType.Air) return false; // Center must be empty
        
        // Check all edge positions for wood planks
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (x == 1 && y == 1) continue; // Skip center
                if (pattern[x, y] != BlockType.WoodPlanks) return false;
            }
        }
        
        Debug.Log("CraftingManager: Chest recipe pattern matched!");
        return true;
    }
    
    BlockType[,] GetCurrentCraftingPattern()
    {
        int gridSize = CurrentGridSize;
        BlockType[,] pattern = new BlockType[gridSize, gridSize];
        
        for (int i = 0; i < CurrentCraftingSlotCount; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;
            var stack = GetCraftingSlot(i);
            pattern[x, y] = stack.IsEmpty ? BlockType.Air : stack.blockType;
        }
        
        return pattern;
    }
    
    public ItemStack GetCraftingSlot(int craftingIndex)
    {
        if (playerInventory != null)
        {
            var entry = playerInventory.GetSlot(CurrentCraftingStartIndex + craftingIndex);
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
            playerInventory.SetSlot(CurrentCraftingStartIndex + craftingIndex, entry);
        }
    }
    
    public ItemStack GetResultSlot()
    {
        if (playerInventory != null)
        {
            var entry = playerInventory.GetSlot(CurrentResultSlotIndex);
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
            playerInventory.SetSlot(CurrentResultSlotIndex, entry);
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
        for (int i = 0; i < CurrentCraftingSlotCount; i++)
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
        
        for (int i = 0; i < CurrentCraftingSlotCount; i++)
        {
            // Get the actual inventory entry, not just the ItemStack
            var entry = playerInventory.GetSlot(CurrentCraftingStartIndex + i);
            if (!entry.IsEmpty)
            {
                // Add back to inventory based on entry type
                if (entry.entryType == InventoryEntryType.Block)
                {
                    playerInventory.AddBlock(entry.blockType, entry.count);
                }
                else
                {
                    playerInventory.AddItem(entry.itemType, entry.count);
                }
                
                // Clear the crafting slot
                playerInventory.SetSlot(CurrentCraftingStartIndex + i, InventoryEntry.Empty);
            }
        }
        
        // Also clear result slot if it has anything
        var resultEntry = playerInventory.GetSlot(CurrentResultSlotIndex);
        if (!resultEntry.IsEmpty)
        {
            if (resultEntry.entryType == InventoryEntryType.Block)
            {
                playerInventory.AddBlock(resultEntry.blockType, resultEntry.count);
            }
            else
            {
                playerInventory.AddItem(resultEntry.itemType, resultEntry.count);
            }
            playerInventory.SetSlot(CurrentResultSlotIndex, InventoryEntry.Empty);
        }
        
        CheckForValidRecipe();
    }
}