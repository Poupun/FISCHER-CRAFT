using UnityEngine;

[CreateAssetMenu(fileName = "BlockConfiguration", menuName = "Blocks/Block Configuration", order = 1)]
public class BlockConfiguration : ScriptableObject
{
    [Header("Basic Info")]
    public BlockType blockType;
    public string displayName;
    public Color tintColor = Color.white;
    
    [Header("Textures")]
    [Tooltip("Main texture used for single-sided blocks or as fallback")]
    public Texture2D mainTexture;
    
    [Space]
    public bool hasMultipleSides;
    [Tooltip("Only used if hasMultipleSides is true")]
    public Texture2D topTexture;
    [Tooltip("Only used if hasMultipleSides is true")]
    public Texture2D sideTexture;
    [Tooltip("Only used if hasMultipleSides is true")]
    public Texture2D bottomTexture;
    
    [Header("Advanced Face Textures (Optional)")]
    [Tooltip("If set, overrides sideTexture for front face")]
    public Texture2D frontTexture;
    [Tooltip("If set, overrides sideTexture for back face")]
    public Texture2D backTexture;
    [Tooltip("If set, overrides sideTexture for left face")]
    public Texture2D leftTexture;
    [Tooltip("If set, overrides sideTexture for right face")]
    public Texture2D rightTexture;
    
    [Header("Properties")]
    [Range(0.1f, 10f)]
    public float hardness = 1f;
    public bool isUnbreakable;
    public bool isPlaceable = true;
    public bool isCraftable = true;
    public bool isMineable = true;
    
    [Header("World Rendering")]
    [Tooltip("Optional material override for world rendering")]
    public Material customMaterial;
    
    public Texture2D GetTextureForFace(BlockFace face)
    {
        if (!hasMultipleSides)
            return mainTexture;
            
        switch (face)
        {
            case BlockFace.Top:
                return topTexture ?? mainTexture;
            case BlockFace.Bottom:
                return bottomTexture ?? mainTexture;
            case BlockFace.Front:
                return frontTexture ?? sideTexture ?? mainTexture;
            case BlockFace.Back:
                return backTexture ?? sideTexture ?? mainTexture;
            case BlockFace.Left:
                return leftTexture ?? sideTexture ?? mainTexture;
            case BlockFace.Right:
                return rightTexture ?? sideTexture ?? mainTexture;
            default:
                return mainTexture;
        }
    }
}

public enum BlockFace
{
    Top,
    Bottom,
    Front,
    Back,
    Left,
    Right
}