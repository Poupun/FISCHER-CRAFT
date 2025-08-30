using UnityEngine;

[System.Serializable]
public enum BlockType
{
    Air,
    Grass,
    Dirt,
    Stone,
    Water,
    Sand,
    Coal,
    Log,
    Leaves,
    Bedrock,
    Gravel,
    Iron,
    Gold,
    Diamond
}

[System.Serializable]
public class BlockData
{
    public BlockType blockType;
    public string blockName;
    public Color blockColor;
    public Material blockMaterial;
    public Texture2D blockTexture;
    
    public BlockData(BlockType type, string name, Color color)
    {
        blockType = type;
        blockName = name;
        blockColor = color;
    }
    
    public BlockData(BlockType type, string name, Color color, Texture2D texture)
    {
        blockType = type;
        blockName = name;
        blockColor = color;
        blockTexture = texture;
    }
}

public static class BlockDatabase
{
    public static BlockData[] blockTypes = new BlockData[]
    {
        new BlockData(BlockType.Air, "Air", Color.clear),
        new BlockData(BlockType.Grass, "Grass", Color.green),
        new BlockData(BlockType.Dirt, "Dirt", new Color(0.6f, 0.4f, 0.2f)), // Brown
        new BlockData(BlockType.Stone, "Stone", Color.gray),
        new BlockData(BlockType.Water, "Water", Color.blue),
        new BlockData(BlockType.Sand, "Sand", Color.white),
        new BlockData(BlockType.Coal, "Coal", Color.black),
        new BlockData(BlockType.Log, "Log", new Color(0.55f, 0.27f, 0.07f)),
        new BlockData(BlockType.Leaves, "Leaves", new Color(0.2f, 0.6f, 0.2f)),
        new BlockData(BlockType.Bedrock, "Bedrock", new Color(0.2f, 0.2f, 0.2f)),
        new BlockData(BlockType.Gravel, "Gravel", new Color(0.5f, 0.5f, 0.5f)),
        new BlockData(BlockType.Iron, "Iron Ore", new Color(0.8f, 0.7f, 0.6f)),
        new BlockData(BlockType.Gold, "Gold Ore", new Color(1.0f, 0.8f, 0.0f)),
        new BlockData(BlockType.Diamond, "Diamond Ore", new Color(0.4f, 0.8f, 1.0f))
    };
    
    public static BlockData GetBlockData(BlockType blockType)
    {
        return blockTypes[(int)blockType];
    }
}