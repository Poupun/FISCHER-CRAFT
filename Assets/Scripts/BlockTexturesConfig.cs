using UnityEngine;

[CreateAssetMenu(menuName = "Config/Block Textures Config", fileName = "BlockTexturesConfig")] 
public class BlockTexturesConfig : ScriptableObject
{
    [Header("Core Block Textures")] 
    public Texture2D grassTop; 
    public Texture2D grassSide; 
    public Texture2D dirt; 
    public Texture2D stone; 
    public Texture2D sand; 
    public Texture2D coal; 
    public Texture2D log; 
    public Texture2D leaves; 

    [Header("Plants (Optional)")] 
    [Tooltip("Plant textures for simple billboard plants; order not enforced.")] 
    public Texture2D[] plantTextures; 
}
