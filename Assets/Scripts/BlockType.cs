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
    Coal
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
        new BlockData(BlockType.Coal, "Coal", Color.black)
    };
    
    public static BlockData GetBlockData(BlockType blockType)
    {
        return blockTypes[(int)blockType];
    }
}