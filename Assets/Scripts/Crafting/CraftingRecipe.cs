using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CraftingIngredient
{
    public BlockType blockType;
    public int requiredCount;

    public CraftingIngredient(BlockType type, int count)
    {
        blockType = type;
        requiredCount = count;
    }
}

[System.Serializable]
public class CraftingResult
{
    public BlockType blockType;
    public int resultCount;

    public CraftingResult(BlockType type, int count)
    {
        blockType = type;
        resultCount = count;
    }
}

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Information")]
    public string recipeName;
    public Sprite recipeIcon;
    
    [Header("Crafting Pattern")]
    [SerializeField]
    private BlockType[,] craftingPattern = new BlockType[2, 2];
    
    [Header("Result")]
    public CraftingResult result;
    
    [Header("Alternative Input Method")]
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
    public bool useIngredientsInsteadOfPattern = false;

    public BlockType GetPatternSlot(int x, int y)
    {
        if (x >= 0 && x < 2 && y >= 0 && y < 2)
            return craftingPattern[x, y];
        return BlockType.Air;
    }

    public void SetPatternSlot(int x, int y, BlockType blockType)
    {
        if (x >= 0 && x < 2 && y >= 0 && y < 2)
            craftingPattern[x, y] = blockType;
    }

    public bool MatchesPattern(BlockType[,] inputPattern)
    {
        if (useIngredientsInsteadOfPattern)
        {
            return MatchesIngredients(inputPattern);
        }

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (craftingPattern[x, y] != inputPattern[x, y])
                    return false;
            }
        }
        return true;
    }

    private bool MatchesIngredients(BlockType[,] inputPattern)
    {
        Dictionary<BlockType, int> inputCounts = new Dictionary<BlockType, int>();
        
        // Count input materials
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                BlockType type = inputPattern[x, y];
                if (type != BlockType.Air)
                {
                    if (inputCounts.ContainsKey(type))
                        inputCounts[type]++;
                    else
                        inputCounts[type] = 1;
                }
            }
        }

        // Check if required ingredients match
        foreach (var ingredient in ingredients)
        {
            if (!inputCounts.ContainsKey(ingredient.blockType) || 
                inputCounts[ingredient.blockType] < ingredient.requiredCount)
            {
                return false;
            }
        }

        // Ensure no extra materials
        foreach (var kvp in inputCounts)
        {
            bool found = false;
            foreach (var ingredient in ingredients)
            {
                if (ingredient.blockType == kvp.Key)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        return true;
    }

    public static CraftingRecipe CreateLogToPlanksRecipe()
    {
        var recipe = CreateInstance<CraftingRecipe>();
        recipe.recipeName = "Wood Planks";
        recipe.result = new CraftingResult(BlockType.WoodPlanks, 4);
        recipe.useIngredientsInsteadOfPattern = true;
        recipe.ingredients.Add(new CraftingIngredient(BlockType.Log, 1));
        return recipe;
    }
}