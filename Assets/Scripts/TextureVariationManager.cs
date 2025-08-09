using UnityEngine;
using System.Collections.Generic;

public class TextureVariationManager : MonoBehaviour
{
    [System.Serializable]
    public class TextureSet
    {
        public BlockType blockType;
        public Texture2D[] variations;
        public Material[] materials; // Cache for generated materials
    }
    
    [Header("Texture Variations")]
    public TextureSet[] textureSets;
    
    [Header("Texture Settings")]
    [Range(0.1f, 2.0f)]
    public float textureScale = 1.0f;
    
    [Range(0f, 1f)]
    public float noiseIntensity = 0.2f;
    
    private Dictionary<BlockType, TextureSet> textureMap;
    private System.Random random;
    
    void Awake()
    {
        InitializeTextures();
        CreateVariationMaterials();
    }
    
    void InitializeTextures()
    {
        textureMap = new Dictionary<BlockType, TextureSet>();
        random = new System.Random();
        
        // Add null check for textureSets
        if (textureSets == null)
        {
            Debug.LogWarning("TextureVariationManager: textureSets is null. No texture variations will be available.");
            return;
        }
        
        // Configure texture import settings to reduce tiling
        foreach (var set in textureSets)
        {
            // Add null check for the set itself
            if (set == null)
            {
                Debug.LogWarning("TextureVariationManager: Found null texture set, skipping.");
                continue;
            }
            
            textureMap[set.blockType] = set;
            
            // Add null check for variations array
            if (set.variations == null)
            {
                Debug.LogWarning($"TextureVariationManager: variations array is null for {set.blockType}, skipping.");
                continue;
            }
            
            foreach (var texture in set.variations)
            {
                if (texture != null)
                {
                    // Set texture to point filtering for pixelated look (Minecraft style)
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Repeat;
                    texture.anisoLevel = 0;
                }
            }
        }
    }
    
    void CreateVariationMaterials()
    {
        // Add null check for textureSets
        if (textureSets == null)
        {
            Debug.LogWarning("TextureVariationManager: textureSets is null. No materials will be created.");
            return;
        }
        
        foreach (var set in textureSets)
        {
            // Add null check for the set itself
            if (set == null)
            {
                Debug.LogWarning("TextureVariationManager: Found null texture set, skipping material creation.");
                continue;
            }
            
            // Add null check for variations array
            if (set.variations == null || set.variations.Length == 0)
            {
                Debug.LogWarning($"TextureVariationManager: No variations found for {set.blockType}, skipping material creation.");
                continue;
            }
            
            set.materials = new Material[set.variations.Length];
            
            for (int i = 0; i < set.variations.Length; i++)
            {
                if (set.variations[i] != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.mainTexture = set.variations[i];
                    mat.color = Color.white;
                    mat.name = $"{set.blockType}Material_Variation{i}";
                    
                    // Configure material for better pixel art rendering
                    mat.SetFloat("_Smoothness", 0.0f);
                    mat.SetFloat("_Metallic", 0.0f);
                    
                    // Set texture scale
                    mat.mainTextureScale = Vector2.one * textureScale;
                    
                    set.materials[i] = mat;
                    
                    // Update BlockDatabase if it exists
                    if (i == 0 && BlockDatabase.blockTypes.Length > (int)set.blockType)
                    {
                        BlockDatabase.blockTypes[(int)set.blockType].blockMaterial = mat;
                    }
                }
            }
        }
    }
    
    public Material GetRandomMaterial(BlockType blockType, Vector3Int position)
    {
        if (!textureMap.ContainsKey(blockType))
            return null;
            
        var textureSet = textureMap[blockType];
        if (textureSet.materials == null || textureSet.materials.Length == 0)
            return null;
            
        // Use position-based seeded random for consistent variation per block position
        int seed = position.x * 73856093 + position.y * 19349663 + position.z * 83492791;
        System.Random positionRandom = new System.Random(seed);
        
        int index = positionRandom.Next(textureSet.materials.Length);
        return textureSet.materials[index];
    }
    
    public Material GetMaterial(BlockType blockType, int variationIndex = 0)
    {
        if (!textureMap.ContainsKey(blockType))
            return null;
            
        var textureSet = textureMap[blockType];
        if (textureSet.materials == null || textureSet.materials.Length == 0)
            return null;
            
        variationIndex = Mathf.Clamp(variationIndex, 0, textureSet.materials.Length - 1);
        return textureSet.materials[variationIndex];
    }
}
