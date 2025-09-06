using UnityEngine;
using System.Collections.Generic;

public class CraftingSystem : MonoBehaviour
{
    [Header("Crafting Grid")]
    public CraftingSlot[] craftingSlots = new CraftingSlot[4];
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
        var logToPlanksRecipe = CraftingRecipe.CreateLogToPlanksRecipe();
        availableRecipes.Add(logToPlanksRecipe);
    }
    
    void SetupCraftingSlots()
    {
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (craftingSlots[i] != null)
            {
                int x = i % 2;
                int y = i / 2;
                craftingSlots[i].Initialize(this, x, y, false);
            }
        }
        
        if (resultSlot != null)
        {
            resultSlot.Initialize(this, 0, 0, true);
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
        
        foreach (var recipe in availableRecipes)
        {
            if (recipe.MatchesPattern(currentPattern))
            {
                matchedRecipe = recipe;
                break;
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
    
    BlockType[,] GetCurrentCraftingPattern()
    {
        BlockType[,] pattern = new BlockType[2, 2];
        
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (craftingSlots[i] != null)
            {
                int x = i % 2;
                int y = i / 2;
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