using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BlockHardnessData
{
    public BlockType blockType;
    public float hardness;
    public bool isUnbreakable;
    
    public BlockHardnessData(BlockType type, float hardnessValue, bool unbreakable = false)
    {
        blockType = type;
        hardness = hardnessValue;
        isUnbreakable = unbreakable;
    }
}

public static class BlockHardnessSystem
{
    private static Dictionary<BlockType, BlockHardnessData> hardnessData;
    
    static BlockHardnessSystem()
    {
        InitializeHardnessData();
    }
    
    private static void InitializeHardnessData()
    {
        hardnessData = new Dictionary<BlockType, BlockHardnessData>();
        
        // Define hardness values (lower = easier to break, like Minecraft)
        var hardnessValues = new BlockHardnessData[]
        {
            new BlockHardnessData(BlockType.Air, 0f),
            new BlockHardnessData(BlockType.Grass, 0.3f),
            new BlockHardnessData(BlockType.Dirt, 0.25f),
            new BlockHardnessData(BlockType.Sand, 0.25f),
            new BlockHardnessData(BlockType.Gravel, 0.3f),
            new BlockHardnessData(BlockType.Log, 1.0f),
            new BlockHardnessData(BlockType.Leaves, 0.1f),
            new BlockHardnessData(BlockType.Stone, 0.75f),
            new BlockHardnessData(BlockType.Coal, 1.5f),
            new BlockHardnessData(BlockType.Iron, 1.5f),
            new BlockHardnessData(BlockType.Gold, 1.5f),
            new BlockHardnessData(BlockType.Diamond, 1.5f),
            new BlockHardnessData(BlockType.Water, 0f),
            new BlockHardnessData(BlockType.Bedrock, 0f, true) // Unbreakable
        };
        
        foreach (var data in hardnessValues)
        {
            hardnessData[data.blockType] = data;
        }
    }
    
    public static float GetHardness(BlockType blockType)
    {
        if (hardnessData.TryGetValue(blockType, out BlockHardnessData data))
        {
            return data.hardness;
        }
        return 1.0f; // Default hardness
    }
    
    public static bool IsUnbreakable(BlockType blockType)
    {
        if (hardnessData.TryGetValue(blockType, out BlockHardnessData data))
        {
            return data.isUnbreakable;
        }
        return false;
    }
    
    public static float GetMiningTime(BlockType blockType)
    {
        // Base mining time multiplied by hardness
        float baseTime = 0.5f; // Base time in seconds for soft blocks
        float hardness = GetHardness(blockType);
        
        if (IsUnbreakable(blockType))
            return float.MaxValue;
            
        return baseTime * Mathf.Max(0.1f, hardness);
    }
}