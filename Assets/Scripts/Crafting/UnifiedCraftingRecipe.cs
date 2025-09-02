using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CraftingIngredientEntry
{
    public InventoryEntryType entryType;
    public BlockType blockType;
    public ItemType itemType;
    public int requiredCount;

    public static CraftingIngredientEntry CreateBlock(BlockType blockType, int count)
    {
        return new CraftingIngredientEntry
        {
            entryType = InventoryEntryType.Block,
            blockType = blockType,
            requiredCount = count
        };
    }
    
    public static CraftingIngredientEntry CreateItem(ItemType itemType, int count)
    {
        return new CraftingIngredientEntry
        {
            entryType = InventoryEntryType.Item,
            itemType = itemType,
            requiredCount = count
        };
    }
    
    public bool Matches(InventoryEntry entry)
    {
        if (entry.entryType != entryType) return false;
        
        if (entryType == InventoryEntryType.Block)
            return blockType == entry.blockType;
        else
            return itemType == entry.itemType;
    }
    
    public string DisplayName
    {
        get
        {
            if (entryType == InventoryEntryType.Block)
                return BlockManager.GetDisplayName(blockType);
            else
                return ItemManager.GetDisplayName(itemType);
        }
    }
}

[System.Serializable]
public class CraftingResultEntry
{
    public InventoryEntryType entryType;
    public BlockType blockType;
    public ItemType itemType;
    public int resultCount;

    public static CraftingResultEntry CreateBlock(BlockType blockType, int count)
    {
        return new CraftingResultEntry
        {
            entryType = InventoryEntryType.Block,
            blockType = blockType,
            resultCount = count
        };
    }
    
    public static CraftingResultEntry CreateItem(ItemType itemType, int count)
    {
        return new CraftingResultEntry
        {
            entryType = InventoryEntryType.Item,
            itemType = itemType,
            resultCount = count
        };
    }
    
    public InventoryEntry ToInventoryEntry()
    {
        if (entryType == InventoryEntryType.Block)
            return InventoryEntry.CreateBlock(blockType, resultCount);
        else
            return InventoryEntry.CreateItem(itemType, resultCount);
    }
}

[CreateAssetMenu(fileName = "New Unified Recipe", menuName = "Crafting/Unified Recipe")]
public class UnifiedCraftingRecipe : ScriptableObject
{
    [Header("Recipe Information")]
    public string recipeName;
    public Sprite recipeIcon;
    
    [Header("Result")]
    public CraftingResultEntry result;
    
    [Header("Ingredients")]
    public List<CraftingIngredientEntry> ingredients = new List<CraftingIngredientEntry>();

    public bool CanCraft(InventoryEntry[,] inputPattern)
    {
        Dictionary<string, int> inputCounts = new Dictionary<string, int>();
        
        // Count input materials
        for (int x = 0; x < inputPattern.GetLength(0); x++)
        {
            for (int y = 0; y < inputPattern.GetLength(1); y++)
            {
                InventoryEntry entry = inputPattern[x, y];
                if (!entry.IsEmpty)
                {
                    string key = GetEntryKey(entry);
                    if (inputCounts.ContainsKey(key))
                        inputCounts[key] += entry.count;
                    else
                        inputCounts[key] = entry.count;
                }
            }
        }

        // Check if required ingredients match
        foreach (var ingredient in ingredients)
        {
            string key = GetIngredientKey(ingredient);
            if (!inputCounts.ContainsKey(key) || 
                inputCounts[key] < ingredient.requiredCount)
            {
                return false;
            }
        }

        // Ensure no extra materials (strict matching)
        foreach (var kvp in inputCounts)
        {
            bool found = false;
            foreach (var ingredient in ingredients)
            {
                if (GetIngredientKey(ingredient) == kvp.Key)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        return true;
    }
    
    private string GetEntryKey(InventoryEntry entry)
    {
        if (entry.entryType == InventoryEntryType.Block)
            return $"Block_{entry.blockType}";
        else
            return $"Item_{entry.itemType}";
    }
    
    private string GetIngredientKey(CraftingIngredientEntry ingredient)
    {
        if (ingredient.entryType == InventoryEntryType.Block)
            return $"Block_{ingredient.blockType}";
        else
            return $"Item_{ingredient.itemType}";
    }
    
    public static UnifiedCraftingRecipe CreateLogToPlanksRecipe()
    {
        var recipe = CreateInstance<UnifiedCraftingRecipe>();
        recipe.recipeName = "Wood Planks";
        recipe.result = CraftingResultEntry.CreateBlock(BlockType.WoodPlanks, 4);
        recipe.ingredients.Add(CraftingIngredientEntry.CreateBlock(BlockType.Log, 1));
        return recipe;
    }
    
    public static UnifiedCraftingRecipe CreateSticksRecipe()
    {
        var recipe = CreateInstance<UnifiedCraftingRecipe>();
        recipe.recipeName = "Sticks";
        recipe.result = CraftingResultEntry.CreateItem(ItemType.Stick, 4);
        recipe.ingredients.Add(CraftingIngredientEntry.CreateBlock(BlockType.WoodPlanks, 2));
        return recipe;
    }
    
    public static UnifiedCraftingRecipe CreateCraftingTableRecipe()
    {
        var recipe = CreateInstance<UnifiedCraftingRecipe>();
        recipe.recipeName = "Crafting Table";
        recipe.result = CraftingResultEntry.CreateItem(ItemType.CraftingTable, 1);
        recipe.ingredients.Add(CraftingIngredientEntry.CreateBlock(BlockType.WoodPlanks, 4));
        return recipe;
    }
}