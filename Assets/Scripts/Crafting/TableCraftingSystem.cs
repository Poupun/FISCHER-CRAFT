using UnityEngine;
using System.Collections.Generic;

public class TableCraftingSystem : MonoBehaviour
{
    [Header("Crafting Grid")]
    public CraftingSlot[] craftingSlots = new CraftingSlot[9]; // 3x3 grid
    public CraftingSlot resultSlot;
    
    [Header("Recipe Database")]
    public List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
    
    private PlayerInventory playerInventory;
    private ItemStack currentResult;
    
    void Start()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        
        InitializeDefaultRecipes();
        SetupCraftingSlots();
    }
    
    void InitializeDefaultRecipes()
    {
        // For now we'll use hardcoded recipes similar to CraftingManager
        // Later this can be expanded to use ScriptableObject recipes
        var logToPlanksRecipe = CraftingRecipe.CreateLogToPlanksRecipe();
        availableRecipes.Add(logToPlanksRecipe);
    }
    
    void SetupCraftingSlots()
    {
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (craftingSlots[i] != null)
            {
                int x = i % 3;
                int y = i / 3;
                craftingSlots[i].Initialize(this, x, y, false);
                SetupSlotComponents(craftingSlots[i]);
            }
        }
        
        if (resultSlot != null)
        {
            resultSlot.Initialize(this, 0, 0, true);
            SetupSlotComponents(resultSlot);
        }
    }
    
    void SetupSlotComponents(CraftingSlot slot)
    {
        // Use the same detection logic as InventoryUI - direct name-based detection
        var images = slot.GetComponentsInChildren<UnityEngine.UI.Image>();
        
        foreach (var img in images)
        {
            if (img.name.ToLower().Contains("background") || img.name.ToLower().Contains("bg"))
                slot.background = img;
            else if (img.name.ToLower().Contains("icon"))
                slot.icon = img;
        }
        
        if (slot.countText == null)
        {
            slot.countText = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }
        
        // Set the same colors as the existing system
        slot.normalColor = new UnityEngine.Color(0, 0, 0, 0.5f);
        
        if (slot.background != null)
        {
            slot.background.color = slot.normalColor;
        }
    }
    
    public void OnCraftingGridChanged()
    {
        CheckForValidRecipe();
    }
    
    void CheckForValidRecipe()
    {
        BlockType[,] currentPattern = GetCurrentCraftingPattern();
        CraftingRecipe matchedRecipe = null;
        
        // First check for 3x3 recipes
        matchedRecipe = CheckTableRecipes(currentPattern);
        
        if (matchedRecipe == null)
        {
            // Check for smaller recipes that can fit in 3x3
            foreach (var recipe in availableRecipes)
            {
                if (recipe.MatchesPattern(currentPattern))
                {
                    matchedRecipe = recipe;
                    break;
                }
            }
        }
        
        if (matchedRecipe != null)
        {
            currentResult = new ItemStack(matchedRecipe.result.blockType, matchedRecipe.result.resultCount);
            if (resultSlot != null)
            {
                resultSlot.SetStack(currentResult);
            }
        }
        else
        {
            currentResult = new ItemStack();
            if (resultSlot != null)
            {
                resultSlot.SetStack(currentResult);
            }
        }
    }
    
    CraftingRecipe CheckTableRecipes(BlockType[,] pattern)
    {
        // Add 3x3 specific recipes here
        // For example: Chest recipe (8 wood planks around edges)
        if (CheckChestRecipe(pattern))
        {
            // Create a temporary recipe result for chest
            var chestRecipe = ScriptableObject.CreateInstance<CraftingRecipe>();
            chestRecipe.result = new CraftingResult(BlockType.Log, 1); // Placeholder - should be chest when you have that block type
            return chestRecipe;
        }
        
        return null;
    }
    
    bool CheckChestRecipe(BlockType[,] pattern)
    {
        // Check for chest pattern: 8 wood planks around the edges, center empty
        // Pattern should be:
        // [WP][WP][WP]
        // [WP][  ][WP]
        // [WP][WP][WP]
        
        if (pattern[1, 1] != BlockType.Air) return false; // Center must be empty
        
        // Check all edge positions for wood planks
        BlockType[,] positions = {
            {pattern[0,0], pattern[1,0], pattern[2,0]},
            {pattern[0,1], pattern[1,1], pattern[2,1]},
            {pattern[0,2], pattern[1,2], pattern[2,2]}
        };
        
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (x == 1 && y == 1) continue; // Skip center
                if (positions[x, y] != BlockType.WoodPlanks) return false;
            }
        }
        
        return true;
    }
    
    BlockType[,] GetCurrentCraftingPattern()
    {
        BlockType[,] pattern = new BlockType[3, 3];
        
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (craftingSlots[i] != null)
            {
                int x = i % 3;
                int y = i / 3;
                var stack = craftingSlots[i].GetStack();
                pattern[x, y] = stack.IsEmpty ? BlockType.Air : stack.blockType;
            }
        }
        
        return pattern;
    }
    
    public bool TryCollectResult()
    {
        if (currentResult.IsEmpty || playerInventory == null)
            return false;
        
        if (playerInventory.AddBlock(currentResult.blockType, currentResult.count))
        {
            ConsumeCraftingMaterials();
            
            currentResult = new ItemStack();
            if (resultSlot != null)
            {
                resultSlot.SetStack(currentResult);
            }
            
            CheckForValidRecipe();
            return true;
        }
        
        return false;
    }
    
    void ConsumeCraftingMaterials()
    {
        foreach (var slot in craftingSlots)
        {
            if (slot != null)
            {
                var stack = slot.GetStack();
                if (!stack.IsEmpty)
                {
                    stack.count--;
                    if (stack.count <= 0)
                    {
                        stack = new ItemStack();
                    }
                    slot.SetStack(stack);
                }
            }
        }
    }
    
    public void ClearCraftingGrid()
    {
        if (playerInventory == null) return;
        
        foreach (var slot in craftingSlots)
        {
            if (slot != null)
            {
                var stack = slot.GetStack();
                if (!stack.IsEmpty)
                {
                    playerInventory.AddBlock(stack.blockType, stack.count);
                    slot.SetStack(new ItemStack());
                }
            }
        }
        
        CheckForValidRecipe();
    }
}