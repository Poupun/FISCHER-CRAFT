using UnityEngine;

[System.Serializable]
public class TextureAntiTilingSettings : ScriptableObject
{
    [Header("Texture Settings")]
    [Range(0.1f, 5.0f)]
    public float globalTextureScale = 1.0f;
    
    [Range(0f, 0.5f)]
    public float randomOffset = 0.1f;
    
    [Range(0.8f, 1.2f)]
    public float scaleVariation = 1.0f;
    
    [Range(0.9f, 1.1f)]
    public float colorVariation = 1.0f;
    
    [Header("Filter Settings")]
    public FilterMode filterMode = FilterMode.Point;
    public int anisoLevel = 0;
    public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
    
    public void ApplyToTexture(Texture2D texture)
    {
        if (texture == null) return;
        
        texture.filterMode = filterMode;
        texture.anisoLevel = anisoLevel;
        texture.wrapMode = wrapMode;
    }
    
    public Material CreateAntiTilingMaterial(Texture2D texture, Vector3Int position)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        if (texture != null)
        {
            ApplyToTexture(texture);
            mat.mainTexture = texture;
            
            // Position-based random variations
            int seed = position.x * 73856093 + position.y * 19349663 + position.z * 83492791;
            System.Random random = new System.Random(seed);
            
            // Scale variation
            float scale = globalTextureScale * (1.0f + ((float)random.NextDouble() - 0.5f) * (scaleVariation - 1.0f));
            mat.mainTextureScale = Vector2.one * scale;
            
            // Offset variation
            Vector2 offset = new Vector2(
                (float)random.NextDouble() * randomOffset,
                (float)random.NextDouble() * randomOffset
            );
            mat.mainTextureOffset = offset;
            
            // Color variation
            float colorVar = 1.0f + ((float)random.NextDouble() - 0.5f) * (colorVariation - 1.0f);
            mat.color = Color.white * colorVar;
        }
        
        // Material properties for better pixel art rendering
        mat.SetFloat("_Smoothness", 0.0f);
        mat.SetFloat("_Metallic", 0.0f);
        mat.SetFloat("_SpecularHighlights", 0.0f);
        mat.SetFloat("_EnvironmentReflections", 0.5f);
        
        return mat;
    }
}
