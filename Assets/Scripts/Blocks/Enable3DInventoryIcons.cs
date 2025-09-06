using UnityEngine;

public class Enable3DInventoryIcons : MonoBehaviour
{
    [Header("3D Icon Settings")]
    [SerializeField] private bool enableOnStart = true;
    [SerializeField] private int iconResolution = 128;
    [SerializeField] private Vector3 cubeRotation = new Vector3(25, -45, 0);
    [SerializeField] private float cubeScale = 1.0f;
    
    [Header("Face Shading (Minecraft-style Occlusion)")]
    [SerializeField] private Color topFaceShade = new Color(1.0f, 1.0f, 1.0f, 1.0f);      // Brightest (top face)
    [SerializeField] private Color frontFaceShade = new Color(0.8f, 0.8f, 0.8f, 1.0f);   // Medium (front face)
    [SerializeField] private Color rightFaceShade = new Color(0.6f, 0.6f, 0.6f, 1.0f);   // Darkest (right face)
    
    // Track previous values to detect changes
    private int previousResolution;
    private Vector3 previousRotation;
    private float previousScale;
    private Color previousTopShade;
    private Color previousFrontShade;
    private Color previousRightShade;
    
    void Start()
    {
        if (enableOnStart)
        {
            Setup3DIcons();
        }
        
        // Initialize previous values
        UpdatePreviousValues();
    }
    
    void Update()
    {
        // Check for changes in inspector values and auto-update
        if (HasSettingsChanged())
        {
            UpdateSettings();
            UpdatePreviousValues();
        }
    }
    
    private bool HasSettingsChanged()
    {
        return iconResolution != previousResolution ||
               cubeRotation != previousRotation ||
               cubeScale != previousScale ||
               topFaceShade != previousTopShade ||
               frontFaceShade != previousFrontShade ||
               rightFaceShade != previousRightShade;
    }
    
    private void UpdatePreviousValues()
    {
        previousResolution = iconResolution;
        previousRotation = cubeRotation;
        previousScale = cubeScale;
        previousTopShade = topFaceShade;
        previousFrontShade = frontFaceShade;
        previousRightShade = rightFaceShade;
    }
    
    public void Setup3DIcons()
    {
        // Check if already exists
        if (Simple3DIconRenderer.Instance != null)
        {
            Debug.Log("3D Icon Renderer already exists");
            return;
        }
        
        // Create the renderer
        var rendererGO = new GameObject("3D Icon Renderer");
        var renderer = rendererGO.AddComponent<Simple3DIconRenderer>();
        
        // Apply settings via reflection (since fields are private)
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        
        var resolutionField = typeof(Simple3DIconRenderer).GetField("iconResolution", flags);
        resolutionField?.SetValue(renderer, iconResolution);
        
        var rotationField = typeof(Simple3DIconRenderer).GetField("cubeRotation", flags);
        rotationField?.SetValue(renderer, cubeRotation);
        
        var scaleField = typeof(Simple3DIconRenderer).GetField("cubeScale", flags);
        scaleField?.SetValue(renderer, cubeScale);
        
        var topShadeField = typeof(Simple3DIconRenderer).GetField("topFaceShade", flags);
        topShadeField?.SetValue(renderer, topFaceShade);
        
        var frontShadeField = typeof(Simple3DIconRenderer).GetField("frontFaceShade", flags);
        frontShadeField?.SetValue(renderer, frontFaceShade);
        
        var rightShadeField = typeof(Simple3DIconRenderer).GetField("rightFaceShade", flags);
        rightShadeField?.SetValue(renderer, rightFaceShade);
        
        Debug.Log("3D Inventory Icons enabled!");
    }
    
    public void Disable3DIcons()
    {
        if (Simple3DIconRenderer.Instance != null)
        {
            Destroy(Simple3DIconRenderer.Instance.gameObject);
            Debug.Log("3D Inventory Icons disabled");
        }
    }
    
    [ContextMenu("Update 3D Icon Settings")]
    public void UpdateSettings()
    {
        if (Simple3DIconRenderer.Instance != null)
        {
            Debug.Log($"Updating 3D Icon settings - Scale: {cubeScale}, Rotation: {cubeRotation}");
            Simple3DIconRenderer.Instance.UpdateSettings(
                cubeScale, 
                cubeRotation, 
                topFaceShade, 
                frontFaceShade, 
                rightFaceShade
            );
            Debug.Log("3D Icon settings updated and cache cleared!");
        }
        else
        {
            Debug.Log("No 3D Icon Renderer found. Enable 3D icons first.");
        }
    }
    
    [ContextMenu("Clear 3D Icon Cache")]
    public void ClearIconCache()
    {
        if (Simple3DIconRenderer.Instance != null)
        {
            Simple3DIconRenderer.Instance.ClearCache();
        }
        else
        {
            Debug.Log("No 3D Icon Renderer found.");
        }
    }
}