using UnityEngine;

[CreateAssetMenu(fileName = "ItemConfiguration", menuName = "Items/Item Configuration", order = 2)]
public class ItemConfiguration : ScriptableObject
{
    [Header("Basic Info")]
    public BlockType itemType; // Reuse BlockType enum for simplicity
    public string displayName;
    public Color tintColor = Color.white;
    
    [Header("Item Properties")]
    public Texture2D iconTexture;
    public bool isStackable = true;
    public int maxStackSize = 64;
    public bool isCraftable = true;
    public bool isUsable = false;
    
    [Header("Tool Properties")]
    public bool isTool = false;
    public float toolPower = 1.0f;
    public int durability = 100;
}
