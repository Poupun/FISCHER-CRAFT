using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int worldWidth = 16;
    public int worldHeight = 16;
    public int worldDepth = 16;
    
    [Header("Block Prefab")]
    public GameObject blockPrefab;
    
    [Header("Block Textures")]
    public Texture2D grassTexture;
    public Texture2D grassSideTexture; // sides for grass block
    public Texture2D dirtTexture;
    public Texture2D stoneTexture;
    public Texture2D sandTexture;
    public Texture2D coalTexture;

    [Header("Plant Textures (1..8)")]
    public Texture2D[] plantTextures = new Texture2D[8];

    [Header("Plant Spawn Settings")]
    [Range(0f, 3f)] public float plantDensity = 0.7f; // density control; probability is clamped to [0,1]
    public AnimationCurve plantRarityCurve = AnimationCurve.Linear(0, 1, 1, 0); // we will override by weights

    [Header("Anti-Tiling Settings")]
    [Range(0.8f, 1.2f)]
    public float textureScaleVariation = 1.0f;
    
    [Range(0f, 0.3f)]
    public float textureOffsetVariation = 0.1f;
    
    private BlockType[,,] worldData;
    private Dictionary<Vector3Int, GameObject> blockObjects = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> plantObjects = new Dictionary<Vector3Int, GameObject>();
    private Material[] blockMaterials;
    private TextureVariationManager textureManager;

    
    void Start()
    {
        // Try to find TextureVariationManager, create one if it doesn't exist
        textureManager = GetComponent<TextureVariationManager>();
        if (textureManager == null)
        {
            textureManager = gameObject.AddComponent<TextureVariationManager>();
        }
        
        LoadTextures();
        CreateBlockMaterials();
        GenerateWorld();
    }
    
    
    void LoadTextures()
    {
        // Load textures from Resources folder if not assigned
        if (grassTexture == null) grassTexture = Resources.Load<Texture2D>("Textures/grass");
    if (grassSideTexture == null) grassSideTexture = Resources.Load<Texture2D>("Textures/side_normal");
    if (dirtTexture == null) dirtTexture = Resources.Load<Texture2D>("Textures/dirt");
    if (stoneTexture == null) stoneTexture = Resources.Load<Texture2D>("Textures/stone");
    if (sandTexture == null) sandTexture = Resources.Load<Texture2D>("Textures/sand");
    if (coalTexture == null) coalTexture = Resources.Load<Texture2D>("Textures/coal");

        // Plants under Resources/Textures/plants/plant1..plant8 (optional)
        for (int i = 0; i < plantTextures.Length; i++)
        {
            if (plantTextures[i] == null)
            {
                plantTextures[i] = Resources.Load<Texture2D>($"Textures/plants/plant{i+1}");
            }
        }

#if UNITY_EDITOR
    // Editor-only fallback to load directly from Assets/Textures if not found in Resources
    if (grassTexture == null) grassTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass.png");
    if (grassSideTexture == null) grassSideTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/side_normal.png");
    if (dirtTexture == null) dirtTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/dirt.png");
    if (stoneTexture == null) stoneTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/stone.png");
    if (sandTexture == null) sandTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sand.png");
    if (coalTexture == null) coalTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/coal.png");

        // Plants located at Assets/Textures/plants/1..8 or plant1..plant8
        for (int i = 0; i < plantTextures.Length; i++)
        {
            if (plantTextures[i] == null)
            {
                var p = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/plants/{i+1}.png");
                if (p == null) p = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/plants/plant{i+1}.png");
                plantTextures[i] = p;
            }
        }
#endif
        
        // Configure textures for better pixel art rendering
        ConfigureTexture(grassTexture);
    ConfigureTexture(grassSideTexture);
        ConfigureTexture(dirtTexture);
        ConfigureTexture(stoneTexture);
        ConfigureTexture(sandTexture);
        ConfigureTexture(coalTexture);
    foreach (var pt in plantTextures) ConfigureTexture(pt);
        
        // Assign textures to block data
        BlockDatabase.blockTypes[(int)BlockType.Grass].blockTexture = grassTexture;
        BlockDatabase.blockTypes[(int)BlockType.Dirt].blockTexture = dirtTexture;
        BlockDatabase.blockTypes[(int)BlockType.Stone].blockTexture = stoneTexture;
        BlockDatabase.blockTypes[(int)BlockType.Sand].blockTexture = sandTexture;
        BlockDatabase.blockTypes[(int)BlockType.Coal].blockTexture = coalTexture;
    }
    
    void ConfigureTexture(Texture2D texture)
    {
        if (texture == null) return;
        
        // Apply settings that reduce tiling appearance
        texture.filterMode = FilterMode.Point; // Pixelated look
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.anisoLevel = 0; // No anisotropic filtering for pixel art
    }
    
    void CreateBlockMaterials()
    {
        blockMaterials = new Material[BlockDatabase.blockTypes.Length];
        
        for (int i = 0; i < BlockDatabase.blockTypes.Length; i++)
        {
            if (BlockDatabase.blockTypes[i].blockType != BlockType.Air)
            {
                // Use URP/Lit shader instead of Standard
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                
                // Apply texture if available, otherwise use color
                if (BlockDatabase.blockTypes[i].blockTexture != null)
                {
                    mat.mainTexture = BlockDatabase.blockTypes[i].blockTexture;
                    mat.color = Color.white; // Use white to show texture correctly
                }
                else
                {
                    mat.color = BlockDatabase.blockTypes[i].blockColor;
                }
                
                // Configure material properties for pixel art and reduced tiling
                mat.SetFloat("_Smoothness", 0.0f); // No glossiness
                mat.SetFloat("_Metallic", 0.0f);   // Not metallic
                
                mat.name = BlockDatabase.blockTypes[i].blockName + "Material";
                blockMaterials[i] = mat;
                BlockDatabase.blockTypes[i].blockMaterial = mat;
            }
        }
    }
    
    void GenerateWorld()
    {
        worldData = new BlockType[worldWidth, worldHeight, worldDepth];
        
        // Simple world generation
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldDepth; z++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    BlockType blockType = BlockType.Air;
                    
                    // Simple terrain generation
                    if (y == 0) blockType = BlockType.Stone; // Bedrock
                    else if (y < 3) blockType = BlockType.Stone;
                    else if (y < 6) blockType = BlockType.Dirt;
                    else if (y == 6) blockType = BlockType.Grass;
                    else if (y < 4 && Random.Range(0, 100) < 5) blockType = BlockType.Coal;
                    
                    worldData[x, y, z] = blockType;
                }
            }
        }
        
        // Create visible blocks
        UpdateVisibleBlocks();
    SpawnPlants();
    }
    
    void UpdateVisibleBlocks()
    {
        // Clear existing blocks
        foreach (var block in blockObjects.Values)
        {
            if (block != null) Destroy(block);
        }
        blockObjects.Clear();
        // Clear existing plants
        foreach (var plant in plantObjects.Values)
        {
            if (plant != null) Destroy(plant);
        }
        plantObjects.Clear();
        
        // Create new blocks
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                for (int z = 0; z < worldDepth; z++)
                {
                    if (worldData[x, y, z] != BlockType.Air && ShouldRenderBlock(x, y, z))
                    {
                        CreateBlock(new Vector3Int(x, y, z), worldData[x, y, z]);
                    }
                }
            }
        }
    }

    // Spawn plants on top of exposed Grass blocks
    void SpawnPlants()
    {
        // Weighted rarity: 1 most common, 8 rarest
        int[] weights = new int[] { 40, 25, 15, 9, 6, 3, 1, 1 };
        int total = 0; foreach (var w in weights) total += w;

        System.Random rng = new System.Random(12345);

        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldDepth; z++)
            {
                for (int y = 1; y < worldHeight; y++)
                {
                    var pos = new Vector3Int(x, y, z);
                    // Check block is Grass and the block above is Air
                    if (GetBlockType(pos) == BlockType.Grass && GetBlockType(new Vector3Int(x, y + 1, z)) == BlockType.Air)
                    {
                        // Only if the top face is exposed
                        if (ShouldRenderBlock(x, y, z))
                        {
                            if (HasAnyPlantTextures())
                            {
                                // Interpret plantDensity as expected blades per tile. Values >1 spawn clusters.
                                float desired = Mathf.Max(0f, plantDensity);
                                int count = Mathf.FloorToInt(desired);
                                double remainder = desired - count;
                                if (rng.NextDouble() < remainder) count++;
                                if (count > 0)
                                {
                                    var parent = new GameObject($"Plants ({x},{y+1},{z})");
                                    parent.transform.parent = this.transform;
                                    // place on exact top face center using renderer bounds if available
                                    Vector3 placePos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                                    if (blockObjects.TryGetValue(new Vector3Int(x, y, z), out var grassGo))
                                    {
                                        var rend = grassGo.GetComponent<Renderer>();
                                        if (rend != null)
                                        {
                                            var b = rend.bounds;
                                            placePos = new Vector3(b.center.x, b.max.y + 0.001f, b.center.z);
                                        }
                                    }
                                    parent.transform.position = placePos;

                                    for (int i = 0; i < count; i++)
                                    {
                                        // pick a plant texture by weights
                                        int pick = rng.Next(total);
                                        int idx = 0; int acc = 0;
                                        for (; idx < weights.Length; idx++) { acc += weights[idx]; if (pick < acc) break; }
                                        idx = Mathf.Clamp(idx, 0, plantTextures.Length - 1);
                                        var tex = plantTextures[idx];
                                        if (tex == null) continue;

                                        var child = new GameObject($"Plant_{idx+1}");
                                        child.transform.parent = parent.transform;
                                        // small random local offset inside the tile footprint
                                        float off = 0.18f;
                                        float ox = (float)(rng.NextDouble()*2 - 1) * off;
                                        float oz = (float)(rng.NextDouble()*2 - 1) * off;
                                        child.transform.localPosition = new Vector3(ox, 0f, oz);
                                        child.transform.localRotation = Quaternion.Euler(0f, (float)(rng.NextDouble()*360.0), 0f);
                                        child.AddComponent<MeshFilter>();
                                        child.AddComponent<MeshRenderer>();
                                        var pbt = ResolveTypeByName("PlantBillboard");
                                        if (pbt != null)
                                        {
                                            var comp = child.AddComponent(pbt);
                                            float h = Mathf.Lerp(0.5f, 1.2f, idx / 7.0f);
                                            var method = pbt.GetMethod("Configure", new System.Type[] { typeof(Texture2D), typeof(float), typeof(float), typeof(float) });
                                            if (method != null)
                                            {
                                                method.Invoke(comp, new object[] { tex, 0.9f, h, 0.01f });
                                            }
                                            var fWorld = pbt.GetField("world"); if (fWorld != null) fWorld.SetValue(comp, this);
                                            var fSupport = pbt.GetField("supportCell"); if (fSupport != null) fSupport.SetValue(comp, new Vector3Int(x, y, z));
                                        }
                                    }

                                    plantObjects[new Vector3Int(x, y + 1, z)] = parent;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    bool HasAnyPlantTextures()
    {
        foreach (var t in plantTextures) if (t != null) return true; return false;
    }
    
    bool ShouldRenderBlock(int x, int y, int z)
    {
        // Check if block has at least one exposed face
        Vector3Int[] directions = {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.forward, Vector3Int.back
        };
        
        foreach (Vector3Int dir in directions)
        {
            Vector3Int neighbor = new Vector3Int(x, y, z) + dir;
            if (IsOutOfBounds(neighbor) || GetBlockType(neighbor) == BlockType.Air)
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool IsOutOfBounds(Vector3Int pos)
    {
        return pos.x < 0 || pos.x >= worldWidth ||
               pos.y < 0 || pos.y >= worldHeight ||
               pos.z < 0 || pos.z >= worldDepth;
    }
    
    void CreateBlock(Vector3Int position, BlockType blockType)
    {
        if (blockPrefab == null) return;
        
        GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
        block.name = $"{BlockDatabase.GetBlockData(blockType).blockName} ({position.x},{position.y},{position.z})";
        
        // Apply material with position-based variation
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (blockType == BlockType.Grass)
            {
                // Try to add/apply GrassBlockRenderer via reflection to avoid compile order issues
                var type = ResolveTypeByName("GrassBlockRenderer");
                if (type != null)
                {
                    var comp = block.GetComponent(type);
                    if (comp == null) comp = block.AddComponent(type);
                    var apply = type.GetMethod("Apply", new System.Type[] { typeof(WorldGenerator) });
                    if (apply != null)
                    {
                        apply.Invoke(comp, new object[] { this });
                    }
                }
                else
                {
                    // Fallback to single material
                    Material baseMaterial = blockMaterials[(int)blockType];
                    if (baseMaterial != null)
                    {
                        Material instanceMaterial = CreateVariationMaterial(baseMaterial, position);
                        renderer.material = instanceMaterial;
                    }
                }
            }
            else
            {
                Material baseMaterial = blockMaterials[(int)blockType];
                if (baseMaterial != null)
                {
                    // Create a unique material instance with slight variations
                    Material instanceMaterial = CreateVariationMaterial(baseMaterial, position);
                    renderer.material = instanceMaterial;
                }
            }
        }
        
        // Add BlockInfo component
        BlockInfo blockInfo = block.GetComponent<BlockInfo>();
        if (blockInfo == null)
        {
            blockInfo = block.AddComponent<BlockInfo>();
        }
        blockInfo.blockType = blockType;
        blockInfo.position = position;
        
        blockObjects[position] = block;
    }
    
    Material CreateVariationMaterial(Material baseMaterial, Vector3Int position)
    {
        // Create an instance of the material
        Material instanceMaterial = new Material(baseMaterial);
        
        // Create position-based seed for consistent variation
        int seed = position.x * 73856093 + position.y * 19349663 + position.z * 83492791;
        System.Random posRandom = new System.Random(seed);
        
        // Apply subtle scale variations
        float scaleVariation = 1.0f + (posRandom.Next(-10, 11) * 0.01f * textureScaleVariation);
        instanceMaterial.mainTextureScale = Vector2.one * scaleVariation;
        
        // Apply small random texture offset to break up tiling patterns
        float offsetX = (float)posRandom.NextDouble() * textureOffsetVariation;
        float offsetY = (float)posRandom.NextDouble() * textureOffsetVariation;
        instanceMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
        
        // Slight color variation to add more natural look
        float colorVariation = 0.95f + (float)posRandom.NextDouble() * 0.1f; // 95% to 105%
        Color baseColor = baseMaterial.color;
        instanceMaterial.color = new Color(
            baseColor.r * colorVariation,
            baseColor.g * colorVariation,
            baseColor.b * colorVariation,
            baseColor.a
        );
        
        return instanceMaterial;
    }
    
    public BlockType GetBlockType(Vector3Int position)
    {
        if (IsOutOfBounds(position)) return BlockType.Air;
        return worldData[position.x, position.y, position.z];
    }
    
    public bool PlaceBlock(Vector3Int position, BlockType blockType)
    {
        if (IsOutOfBounds(position)) return false;
        
        BlockType oldBlockType = worldData[position.x, position.y, position.z];
        worldData[position.x, position.y, position.z] = blockType;

        // If a block is placed where a plant currently exists, remove the plant at that cell
        if (blockType != BlockType.Air)
        {
            if (plantObjects.TryGetValue(position, out var plantAtPos))
            {
                if (plantAtPos != null) Destroy(plantAtPos);
                plantObjects.Remove(position);
            }
        }

    // If the support block is not Grass anymore (including Air), remove plant above it
    var above = position + Vector3Int.up;
    if (blockType != BlockType.Grass)
        {
            if (plantObjects.TryGetValue(above, out var plantAbove))
            {
                if (plantAbove != null) Destroy(plantAbove);
                plantObjects.Remove(above);
            }
        }
        
        if (blockType == BlockType.Air)
        {
            // Remove block
            if (blockObjects.ContainsKey(position))
            {
                Destroy(blockObjects[position]);
                blockObjects.Remove(position);
            }
        }
        else
        {
            // Place block
            CreateBlock(position, blockType);
        }
        
        // Update neighboring blocks visibility
        UpdateNeighboringBlocks(position);
        
        
        return true;
    }
    
    void UpdateNeighboringBlocks(Vector3Int position)
    {
        Vector3Int[] directions = {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.forward, Vector3Int.back
        };
        
        foreach (Vector3Int dir in directions)
        {
            Vector3Int neighbor = position + dir;
            if (!IsOutOfBounds(neighbor))
            {
                BlockType blockType = worldData[neighbor.x, neighbor.y, neighbor.z];
                
                if (blockType != BlockType.Air)
                {
                    if (ShouldRenderBlock(neighbor.x, neighbor.y, neighbor.z))
                    {
                        if (!blockObjects.ContainsKey(neighbor))
                        {
                            CreateBlock(neighbor, blockType);
                        }
                        // If this is grass with air above, consider spawning a plant (on-demand)
                        if (blockType == BlockType.Grass && GetBlockType(neighbor + Vector3Int.up) == BlockType.Air)
                        {
                            // Avoid duplicates
                            if (!plantObjects.ContainsKey(neighbor + Vector3Int.up))
                            {
                                // When revealing a new grass top, spawn a cluster based on scaled density
                                System.Random rng = new System.Random(neighbor.GetHashCode());
                                if (HasAnyPlantTextures())
                                {
                                    int[] weights = new int[] { 40, 25, 15, 9, 6, 3, 1, 1 };
                                    int total = 0; foreach (var w in weights) total += w;

                                    float desired = Mathf.Max(0f, plantDensity * 0.5f);
                                    int count = Mathf.FloorToInt(desired);
                                    double remainder = desired - count;
                                    if (rng.NextDouble() < remainder) count++;

                                    if (count > 0)
                                    {
                                        var parent = new GameObject($"Plants ({neighbor.x},{neighbor.y+1},{neighbor.z})");
                                        parent.transform.parent = this.transform;
                                        Vector3 placePos2 = new Vector3(neighbor.x + 0.5f, neighbor.y + 0.5f, neighbor.z + 0.5f);
                                        if (blockObjects.TryGetValue(neighbor, out var grassGo2))
                                        {
                                            var rend2 = grassGo2.GetComponent<Renderer>();
                                            if (rend2 != null)
                                            {
                                                var b2 = rend2.bounds;
                                                placePos2 = new Vector3(b2.center.x, b2.max.y + 0.001f, b2.center.z);
                                            }
                                        }
                                        parent.transform.position = placePos2;

                                        for (int i = 0; i < count; i++)
                                        {
                                            int pick = rng.Next(total);
                                            int idx = 0; int acc = 0;
                                            for (; idx < weights.Length; idx++) { acc += weights[idx]; if (pick < acc) break; }
                                            idx = Mathf.Clamp(idx, 0, plantTextures.Length - 1);
                                            var tex = plantTextures[idx];
                                            if (tex == null) continue;

                                            var child = new GameObject($"Plant_{idx+1}");
                                            child.transform.parent = parent.transform;
                                            float off = 0.18f;
                                            float ox = (float)(rng.NextDouble()*2 - 1) * off;
                                            float oz = (float)(rng.NextDouble()*2 - 1) * off;
                                            child.transform.localPosition = new Vector3(ox, 0f, oz);
                                            child.transform.localRotation = Quaternion.Euler(0f, (float)(rng.NextDouble()*360.0), 0f);
                                            child.AddComponent<MeshFilter>();
                                            child.AddComponent<MeshRenderer>();
                                            var pbt = ResolveTypeByName("PlantBillboard");
                                            if (pbt != null)
                                            {
                                                var comp = child.AddComponent(pbt);
                                                float h = Mathf.Lerp(0.5f, 1.2f, idx / 7.0f);
                                                var method = pbt.GetMethod("Configure", new System.Type[] { typeof(Texture2D), typeof(float), typeof(float), typeof(float) });
                                                if (method != null)
                                                {
                                                    method.Invoke(comp, new object[] { tex, 0.9f, h, 0.01f });
                                                }
                                                var fWorld = pbt.GetField("world"); if (fWorld != null) fWorld.SetValue(comp, this);
                                                var fSupport = pbt.GetField("supportCell"); if (fSupport != null) fSupport.SetValue(comp, neighbor);
                                            }
                                        }

                                        plantObjects[neighbor + Vector3Int.up] = parent;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (blockObjects.ContainsKey(neighbor))
                        {
                            Destroy(blockObjects[neighbor]);
                            blockObjects.Remove(neighbor);
                        }
                        // remove plant above if exists
                        if (plantObjects.ContainsKey(neighbor + Vector3Int.up))
                        {
                            Destroy(plantObjects[neighbor + Vector3Int.up]);
                            plantObjects.Remove(neighbor + Vector3Int.up);
                        }
                        // also if neighbor became non-grass or air, ensure plant above is cleared
                        var neighborType = GetBlockType(neighbor);
                        if (neighborType == BlockType.Air || neighborType != BlockType.Grass)
                        {
                            var posAbove = neighbor + Vector3Int.up;
                            if (plantObjects.TryGetValue(posAbove, out var p))
                            {
                                if (p != null) Destroy(p);
                                plantObjects.Remove(posAbove);
                            }
                        }
                    }
                }
            }
        }
    }

    // Public helper for PlantBillboard and internal cleanup
    public void RemovePlantAt(Vector3Int cell)
    {
        if (plantObjects.TryGetValue(cell, out var obj))
        {
            if (obj != null) Destroy(obj);
            plantObjects.Remove(cell);
        }
    }

    // Utility: find a type by simple name across loaded assemblies
    private System.Type ResolveTypeByName(string typeName)
    {
        var t = System.Type.GetType(typeName);
        if (t != null) return t;
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            t = asm.GetType(typeName);
            if (t != null) return t;
            foreach (var candidate in asm.GetTypes())
            {
                if (candidate.Name == typeName) return candidate;
            }
        }
        return null;
    }
}
