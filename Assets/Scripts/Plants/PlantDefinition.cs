using UnityEngine;

[CreateAssetMenu(menuName = "FISCHER-CRAFT/Plants/Plant Definition", fileName = "PlantDefinition")]
public class PlantDefinition : ScriptableObject
{
    [Header("Visuals")]
    public Texture2D texture;
    public Color tint = Color.white;
    [Tooltip("Crossed-quad width in meters.")]
    [Range(0.2f, 1.5f)] public float width = 0.9f;
    [Tooltip("Min/Max height range in meters; a random value is picked per instance.")]
    public Vector2 heightRange = new Vector2(0.6f, 1.2f);
    [Tooltip("Vertical offset to avoid z-fighting with the ground.")]
    [Range(-0.05f, 0.1f)] public float yOffset = 0.02f;
    [Tooltip("How many crossed-quad sets per instance (cluster look).")]
    [Range(1, 4)] public int quadsPerInstance = 2;

    [Header("Distribution")] 
    [Tooltip("Relative spawn weight for this plant compared to others.")]
    [Min(0f)] public float weight = 1f;

    [Tooltip("Optional perlin threshold [0..1] to gate spawn; <=0 disables.")]
    [Range(0f, 1f)] public float noiseThreshold = 0f;
    [Tooltip("Noise scale (smaller = larger patches).")]
    [Min(0.0001f)] public float noiseScale = 0.15f;
}
