using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class Simple3DIconRenderer : MonoBehaviour
{
    [Header("Rendering Settings")]
    [SerializeField] private int iconResolution = 128;
    [SerializeField] private Vector3 cubeRotation = new Vector3(25, -45, 0); // Minecraft-style isometric angle
    [SerializeField] private float cubeScale = 1.0f;
    
    [Header("Face Shading (Minecraft-style Occlusion)")]
    [SerializeField] private Color topFaceShade = new Color(1.0f, 1.0f, 1.0f, 1.0f);      // Brightest (top face)
    [SerializeField] private Color frontFaceShade = new Color(0.8f, 0.8f, 0.8f, 1.0f);   // Medium (front face)
    [SerializeField] private Color rightFaceShade = new Color(0.6f, 0.6f, 0.6f, 1.0f);   // Darkest (right face)
    
    private Dictionary<BlockType, Sprite> iconCache;
    private Camera renderCamera;
    private Light renderLight;
    private GameObject iconCube;
    private Renderer cubeRenderer;
    
    public static Simple3DIconRenderer Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            iconCache = new Dictionary<BlockType, Sprite>();
            SetupRenderingEnvironment();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void SetupRenderingEnvironment()
    {
        // Create camera
        var cameraGO = new GameObject("IconRenderCamera");
        cameraGO.transform.SetParent(transform);
        cameraGO.transform.position = new Vector3(0, 0, -5);
        renderCamera = cameraGO.AddComponent<Camera>();
        
        renderCamera.cullingMask = 1 << 31; // Layer 31 for icon rendering
        renderCamera.clearFlags = CameraClearFlags.Color;
        renderCamera.backgroundColor = Color.clear;
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 1.2f;
        renderCamera.nearClipPlane = 0.1f;
        renderCamera.farClipPlane = 10f;
        renderCamera.enabled = false;
        
        // Create light
        var lightGO = new GameObject("IconRenderLight");
        lightGO.transform.SetParent(transform);
        renderLight = lightGO.AddComponent<Light>();
        renderLight.type = LightType.Directional;
        renderLight.intensity = 1.2f;
        renderLight.shadows = LightShadows.None;
        renderLight.cullingMask = 1 << 31;
        renderLight.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // Create cube will be called later when we need it
    }
    
    void CreateShadedCube()
    {
        iconCube = new GameObject("IconRenderCube");
        iconCube.transform.SetParent(transform);
        iconCube.transform.position = Vector3.zero;
        iconCube.transform.rotation = Quaternion.Euler(cubeRotation);
        iconCube.transform.localScale = Vector3.one * cubeScale;
        iconCube.layer = 31;
        
        // Create custom mesh with separate submeshes for each visible face
        var mesh = new Mesh();
        mesh.name = "ShadedCubeMesh";
        
        // Define vertices for top, front, and right faces (the visible ones in isometric view)
        Vector3[] vertices = new Vector3[12]; // 3 faces * 4 vertices each
        Vector2[] uvs = new Vector2[12];
        
        // Top face (most visible, brightest)
        vertices[0] = new Vector3(-0.5f, 0.5f, -0.5f); // back-left
        vertices[1] = new Vector3(0.5f, 0.5f, -0.5f);  // back-right
        vertices[2] = new Vector3(0.5f, 0.5f, 0.5f);   // front-right
        vertices[3] = new Vector3(-0.5f, 0.5f, 0.5f);  // front-left
        
        // Front face (medium brightness)
        vertices[4] = new Vector3(-0.5f, -0.5f, 0.5f); // bottom-left
        vertices[5] = new Vector3(0.5f, -0.5f, 0.5f);  // bottom-right
        vertices[6] = new Vector3(0.5f, 0.5f, 0.5f);   // top-right
        vertices[7] = new Vector3(-0.5f, 0.5f, 0.5f);  // top-left
        
        // Right face (darkest)
        vertices[8] = new Vector3(0.5f, -0.5f, 0.5f);  // front-bottom
        vertices[9] = new Vector3(0.5f, -0.5f, -0.5f); // back-bottom
        vertices[10] = new Vector3(0.5f, 0.5f, -0.5f); // back-top
        vertices[11] = new Vector3(0.5f, 0.5f, 0.5f);  // front-top
        
        // Set UVs for each face (standard 0,0 to 1,1 mapping)
        for (int i = 0; i < 3; i++)
        {
            int baseIndex = i * 4;
            uvs[baseIndex] = new Vector2(0, 0);     // bottom-left
            uvs[baseIndex + 1] = new Vector2(1, 0); // bottom-right
            uvs[baseIndex + 2] = new Vector2(1, 1); // top-right
            uvs[baseIndex + 3] = new Vector2(0, 1); // top-left
        }
        
        // Create submeshes (one for each face)
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.subMeshCount = 3;
        
        // Top face triangles (submesh 0)
        int[] topTriangles = { 0, 1, 2, 0, 2, 3 };
        mesh.SetTriangles(topTriangles, 0);
        
        // Front face triangles (submesh 1)  
        int[] frontTriangles = { 4, 6, 5, 4, 7, 6 };
        mesh.SetTriangles(frontTriangles, 1);
        
        // Right face triangles (submesh 2)
        int[] rightTriangles = { 8, 10, 9, 8, 11, 10 };
        mesh.SetTriangles(rightTriangles, 2);
        
        mesh.RecalculateNormals();
        
        // Add MeshFilter and MeshRenderer
        var meshFilter = iconCube.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        cubeRenderer = iconCube.AddComponent<MeshRenderer>();
        
        // Create materials for each face (will be updated per block)
        var materials = new Material[3];
        for (int i = 0; i < 3; i++)
        {
            materials[i] = new Material(Shader.Find("Sprites/Default"));
        }
        cubeRenderer.materials = materials;
        
        iconCube.SetActive(false);
    }
    
    public Sprite Get3DIcon(BlockType blockType)
    {
        if (blockType == BlockType.Air) return null;
        
        // Check cache
        if (iconCache.TryGetValue(blockType, out Sprite cachedIcon))
        {
            return cachedIcon;
        }
        
        // Get the existing flat sprite from BlockManager
        var flatSprite = BlockManager.GetBlockSprite(blockType);
        if (flatSprite == null || flatSprite.texture == null)
        {
            return null;
        }
        
        // Create or update cube with current settings
        if (iconCube == null)
        {
            CreateShadedCube();
        }
        
        // Always update cube transform and camera with current settings
        iconCube.transform.rotation = Quaternion.Euler(cubeRotation);
        iconCube.transform.localScale = Vector3.one * cubeScale;
        
        // Keep camera orthographic size fixed, let the cube scale do the work
        renderCamera.orthographicSize = 1.2f;
        
        Debug.Log($"Rendering {blockType} - Scale: {cubeScale}, Cube localScale: {iconCube.transform.localScale}, Camera orthographicSize: {renderCamera.orthographicSize}");
        
        // Apply the flat sprite's texture to each face with different shading
        var materials = cubeRenderer.materials;
        
        Debug.Log($"Applying face shading - Top: {topFaceShade}, Front: {frontFaceShade}, Right: {rightFaceShade}");
        
        // Top face (brightest)
        materials[0].mainTexture = flatSprite.texture;
        materials[0].color = topFaceShade;
        
        // Front face (medium)
        materials[1].mainTexture = flatSprite.texture;
        materials[1].color = frontFaceShade;
        
        // Right face (darkest)
        materials[2].mainTexture = flatSprite.texture;
        materials[2].color = rightFaceShade;
        
        // Reassign the materials array to ensure changes are applied
        cubeRenderer.materials = materials;
        
        // Render the 3D cube
        iconCube.SetActive(true);
        
        RenderTexture renderTexture = new RenderTexture(iconResolution, iconResolution, 16);
        renderTexture.format = RenderTextureFormat.ARGB32;
        renderCamera.targetTexture = renderTexture;
        
        renderCamera.Render();
        
        // Read pixels
        RenderTexture.active = renderTexture;
        Texture2D iconTexture = new Texture2D(iconResolution, iconResolution, TextureFormat.ARGB32, false);
        iconTexture.ReadPixels(new Rect(0, 0, iconResolution, iconResolution), 0, 0);
        iconTexture.Apply();
        RenderTexture.active = null;
        
        // Create sprite
        Sprite icon3D = Sprite.Create(
            iconTexture,
            new Rect(0, 0, iconResolution, iconResolution),
            new Vector2(0.5f, 0.5f),
            iconResolution / 2f
        );
        icon3D.name = $"{blockType}_3DIcon";
        
        // Cache it
        iconCache[blockType] = icon3D;
        
        // Cleanup
        renderCamera.targetTexture = null;
        DestroyImmediate(renderTexture);
        iconCube.SetActive(false);
        
        return icon3D;
    }
    
    public void ClearCache()
    {
        foreach (var sprite in iconCache.Values)
        {
            if (sprite != null) DestroyImmediate(sprite);
        }
        iconCache.Clear();
        Debug.Log("3D Icon cache cleared. New icons will use current settings.");
    }
    
    public void UpdateSettings(float scale, Vector3 rotation, Color topShade, Color frontShade, Color rightShade)
    {
        Debug.Log($"Simple3DIconRenderer: Updating settings - Scale: {scale}, Rotation: {rotation}");
        cubeScale = scale;
        cubeRotation = rotation;
        topFaceShade = topShade;
        frontFaceShade = frontShade;
        rightFaceShade = rightShade;
        
        // Clear cache so new icons use the updated settings
        ClearCache();
        Debug.Log($"Simple3DIconRenderer: Settings applied - Current scale: {cubeScale}");
    }
    
    void OnDestroy()
    {
        ClearCache();
    }
}