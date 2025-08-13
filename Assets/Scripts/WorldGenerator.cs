using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
// PlantDefinition and PlantDatabase are in global namespace after refactor

public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int worldWidth = 16;
    public int worldHeight = 16;
    public int worldDepth = 16;

    [Header("Superflat Settings")] 
    [Tooltip("When enabled, generate a Minecraft-like superflat world (no caves, plains only)")]
    public bool generateSuperflat = true;
    [Min(0)] public int flatStoneLayers = 1;   // acts as bedrock for now
    [Min(0)] public int flatDirtLayers = 3;
    [Min(0)] public int flatGrassLayers = 1;

    [Header("Chunk Streaming")] 
    [Tooltip("Enable player-centered chunk streaming for infinite superflat world")]
    public bool useChunkStreaming = true;
    [Min(4)] public int chunkSizeX = 16;
    [Min(4)] public int chunkSizeZ = 16;
    [Min(1)] public int viewDistanceChunks = 4; // Manhattan or square radius
    [Tooltip("Player transform used to center chunk streaming. If null, will try to auto-find.")]
    public Transform player;
    private Transform _chunksRoot;
    private readonly Dictionary<Vector2Int, WorldGeneration.Chunks.Chunk> _chunks = new Dictionary<Vector2Int, WorldGeneration.Chunks.Chunk>();
    private readonly Queue<Vector2Int> _pendingLoads = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> _queued = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _loading = new HashSet<Vector2Int>();
    [Min(1)] public int maxChunkLoadsPerFrame = 1;
    [Header("Meshing (Experimental)")]
    [Tooltip("If enabled, build one mesh per chunk instead of instantiating each block GameObject.")]
    public bool useChunkMeshing = true;
    [Tooltip("Add MeshCollider to chunk mesh for collisions.")]
    public bool addChunkCollider = true;
    [Header("Persistence")]
    [Tooltip("Save chunk edits so placed/removed blocks persist across unloads.")]
    public bool enableChunkPersistence = true;
    [Tooltip("Folder name under persistent data path to store chunk saves.")]
    public string saveFolderName = "ChunkSaves";
    
    [Header("Block Prefab")]
    public GameObject blockPrefab;
    
    [Header("Player Spawn Gating")]
    [Tooltip("If enabled, the player GameObject stays inactive until the first center chunk finishes building its mesh.")]
    public bool delayPlayerSpawnUntilChunks = true;
    [Tooltip("After chunks are ready, reposition the player to stand on the highest solid block under them.")]
    public bool snapPlayerToGroundOnSpawn = true;
    
    [Header("Block Textures")]
    public Texture2D grassTexture;
    public Texture2D grassSideTexture; // sides for grass block
    public Texture2D dirtTexture;
    public Texture2D stoneTexture;
    public Texture2D sandTexture;
    public Texture2D coalTexture;

    [Header("Plants (New)")]
    [Tooltip("Scriptable Object list of plants with textures, sizes, and weights.")]
    public ScriptableObject plantDatabase;
    [Tooltip("Expected plant instances per exposed grass tile (can be fractional).")]
    [Range(0f, 3f)] public float plantDensity = 0.7f;

    [Header("Anti-Tiling Settings")]
    [Range(0.8f, 1.2f)]
    public float textureScaleVariation = 1.0f;
    
    [Range(0f, 0.3f)]
    public float textureOffsetVariation = 0.1f;
    
    private BlockType[,,] worldData;
    private Dictionary<Vector3Int, GameObject> blockObjects = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> plantObjects = new Dictionary<Vector3Int, GameObject>(); // legacy per-tile plants (non-meshing)
    private Material[] blockMaterials;
    // Cached special-case materials
    private Material _grassSideMaterial;
    private Material _grassSideOverlayMaterial; // transparent cutout overlay used atop dirt on grass sides
    private TextureVariationManager textureManager;
    // Cache for plant materials by texture so we don't create per-instance materials
    private readonly Dictionary<Texture2D, Material> _plantMaterialCache = new Dictionary<Texture2D, Material>();
    
    // Tick system for delayed plant behavior
    [Header("Tick Settings")]
    [Tooltip("Seconds per game tick (Minecraft-like pacing)")]
    public float tickIntervalSeconds = 0.5f;
    [Tooltip("Ticks before plants reappear after being broken")] public int plantRespawnDelayTicks = 5;
    [Tooltip("Ticks before plants appear on newly placed/revealed grass")] public int newGrassPlantDelayTicks = 5;
    [Tooltip("Ticks before exposed Dirt turns into Grass")] public int dirtToGrassDelayTicks = 10;
    private float _tickAccum = 0f;
    private int _currentTick = 0;
    private struct TickAction { public int dueTick; public int type; public Vector3Int cell; }
    private const int TA_PlantRespawn = 1;
    private const int TA_GrassGrow = 2;
    private readonly List<TickAction> _tickQueue = new List<TickAction>();
    private readonly HashSet<Vector3Int> _scheduledPlantCells = new HashSet<Vector3Int>(); // grass cell keys
    private readonly HashSet<Vector3Int> _scheduledGrassCells = new HashSet<Vector3Int>(); // dirt cell keys
    // Chunk persistence: per-chunk map of edited cells (world cell -> type)
    private readonly Dictionary<Vector2Int, Dictionary<Vector3Int, BlockType>> _chunkEdits = new Dictionary<Vector2Int, Dictionary<Vector3Int, BlockType>>();
    // Meshing mode: per-chunk set of plant cells removed by the player (world-space plant cell = above grass)
    private readonly Dictionary<Vector2Int, HashSet<Vector3Int>> _removedBatchedPlants = new Dictionary<Vector2Int, HashSet<Vector3Int>>();

    // Save DTOs (local to avoid cross-file dependency)
    [System.Serializable]
    private class ChangedCellDTO { public int x; public int y; public int z; public int t; }
    [System.Serializable]
    private class ChunkSaveDTO { public int cx; public int cz; public int sizeX; public int sizeY; public int sizeZ; public ChangedCellDTO[] changes; }

    
    void Start()
    {
    // Try to find TextureVariationManager if already configured in the scene (optional)
    textureManager = GetComponent<TextureVariationManager>();
        
        LoadTextures();
        CreateBlockMaterials();
        if (useChunkStreaming)
        {
            EnsureChunksRoot();
            AutoFindPlayer();
            UpdateStreaming(force: true);
            if (delayPlayerSpawnUntilChunks)
            {
                StartCoroutine(StartupPlayerGate());
            }
        }
        else
        {
            GenerateWorld();
        }
    }
    
    void Update()
    {
        // Tick scheduler processing
        _tickAccum += Time.deltaTime;
        while (_tickAccum >= tickIntervalSeconds)
        {
            _tickAccum -= tickIntervalSeconds;
            _currentTick++;
            ProcessTick();
        }

        if (useChunkStreaming)
        {
            UpdateStreaming();
            // Start background chunk loads (limited per frame)
            int budget = Mathf.Max(1, maxChunkLoadsPerFrame);
            while (budget-- > 0 && _pendingLoads.Count > 0)
            {
                var next = _pendingLoads.Dequeue();
                _queued.Remove(next);
                if (_chunks.ContainsKey(next) || _loading.Contains(next)) continue;
                // Mark as loading before starting coroutine to avoid races
                _loading.Add(next);
                StartCoroutine(LoadChunkRoutine(next));
            }
        }
    }
    
    
    void LoadTextures()
    {
        // Load textures from Resources folder if not assigned
        if (grassTexture == null) grassTexture = Resources.Load<Texture2D>("Textures/top_grass");
    if (grassSideTexture == null) grassSideTexture = Resources.Load<Texture2D>("Textures/side_grass");
    if (dirtTexture == null) dirtTexture = Resources.Load<Texture2D>("Textures/dirt");
    if (stoneTexture == null) stoneTexture = Resources.Load<Texture2D>("Textures/stone");
    if (sandTexture == null) sandTexture = Resources.Load<Texture2D>("Textures/sand");
    if (coalTexture == null) coalTexture = Resources.Load<Texture2D>("Textures/coal");

        // Configure plant textures from PlantDatabase (set via ScriptableObjects)
        if (plantDatabase != null)
        {
            var dbType = plantDatabase.GetType();
            var plantsField = dbType.GetField("plants");
            if (plantsField != null)
            {
                var arr = plantsField.GetValue(plantDatabase) as System.Array;
                if (arr != null)
                {
                    foreach (var def in arr)
                    {
                        if (def == null) continue;
                        var tex = def.GetType().GetField("texture")?.GetValue(def) as Texture2D;
                        if (tex != null) ConfigureTexture(tex);
                    }
                }
            }
        }

#if UNITY_EDITOR
    // Editor-only fallback to load directly from Assets/Textures if not found in Resources
    if (grassTexture == null) grassTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/top_grass.png");
    if (grassSideTexture == null) grassSideTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/side_grass.png");
    if (dirtTexture == null) dirtTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/dirt.png");
    if (stoneTexture == null) stoneTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/stone.png");
    if (sandTexture == null) sandTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sand.png");
    if (coalTexture == null) coalTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/coal.png");

    // Plants are assigned via PlantDatabase assets in the editor.
#endif
        
        // Configure textures for better pixel art rendering
        ConfigureTexture(grassTexture);
    ConfigureTexture(grassSideTexture);
        ConfigureTexture(dirtTexture);
        ConfigureTexture(stoneTexture);
        ConfigureTexture(sandTexture);
        ConfigureTexture(coalTexture);
    // Plant textures are configured via PlantDatabase above
        
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
                    mat.SetTexture("_BaseMap", BlockDatabase.blockTypes[i].blockTexture);
                    mat.color = Color.white; // Use white to show texture correctly
                }
                else
                {
                    mat.color = BlockDatabase.blockTypes[i].blockColor;
                }
                
                // Configure material properties for pixel art and reduced tiling
                mat.SetFloat("_Smoothness", 0.0f); // No glossiness
                mat.SetFloat("_Metallic", 0.0f);   // Not metallic
                // Two-sided so we see the underside faces when looking down into holes
                mat.SetFloat("_Cull", 0f);
                
                mat.name = BlockDatabase.blockTypes[i].blockName + "Material";
                blockMaterials[i] = mat;
                BlockDatabase.blockTypes[i].blockMaterial = mat;
            }
        }

        // Build a dedicated grass side material if we have a side texture
        if (grassSideTexture != null)
        {
            _grassSideMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _grassSideMaterial.name = "GrassSideMaterial";
            _grassSideMaterial.mainTexture = grassSideTexture;
            _grassSideMaterial.SetTexture("_BaseMap", grassSideTexture);
            _grassSideMaterial.color = Color.white;
            _grassSideMaterial.SetFloat("_Smoothness", 0.0f);
            _grassSideMaterial.SetFloat("_Metallic", 0.0f);
            _grassSideMaterial.SetFloat("_Cull", 0f);

            // Overlay variant (alpha-clipped, unlit-like) to layer over dirt base
            _grassSideOverlayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _grassSideOverlayMaterial.name = "GrassSideOverlay";
            _grassSideOverlayMaterial.mainTexture = grassSideTexture;
            _grassSideOverlayMaterial.SetTexture("_BaseMap", grassSideTexture);
            _grassSideOverlayMaterial.color = Color.white;
            _grassSideOverlayMaterial.SetFloat("_Cull", 0f);
            _grassSideOverlayMaterial.SetFloat("_AlphaClip", 1f);
            _grassSideOverlayMaterial.SetFloat("_Cutoff", 0.5f);
            _grassSideOverlayMaterial.EnableKeyword("_ALPHATEST_ON");
            _grassSideOverlayMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest; // after opaque dirt
        }
    }

    // Public accessor for chunk mesh builder
    public Material GetBlockMaterial(BlockType t)
    {
        return blockMaterials != null ? blockMaterials[(int)t] : null;
    }

    // Returns the material to use for a particular face of a block.
    // faceIndex matches ChunkMeshBuilder's order: 0=+X,1=-X,2=+Y(top),3=-Y(bottom),4=+Z,5=-Z
    public Material GetFaceMaterial(BlockType t, int faceIndex)
    {
        if (t != BlockType.Grass)
        {
            return GetBlockMaterial(t);
        }
        // Grass: top uses grassTexture, bottom uses dirt, sides use grassSide
        if (faceIndex == 2) // +Y top
        {
            return GetBlockMaterial(BlockType.Grass);
        }
        if (faceIndex == 3) // -Y bottom
        {
            return GetBlockMaterial(BlockType.Dirt);
        }
        // sides
        if (_grassSideMaterial != null)
        {
            return _grassSideMaterial;
        }
        // Fallback to grass if no side texture available
        return GetBlockMaterial(BlockType.Grass);
    }

    // Exposed for mesh builder overlay composition
    public Material GetGrassSideOverlayMaterial()
    {
        return _grassSideOverlayMaterial;
    }
    
    void GenerateWorld()
    {
        worldData = new BlockType[worldWidth, worldHeight, worldDepth];

        if (generateSuperflat)
        {
            // Clamp layers to available height
            int stone = Mathf.Max(0, flatStoneLayers);
            int dirt = Mathf.Max(0, flatDirtLayers);
            int grass = Mathf.Max(0, flatGrassLayers);
            int totalLayers = Mathf.Min(worldHeight, stone + dirt + grass);
            int grassStart = Mathf.Max(0, stone + dirt);

            for (int x = 0; x < worldWidth; x++)
            {
                for (int z = 0; z < worldDepth; z++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        BlockType blockType = BlockType.Air;
                        if (y < stone)
                            blockType = BlockType.Stone; // acting as bedrock for now
                        else if (y < stone + dirt)
                            blockType = BlockType.Dirt;
                        else if (y < totalLayers) // up to grass layers
                            blockType = BlockType.Grass;
                        else
                            blockType = BlockType.Air;

                        worldData[x, y, z] = blockType;
                    }
                }
            }
        }
        else
        {
            // Legacy simple stacked layers (kept for reference/testing)
            for (int x = 0; x < worldWidth; x++)
            {
                for (int z = 0; z < worldDepth; z++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        BlockType blockType = BlockType.Air;
                        if (y == 0) blockType = BlockType.Stone; // Bedrock substitute
                        else if (y < 3) blockType = BlockType.Stone;
                        else if (y < 6) blockType = BlockType.Dirt;
                        else if (y == 6) blockType = BlockType.Grass;
                        worldData[x, y, z] = blockType;
                    }
                }
            }
        }

        // Create visible blocks and spawn plants
        UpdateVisibleBlocks();
        SpawnPlants();
    }

    // Determine block type at a given world position according to current generation settings
    public BlockType GenerateBlockTypeAt(Vector3Int worldPos)
    {
        if (!generateSuperflat)
        {
            // Legacy stack based on Y only (mirrors previous non-superflat path)
            if (worldPos.y == 0) return BlockType.Stone;
            if (worldPos.y < 3) return BlockType.Stone;
            if (worldPos.y < 6) return BlockType.Dirt;
            if (worldPos.y == 6) return BlockType.Grass;
            return BlockType.Air;
        }

        int stone = Mathf.Max(0, flatStoneLayers);
        int dirt = Mathf.Max(0, flatDirtLayers);
        int grass = Mathf.Max(0, flatGrassLayers);
        int totalLayers = stone + dirt + grass;
        if (worldPos.y < 0 || worldPos.y >= worldHeight) return BlockType.Air;
        if (worldPos.y < stone) return BlockType.Stone;
        if (worldPos.y < stone + dirt) return BlockType.Dirt;
        if (worldPos.y < totalLayers) return BlockType.Grass;
        return BlockType.Air;
    }

    // --- Chunk streaming helpers ---
    private void EnsureChunksRoot()
    {
        if (_chunksRoot == null)
        {
            var go = new GameObject("Chunks");
            go.transform.SetParent(this.transform, false);
            _chunksRoot = go.transform;
        }
    }

    private void AutoFindPlayer()
    {
        if (player != null) return;
        var fpc = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
        if (fpc != null) { player = fpc.transform; return; }
        var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        if (pc != null) { player = pc.transform; return; }
        var cam = Camera.main; if (cam != null) player = cam.transform;
    }

    private Vector2Int WorldToChunkCoord(Vector3 position)
    {
        int cx = Mathf.FloorToInt(position.x / Mathf.Max(1, chunkSizeX));
        int cz = Mathf.FloorToInt(position.z / Mathf.Max(1, chunkSizeZ));
        return new Vector2Int(cx, cz);
    }

    private Vector2Int WorldToChunkCoord(Vector3Int wpos)
    {
        return new Vector2Int(Mathf.FloorToInt((float)wpos.x / Mathf.Max(1, chunkSizeX)), Mathf.FloorToInt((float)wpos.z / Mathf.Max(1, chunkSizeZ)));
    }

    private void UpdateStreaming(bool force = false)
    {
        if (player == null) return;
        EnsureChunksRoot();

        var center = WorldToChunkCoord(player.position);
        int r = Mathf.Max(1, viewDistanceChunks);

        // Determine required chunk coords in a square region
        var needed = new HashSet<Vector2Int>();
        for (int dz = -r; dz <= r; dz++)
        {
            int rem = r - Mathf.Abs(dz);
            for (int dx = -rem; dx <= rem; dx++)
            {
                needed.Add(new Vector2Int(center.x + dx, center.y + dz));
            }
        }

        // Unload chunks not needed
        var toUnload = new List<Vector2Int>();
        foreach (var kv in _chunks)
        {
            if (!needed.Contains(kv.Key)) toUnload.Add(kv.Key);
        }
        foreach (var c in toUnload)
        {
            // Persist edits before unloading
            if (enableChunkPersistence) SaveChunkToDisk(c);
            _chunks[c].Unload(this);
            _chunks.Remove(c);
        }

        // Enqueue missing chunks for background loading
        foreach (var c in needed)
        {
            if (_chunks.ContainsKey(c) || _loading.Contains(c) || _queued.Contains(c)) continue;
            _pendingLoads.Enqueue(c);
            _queued.Add(c);
        }
    }

    private IEnumerator LoadChunkRoutine(Vector2Int coord)
    {
    // Create chunk and generate data with small yields to avoid spikes
        var chunk = new WorldGeneration.Chunks.Chunk(coord, chunkSizeX, worldHeight, chunkSizeZ, _chunksRoot);

        // Generate data with coarse yields: iterate Y up to useful layers only
        int stone = Mathf.Max(0, flatStoneLayers);
        int dirt = Mathf.Max(0, flatDirtLayers);
        int grass = Mathf.Max(0, flatGrassLayers);
        int usefulY = Mathf.Min(worldHeight, stone + dirt + grass);
        usefulY = Mathf.Max(1, usefulY);

        for (int lx = 0; lx < chunkSizeX; lx++)
        {
            for (int lz = 0; lz < chunkSizeZ; lz++)
            {
                for (int ly = 0; ly < usefulY; ly++)
                {
                    var wp = new Vector3Int(coord.x * chunkSizeX + lx, ly, coord.y * chunkSizeZ + lz);
                    var t = GenerateBlockTypeAt(wp);
                    chunk.SetLocal(lx, ly, lz, t);
                }
            }
            if ((lx & 3) == 0) yield return null; // yield every few columns
        }

        // Apply persisted edits (disk then memory)
        if (enableChunkPersistence)
        {
            LoadChunkFromDiskInto(coord, chunk);
            if (_chunkEdits.TryGetValue(coord, out var edits))
            {
                foreach (var kv in edits)
                {
                    var lp = chunk.WorldToLocal(kv.Key);
                    if (lp.x >= 0 && lp.x < chunk.sizeX && lp.y >= 0 && lp.y < chunk.sizeY && lp.z >= 0 && lp.z < chunk.sizeZ)
                    {
                        chunk.SetLocal(lp.x, lp.y, lp.z, kv.Value);
                    }
                }
            }
        }

    // Register chunk early so GetBlockType works during spawning logic
    _chunks[coord] = chunk;

    if (useChunkMeshing)
        {
            // Use reflection to avoid compile-order/type resolution issues
            var chunkType = ResolveTypeByName("WorldGeneration.Chunks.Chunk");
            var builderType = ResolveTypeByName("WorldGeneration.Chunks.ChunkMeshBuilder");
            if (builderType != null)
            {
                var build = builderType.GetMethod(
                    "BuildMesh",
                    new System.Type[] { typeof(WorldGenerator), chunkType, typeof(bool) }
                );
                if (build != null)
                {
                    build.Invoke(null, new object[] { this, chunk, addChunkCollider });
                }
            }

            // Spawn plants in batched renderer for very lightweight loads
            BuildChunkPlants(chunk, usefulY);
        }
        else
        {
            // Build visible with yields (GameObject per block)
            for (int lx = 0; lx < chunkSizeX; lx++)
            {
                for (int ly = 0; ly < usefulY; ly++)
                {
                    for (int lz = 0; lz < chunkSizeZ; lz++)
                    {
                        var t = chunk.GetLocal(lx, ly, lz);
                        if (t == BlockType.Air) continue;
                        var wp = new Vector3Int(coord.x * chunkSizeX + lx, ly, coord.y * chunkSizeZ + lz);
                        if (ShouldRenderBlock(wp.x, wp.y, wp.z))
                        {
                            CreateBlock(wp, t, chunk.parent);
                        }
                    }
                }
                if ((lx & 3) == 0) yield return null; // yield periodically
            }

            // Spawn plants on exposed grass; coarse pass with yields
            for (int lx = 0; lx < chunkSizeX; lx++)
            {
                for (int lz = 0; lz < chunkSizeZ; lz++)
                {
                    for (int ly = 1; ly < usefulY; ly++)
                    {
                        var wp = new Vector3Int(coord.x * chunkSizeX + lx, ly, coord.y * chunkSizeZ + lz);
                        if (GetBlockType(wp) == BlockType.Grass && GetBlockType(wp + Vector3Int.up) == BlockType.Air)
                        {
                            if (ShouldRenderBlock(wp.x, wp.y, wp.z))
                            {
                                TrySpawnPlantClusterAt(wp, chunk.parent);
                            }
                        }
                    }
                }
                if ((lx & 3) == 0) yield return null;
            }
        }

    _loading.Remove(coord);
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
                            if (HasAnyPlants())
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
                    // Default: stand just above the top face of the grass block
                    Vector3 placePos = new Vector3(x + 0.5f, y + 1.001f, z + 0.5f);
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
                                    // Use helper with definition from PlantDatabase via reflection
                                    var def = PlantDB_PickByWeight(rng);
                                    var tex = PlantDef_GetTexture(def);
                                    if (tex != null)
                                    {
                                        var hr = PlantDef_GetHeightRange(def);
                                        var w = PlantDef_GetWidth(def);
                                        var yo = PlantDef_GetYOffset(def);
                                        SpawnPlantChildrenIntoParent(parent, new Vector3Int(x, y, z), count, rng, tex, hr, w, yo);
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

    public bool HasAnyPlants()
    {
        return PlantDB_HasAny();
    }
    
    public bool ShouldRenderBlock(int x, int y, int z)
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
        if (useChunkStreaming)
        {
            // Infinite world horizontally; only clamp vertically
            return pos.y < 0 || pos.y >= worldHeight;
        }
        else
        {
            return pos.x < 0 || pos.x >= worldWidth ||
                   pos.y < 0 || pos.y >= worldHeight ||
                   pos.z < 0 || pos.z >= worldDepth;
        }
    }
    
    public void CreateBlock(Vector3Int position, BlockType blockType)
    {
        if (blockPrefab == null) return;

        if (blockType == BlockType.Grass)
        {
            // Grass block is special: create a parent and then individual faces
            GameObject grassBlockParent = new GameObject($"{BlockDatabase.GetBlockData(blockType).blockName} ({position.x},{position.y},{position.z})");
            grassBlockParent.transform.position = position;
            grassBlockParent.transform.parent = transform;

            // Define faces: 0=up, 1=down, 2=left, 3=right, 4=forward, 5=back
            Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            Material[] faceMaterials = {
                GetBlockMaterial(BlockType.Grass), // Top
                GetBlockMaterial(BlockType.Dirt),  // Bottom
                GetBlockMaterial(BlockType.Dirt), // Side
                GetBlockMaterial(BlockType.Dirt), // Side
                GetBlockMaterial(BlockType.Dirt), // Side
                GetBlockMaterial(BlockType.Dirt)  // Side
            };
            Texture2D[] faceTextures = {
                grassTexture,
                dirtTexture,
                grassSideTexture,
                grassSideTexture,
                grassSideTexture,
                grassSideTexture
            };

            for (int i = 0; i < 6; i++)
            {
                Vector3Int neighborPos = position + Vector3Int.RoundToInt(faceNormals[i]);
                if (IsOutOfBounds(neighborPos) || GetBlockType(neighborPos) == BlockType.Air)
                {
                    GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    face.transform.SetParent(grassBlockParent.transform, false);
                    face.transform.position = position + faceNormals[i] * 0.5f;
                    face.transform.rotation = Quaternion.LookRotation(-faceNormals[i]);
                    
                    Renderer faceRenderer = face.GetComponent<Renderer>();
                    if (faceRenderer != null)
                    {
                        Material mat = CreateVariationMaterial(faceMaterials[i], position);
                        mat.mainTexture = faceTextures[i];
                        mat.SetTexture("_BaseMap", faceTextures[i]);
                        faceRenderer.material = mat;
                    }
                    Destroy(face.GetComponent<Collider>());
                }
            }
            blockObjects[position] = grassBlockParent;
        }
        else
        {
            GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
            block.name = $"{BlockDatabase.GetBlockData(blockType).blockName} ({position.x},{position.y},{position.z})";
            
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material baseMaterial = blockMaterials[(int)blockType];
                if (baseMaterial != null)
                {
                    Material instanceMaterial = CreateVariationMaterial(baseMaterial, position);
                    renderer.material = instanceMaterial;
                }
            }
            blockObjects[position] = block;
        }

        // Add BlockInfo component to the root block object
        GameObject rootObject = blockObjects[position];
        BlockInfo blockInfo = rootObject.GetComponent<BlockInfo>();
        if (blockInfo == null)
        {
            blockInfo = rootObject.AddComponent<BlockInfo>();
        }
        blockInfo.blockType = blockType;
        blockInfo.position = position;
    }

    // Overload to allow parenting under a chunk parent
    public void CreateBlock(Vector3Int position, BlockType blockType, Transform parentOverride)
    {
        if (blockPrefab == null) return;

        Transform p = parentOverride != null ? parentOverride : this.transform;
        
        if (blockType == BlockType.Grass)
        {
            GameObject grassBlockParent = new GameObject($"{BlockDatabase.GetBlockData(blockType).blockName} ({position.x},{position.y},{position.z})");
            grassBlockParent.transform.position = position;
            grassBlockParent.transform.SetParent(p, true);

            Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            Material[] faceMaterials = {
                GetBlockMaterial(BlockType.Grass), // Top
                GetBlockMaterial(BlockType.Dirt),  // Bottom
                GetBlockMaterial(BlockType.Dirt), // Side
                GetBlockMaterial(BlockType.Dirt), // Side
                GetBlockMaterial(BlockType.Dirt), // Side
                GetBlockMaterial(BlockType.Dirt)  // Side
            };
            Texture2D[] faceTextures = {
                grassTexture,
                dirtTexture,
                grassSideTexture,
                grassSideTexture,
                grassSideTexture,
                grassSideTexture
            };

            for (int i = 0; i < 6; i++)
            {
                 Vector3Int neighborPos = position + Vector3Int.RoundToInt(faceNormals[i]);
                if (IsOutOfBounds(neighborPos) || GetBlockType(neighborPos) == BlockType.Air)
                {
                    GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    face.transform.SetParent(grassBlockParent.transform, false);
                    face.transform.position = position + faceNormals[i] * 0.5f;
                    face.transform.rotation = Quaternion.LookRotation(-faceNormals[i]);
                    
                    Renderer faceRenderer = face.GetComponent<Renderer>();
                    if (faceRenderer != null)
                    {
                        Material mat = CreateVariationMaterial(faceMaterials[i], position);
                        mat.mainTexture = faceTextures[i];
                        mat.SetTexture("_BaseMap", faceTextures[i]);
                        faceRenderer.material = mat;
                    }
                    Destroy(face.GetComponent<Collider>());
                }
            }
            blockObjects[position] = grassBlockParent;
        }
        else
        {
            GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, p);
            block.name = $"{BlockDatabase.GetBlockData(blockType).blockName} ({position.x},{position.y},{position.z})";

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material baseMaterial = blockMaterials[(int)blockType];
                if (baseMaterial != null)
                {
                    Material instanceMaterial = CreateVariationMaterial(baseMaterial, position);
                    renderer.material = instanceMaterial;
                }
            }
            blockObjects[position] = block;
        }

        // Add BlockInfo component
        GameObject rootObject = blockObjects[position];
        BlockInfo blockInfo = rootObject.GetComponent<BlockInfo>();
        if (blockInfo == null)
        {
            blockInfo = rootObject.AddComponent<BlockInfo>();
        }
        blockInfo.blockType = blockType;
        blockInfo.position = position;
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
        
        // The color is already set on the base material, so no need to change it
        // float colorVariation = 0.95f + (float)posRandom.NextDouble() * 0.1f; // 95% to 105%
        // Color baseColor = baseMaterial.color;
        // instanceMaterial.color = new Color(
        //     baseColor.r * colorVariation,
        //     baseColor.g * colorVariation,
        //     baseColor.b * colorVariation,
        //     baseColor.a
        // );
        
        return instanceMaterial;
    }
    
    public BlockType GetBlockType(Vector3Int position)
    {
        if (useChunkStreaming)
        {
            if (IsOutOfBounds(position)) return BlockType.Air;
            var cc = WorldToChunkCoord(position);
            if (_chunks.TryGetValue(cc, out var chunk))
            {
                var lp = chunk.WorldToLocal(position);
                return chunk.GetLocal(lp.x, lp.y, lp.z);
            }
            // Not loaded: treat as Air for visibility; if needed, could compute procedural type
            return BlockType.Air;
        }
        else
        {
            if (IsOutOfBounds(position)) return BlockType.Air;
            return worldData[position.x, position.y, position.z];
        }
    }
    
    public bool PlaceBlock(Vector3Int position, BlockType blockType)
    {
        if (IsOutOfBounds(position)) return false;

        if (useChunkStreaming)
        {
            var cc = WorldToChunkCoord(position);
            if (!_chunks.TryGetValue(cc, out var chunk))
            {
                // Load the chunk on-demand
                chunk = new WorldGeneration.Chunks.Chunk(cc, chunkSizeX, worldHeight, chunkSizeZ, _chunksRoot);
                chunk.GenerateSuperflat(this);
                _chunks[cc] = chunk;
            }
            var lp = chunk.WorldToLocal(position);
            chunk.SetLocal(lp.x, lp.y, lp.z, blockType);
            if (enableChunkPersistence)
            {
                if (!_chunkEdits.TryGetValue(cc, out var map)) { map = new Dictionary<Vector3Int, BlockType>(); _chunkEdits[cc] = map; }
                map[position] = blockType;
            }
        }
        else
        {
            worldData[position.x, position.y, position.z] = blockType;
        }

        // If a block is placed where a plant currently exists, remove the plant at that cell
        if (blockType != BlockType.Air)
        {
            // Non-meshing: legacy GameObject plants
            if (plantObjects.TryGetValue(position, out var plantAtPos))
            {
                if (plantAtPos != null) Destroy(plantAtPos);
                plantObjects.Remove(position);
            }
            // Meshing: remove batched plant at this cell if present
            if (useChunkStreaming && useChunkMeshing)
            {
                if (HasBatchedPlantAt(position))
                {
                    RemoveBatchedPlantAt(position);
                }
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
            if (useChunkStreaming && useChunkMeshing)
            {
                if (HasBatchedPlantAt(above))
                {
                    RemoveBatchedPlantAt(above);
                }
            }
        }

    // If placing a non-air block and the block below is Grass, convert it to Dirt
        if (blockType != BlockType.Air)
        {
            var below = position + Vector3Int.down;
            if (!IsOutOfBounds(below) && GetBlockType(below) == BlockType.Grass)
            {
                if (useChunkStreaming)
                {
                    var ccBelow = WorldToChunkCoord(below);
                    if (_chunks.TryGetValue(ccBelow, out var chBelow))
                    {
                        var lpBelow = chBelow.WorldToLocal(below);
                        chBelow.SetLocal(lpBelow.x, lpBelow.y, lpBelow.z, BlockType.Dirt);
                        if (enableChunkPersistence)
                        {
                            var ccb = WorldToChunkCoord(below);
                            if (!_chunkEdits.TryGetValue(ccb, out var mapBelow)) { mapBelow = new Dictionary<Vector3Int, BlockType>(); _chunkEdits[ccb] = mapBelow; }
                            mapBelow[below] = BlockType.Dirt;
                        }
                    }
                }
                else
                {
                    worldData[below.x, below.y, below.z] = BlockType.Dirt;
                }
                // Remove any plant above that grass cell
                var aboveBelow = below + Vector3Int.up;
                if (plantObjects.TryGetValue(aboveBelow, out var plantAtAboveBelow))
                {
                    if (plantAtAboveBelow != null) Destroy(plantAtAboveBelow);
                    plantObjects.Remove(aboveBelow);
                }
                if (useChunkStreaming && useChunkMeshing)
                {
                    if (HasBatchedPlantAt(aboveBelow))
                    {
                        RemoveBatchedPlantAt(aboveBelow);
                    }
                }
                _scheduledPlantCells.Remove(below);
                // Refresh visuals for the converted block
                if (!useChunkStreaming || !useChunkMeshing)
                {
                    if (blockObjects.ContainsKey(below))
                    {
                        Destroy(blockObjects[below]);
                        blockObjects.Remove(below);
                    }
                    CreateBlock(below, BlockType.Dirt);
                }
            }
            // If covering exposed Dirt, cancel growth
            if (!IsOutOfBounds(below) && GetBlockType(below) == BlockType.Dirt)
            {
                _scheduledGrassCells.Remove(below);
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
            // If removing a block exposes Dirt below, schedule grass growth
            var belowIfAny = position + Vector3Int.down;
            if (!IsOutOfBounds(belowIfAny) && GetBlockType(belowIfAny) == BlockType.Dirt)
            {
                // The cell we just made Air exposes 'belowIfAny' to sky
                if (GetBlockType(position) == BlockType.Air)
                {
                    ScheduleGrassGrowth(belowIfAny, dirtToGrassDelayTicks);
                }
            }
        }
        else
        {
            // Place block
            if (useChunkStreaming)
            {
                if (!useChunkMeshing)
                {
                    var cc = WorldToChunkCoord(position);
                    var parent = _chunks.TryGetValue(cc, out var ch) ? ch.parent : this.transform;
                    CreateBlock(position, blockType, parent);
                }
                // If meshing, we'll rebuild chunk mesh below
            }
            else
            {
                CreateBlock(position, blockType);
            }
        }

        // If we placed Grass with Air above, schedule delayed plant spawn at this position
    if (blockType == BlockType.Grass)
        {
            if (GetBlockType(above) == BlockType.Air)
            {
                SchedulePlantRespawn(position, newGrassPlantDelayTicks);
            }
        }
        // Side neighbors: if any Dirt neighbor is now exposed (air above it), schedule growth
        if (blockType != BlockType.Air)
        {
            Vector3Int[] lateral = { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
            foreach (var d in lateral)
            {
                var n = position + d;
                if (!IsOutOfBounds(n) && GetBlockType(n) == BlockType.Dirt && GetBlockType(n + Vector3Int.up) == BlockType.Air)
                {
                    ScheduleGrassGrowth(n, dirtToGrassDelayTicks);
                }
            }
        }
        // If we placed Dirt with Air above, schedule grass growth for this cell
        if (blockType == BlockType.Dirt)
        {
            if (GetBlockType(above) == BlockType.Air)
            {
                ScheduleGrassGrowth(position, dirtToGrassDelayTicks);
            }
        }
        
    // Update neighboring blocks visibility / rebuild meshes as needed
    UpdateNeighboringBlocks(position);
        
        
        return true;
    }

    private void OnApplicationQuit()
    {
        if (!enableChunkPersistence) return;
        // Save every loaded chunk's edits to disk
        foreach (var kv in _chunks)
        {
            SaveChunkToDisk(kv.Key);
        }
    }
    
    void UpdateNeighboringBlocks(Vector3Int position)
    {
        if (useChunkStreaming)
        {
            if (useChunkMeshing)
            {
                // Rebuild the chunk containing this cell and any neighboring chunk across boundaries
                var cc = WorldToChunkCoord(position);
                RebuildChunkMeshAt(cc);
                Vector2Int[] neighborOffsets = { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };
                foreach (var off in neighborOffsets)
                {
                    var nwp = position + new Vector3Int(off.x * chunkSizeX, 0, off.y * chunkSizeZ);
                    var ncc = WorldToChunkCoord(nwp);
                    if (ncc != cc) RebuildChunkMeshAt(ncc);
                }
                return;
            }
            // Streaming without meshing: operate on per-block GameObjects under chunk parents
        }

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
                BlockType blockType = GetBlockType(neighbor);
                
                if (blockType != BlockType.Air)
                {
                    if (ShouldRenderBlock(neighbor.x, neighbor.y, neighbor.z))
                    {
                        if (!blockObjects.ContainsKey(neighbor))
                        {
                            if (useChunkStreaming)
                            {
                                var cc = WorldToChunkCoord(neighbor);
                                var parent = _chunks.TryGetValue(cc, out var ch) ? ch.parent : this.transform;
                                CreateBlock(neighbor, blockType, parent);
                            }
                            else
                            {
                                CreateBlock(neighbor, blockType);
                            }
                        }
                        // If this is grass with air above, schedule a delayed plant spawn (on-demand)
                        if (blockType == BlockType.Grass && GetBlockType(neighbor + Vector3Int.up) == BlockType.Air)
                        {
                            SchedulePlantRespawn(neighbor, newGrassPlantDelayTicks);
                        }
                        // If this is dirt with air above, schedule grass growth
                        if (blockType == BlockType.Dirt && GetBlockType(neighbor + Vector3Int.up) == BlockType.Air)
                        {
                            ScheduleGrassGrowth(neighbor, dirtToGrassDelayTicks);
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

    private void RebuildChunkMeshAt(Vector2Int coord)
    {
        if (!_chunks.TryGetValue(coord, out var chunk)) return;
        var builderType = ResolveTypeByName("WorldGeneration.Chunks.ChunkMeshBuilder");
        var chunkType = ResolveTypeByName("WorldGeneration.Chunks.Chunk");
        if (builderType == null || chunkType == null) return;
        var build = builderType.GetMethod("BuildMesh", new System.Type[] { typeof(WorldGenerator), chunkType, typeof(bool) });
        if (build == null) return;
        build.Invoke(null, new object[] { this, chunk, addChunkCollider });

        // After rebuilding block mesh, immediately rebuild batched plants so removals/additions are reflected without delay
        if (useChunkMeshing && HasAnyPlants())
        {
            int stone = Mathf.Max(0, flatStoneLayers);
            int dirt = Mathf.Max(0, flatDirtLayers);
            int grass = Mathf.Max(0, flatGrassLayers);
            int usefulY = Mathf.Min(worldHeight, stone + dirt + grass);
            usefulY = Mathf.Max(1, usefulY);
            BuildChunkPlants(chunk, usefulY);
        }
    }

    // --- Startup player gate ---
    private System.Collections.IEnumerator StartupPlayerGate()
    {
        // If no player, try to find again; if still none, there's nothing to gate.
        if (player == null) AutoFindPlayer();
        if (player == null) yield break;

        // Compute center chunk from current player position before disabling them
        var center = WorldToChunkCoord(player.position);

        // Disable player GameObject during initial chunk build
        var playerGO = player.gameObject;
        bool wasActive = playerGO.activeSelf;
        if (wasActive) playerGO.SetActive(false);

        // Wait until the center chunk is loaded and meshed
        while (true)
        {
            if (_chunks.TryGetValue(center, out var ch) && ch != null && ch.parent != null)
            {
                var mf = ch.parent.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    break;
                }
            }
            yield return null;
        }

        // Optionally snap player to ground
        if (snapPlayerToGroundOnSpawn)
        {
            var p = player.position;
            int x = Mathf.FloorToInt(p.x);
            int z = Mathf.FloorToInt(p.z);
            int gy = FindHighestSolidYAt(x, z);
            if (gy >= 0)
            {
                // place a bit above ground to avoid collisions
                player.position = new Vector3(p.x, gy + 1.6f, p.z);
            }
        }

        if (wasActive) playerGO.SetActive(true);
    }

    private int FindHighestSolidYAt(int x, int z)
    {
        for (int y = worldHeight - 1; y >= 0; y--)
        {
            if (GetBlockType(new Vector3Int(x, y, z)) != BlockType.Air)
                return y;
        }
        // Fallback to superflat ground if none found
        if (generateSuperflat)
        {
            int stone = Mathf.Max(0, flatStoneLayers);
            int dirt = Mathf.Max(0, flatDirtLayers);
            int grass = Mathf.Max(0, flatGrassLayers);
            int totalLayers = Mathf.Min(worldHeight, stone + dirt + grass);
            return totalLayers - 1;
        }
        return -1;
    }

    // Build batched plants for a meshed chunk to keep loads lightweight
    private void BuildChunkPlants(WorldGeneration.Chunks.Chunk chunk, int usefulY)
    {
        if (!HasAnyPlants() || chunk == null || chunk.parent == null) return;

        // Ensure renderer component exists on chunk parent
        var pcr = chunk.parent.GetComponent<PlantChunkRenderer>();
        if (pcr == null) pcr = chunk.parent.gameObject.AddComponent<PlantChunkRenderer>();
        pcr.chunkCoord = chunk.coord;
        pcr.chunkSizeX = chunk.sizeX;
        pcr.chunkSizeZ = chunk.sizeZ;
        pcr.worldHeight = worldHeight;
        pcr.Clear();

        for (int lx = 0; lx < chunk.sizeX; lx++)
        {
            for (int lz = 0; lz < chunk.sizeZ; lz++)
            {
                for (int ly = 1; ly < usefulY; ly++)
                {
                    var t = chunk.GetLocal(lx, ly, lz);
                    var tAbove = chunk.GetLocal(lx, ly + 1, lz);
                    if (t != BlockType.Grass || tAbove != BlockType.Air) continue;
                    // Determine deterministic world-space grass cell and plant cell
                    var worldGrass = chunk.LocalToWorld(new Vector3Int(lx, ly, lz));
                    var plantCell = worldGrass + Vector3Int.up;

                    // Skip if this plant cell has been removed by the player
                    var cc = chunk.coord;
                    if (_removedBatchedPlants.TryGetValue(cc, out var removedSet) && removedSet.Contains(plantCell))
                        continue;

                    // Deterministic count per cell
                    int count = CountBatchedPlantsAtCell(worldGrass);
                    if (count <= 0) continue;

                    // Per-cell deterministic RNG for visuals/definition pick
                    int seed = worldGrass.x * 73856093 ^ worldGrass.y * 19349663 ^ worldGrass.z * 83492791;
                    var rng = new System.Random(seed);

                    // Pick a definition and its texture and params via reflection helpers
                    var def = PlantDB_PickByWeight(rng);
                    var tex = PlantDef_GetTexture(def);
                    if (tex == null) continue;
                    var hr = PlantDef_GetHeightRange(def);
                    var w = PlantDef_GetWidth(def);
                    var yo = PlantDef_GetYOffset(def);

                    // Get shared plant material for this texture
                    var mat = GetPlantSharedMaterial(tex);
                    if (mat == null) continue;

                    // Local position inside chunk parent space
                    var localPos = new Vector3(lx + 0.5f, ly + 1.001f, lz + 0.5f);

                    for (int i = 0; i < count; i++)
                    {
                        pcr.AddPlantCluster(localPos, hr, w, yo, 2, mat, rng);
                    }
                }
            }
        }

        pcr.Flush(this);
    }

    // Return the expected plant instances at a grass cell using deterministic RNG per cell
    private int CountBatchedPlantsAtCell(Vector3Int grassCell)
    {
        if (GetBlockType(grassCell) != BlockType.Grass) return 0;
        if (GetBlockType(grassCell + Vector3Int.up) != BlockType.Air) return 0;
        float desired = Mathf.Max(0f, plantDensity);
        int count = Mathf.FloorToInt(desired);
        double remainder = desired - count;
        int seed = grassCell.x * 73856093 ^ grassCell.y * 19349663 ^ grassCell.z * 83492791;
        var rng = new System.Random(seed);
        if (rng.NextDouble() < remainder) count++;
        return count;
    }

    // Query if a batched plant exists at the given plant cell (world space cell above a grass block)
    public bool HasBatchedPlantAt(Vector3Int plantCell)
    {
        if (!useChunkStreaming || !useChunkMeshing) return false;
        var grassCell = plantCell + Vector3Int.down;
        var cc = WorldToChunkCoord(plantCell);
        if (_removedBatchedPlants.TryGetValue(cc, out var removedSet) && removedSet.Contains(plantCell))
            return false;
        return CountBatchedPlantsAtCell(grassCell) > 0;
    }

    // Remove a batched plant at the given plant cell (no delayed respawn for instant feedback)
    public void RemoveBatchedPlantAt(Vector3Int plantCell)
    {
        if (!useChunkStreaming || !useChunkMeshing) return;
        var grassCell = plantCell + Vector3Int.down;
        var cc = WorldToChunkCoord(plantCell);
        if (!_removedBatchedPlants.TryGetValue(cc, out var removedSet))
        {
            removedSet = new HashSet<Vector3Int>();
            _removedBatchedPlants[cc] = removedSet;
        }
        if (!removedSet.Contains(plantCell)) removedSet.Add(plantCell);

        // Rebuild this chunk's plant batch
        if (_chunks.TryGetValue(cc, out var chunk))
        {
            int stone = Mathf.Max(0, flatStoneLayers);
            int dirt = Mathf.Max(0, flatDirtLayers);
            int grass = Mathf.Max(0, flatGrassLayers);
            int usefulY = Mathf.Min(worldHeight, stone + dirt + grass);
            usefulY = Mathf.Max(1, usefulY);
            BuildChunkPlants(chunk, usefulY);
        }

    // No respawn scheduling: plants reappear only if chunk is regenerated or grass regrows and density logic includes them
    }

    private void SchedulePlantRespawnBatched(Vector3Int grassCell, int delayTicks)
    {
        if (IsOutOfBounds(grassCell)) return;
        if (_scheduledPlantCells.Contains(grassCell)) return;
        _scheduledPlantCells.Add(grassCell);
        _tickQueue.Add(new TickAction { dueTick = _currentTick + Mathf.Max(1, delayTicks), type = TA_PlantRespawn, cell = grassCell });
    }

    // Public helper for PlantBillboard and internal cleanup
    public void RemovePlantAt(Vector3Int cell)
    {
        if (plantObjects.TryGetValue(cell, out var obj))
        {
            if (obj != null) Destroy(obj);
            plantObjects.Remove(cell);
            // Schedule a delayed respawn on the supporting grass cell
            var grassCell = cell + Vector3Int.down;
            SchedulePlantRespawn(grassCell, plantRespawnDelayTicks);
        }
    }

    // ---------- Persistence helpers ----------
    private string GetSaveFolderPath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, saveFolderName);
    }

    private string GetChunkSavePath(Vector2Int coord)
    {
        return System.IO.Path.Combine(GetSaveFolderPath(), $"chunk_{coord.x}_{coord.y}.json");
    }

    private void EnsureSaveFolder()
    {
        var dir = GetSaveFolderPath();
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
    }

    private void SaveChunkToDisk(Vector2Int coord)
    {
        if (!enableChunkPersistence) return;
        if (!_chunkEdits.TryGetValue(coord, out var map) || map.Count == 0) return;
        EnsureSaveFolder();
    var data = new ChunkSaveDTO
        {
            cx = coord.x,
            cz = coord.y,
            sizeX = chunkSizeX,
            sizeY = worldHeight,
            sizeZ = chunkSizeZ,
        changes = new ChangedCellDTO[map.Count]
        };
        int i = 0;
        foreach (var kv in map)
        {
        data.changes[i++] = new ChangedCellDTO { x = kv.Key.x, y = kv.Key.y, z = kv.Key.z, t = (int)kv.Value };
        }
        var json = JsonUtility.ToJson(data, false);
        System.IO.File.WriteAllText(GetChunkSavePath(coord), json);
    }

    private void LoadChunkFromDiskInto(Vector2Int coord, WorldGeneration.Chunks.Chunk chunk)
    {
        if (!enableChunkPersistence) return;
        var path = GetChunkSavePath(coord);
        if (!System.IO.File.Exists(path)) return;
        var json = System.IO.File.ReadAllText(path);
    var data = JsonUtility.FromJson<ChunkSaveDTO>(json);
        if (data?.changes == null) return;
        foreach (var c in data.changes)
        {
            var wp = new Vector3Int(c.x, c.y, c.z);
            var lp = chunk.WorldToLocal(wp);
            if (lp.x >= 0 && lp.x < chunk.sizeX && lp.y >= 0 && lp.y < chunk.sizeY && lp.z >= 0 && lp.z < chunk.sizeZ)
            {
        chunk.SetLocal(lp.x, lp.y, lp.z, (BlockType)c.t);
            }
        }
        if (!_chunkEdits.ContainsKey(coord)) _chunkEdits[coord] = new Dictionary<Vector3Int, BlockType>();
        var map = _chunkEdits[coord];
    foreach (var c in data.changes) map[new Vector3Int(c.x, c.y, c.z)] = (BlockType)c.t;
    }

    // --- Tick scheduling and helpers ---
    public void SchedulePlantRespawn(Vector3Int grassCell, int delayTicks)
    {
        if (IsOutOfBounds(grassCell)) return;
        var above = grassCell + Vector3Int.up;
        if (GetBlockType(grassCell) != BlockType.Grass) return;
        if (GetBlockType(above) != BlockType.Air) return;
        if (plantObjects.TryGetValue(above, out var obj))
        {
            if (obj == null)
            {
                plantObjects.Remove(above);
            }
            else
            {
                return;
            }
        }
        if (_scheduledPlantCells.Contains(grassCell)) return;
        _scheduledPlantCells.Add(grassCell);
        _tickQueue.Add(new TickAction { dueTick = _currentTick + Mathf.Max(1, delayTicks), type = TA_PlantRespawn, cell = grassCell });
    }

    public void ScheduleGrassGrowth(Vector3Int dirtCell, int delayTicks)
    {
        if (IsOutOfBounds(dirtCell)) return;
        var above = dirtCell + Vector3Int.up;
        if (GetBlockType(dirtCell) != BlockType.Dirt) return;
        if (GetBlockType(above) != BlockType.Air) return;
        if (_scheduledGrassCells.Contains(dirtCell)) return;
        _scheduledGrassCells.Add(dirtCell);
        _tickQueue.Add(new TickAction { dueTick = _currentTick + Mathf.Max(1, delayTicks), type = TA_GrassGrow, cell = dirtCell });
    }

    private void ProcessTick()
    {
        if (_tickQueue.Count == 0) return;
        for (int i = _tickQueue.Count - 1; i >= 0; i--)
        {
            var ta = _tickQueue[i];
            if (ta.dueTick > _currentTick) continue;
            _tickQueue.RemoveAt(i);
            if (ta.type == TA_PlantRespawn)
            {
                _scheduledPlantCells.Remove(ta.cell);
                // Batched plants no longer auto-respawn; only legacy per-plant GameObjects do.
                if (!(useChunkStreaming && useChunkMeshing))
                {
                    TrySpawnPlantClusterAt(ta.cell);
                }
            }
            else if (ta.type == TA_GrassGrow)
            {
                _scheduledGrassCells.Remove(ta.cell);
                if (GetBlockType(ta.cell) == BlockType.Dirt && GetBlockType(ta.cell + Vector3Int.up) == BlockType.Air)
                {
                    if (useChunkStreaming)
                    {
                        var cc = WorldToChunkCoord(ta.cell);
                        if (_chunks.TryGetValue(cc, out var ch))
                        {
                            var lp = ch.WorldToLocal(ta.cell);
                            ch.SetLocal(lp.x, lp.y, lp.z, BlockType.Grass);
                            if (enableChunkPersistence)
                            {
                                var ccg = WorldToChunkCoord(ta.cell);
                                if (!_chunkEdits.TryGetValue(ccg, out var mapG)) { mapG = new Dictionary<Vector3Int, BlockType>(); _chunkEdits[ccg] = mapG; }
                                mapG[ta.cell] = BlockType.Grass;
                            }
                            if (useChunkMeshing)
                            {
                                RebuildChunkMeshAt(cc);
                            }
                            else
                            {
                                if (blockObjects.ContainsKey(ta.cell))
                                {
                                    Destroy(blockObjects[ta.cell]);
                                    blockObjects.Remove(ta.cell);
                                }
                                CreateBlock(ta.cell, BlockType.Grass, ch.parent);
                            }
                        }
                    }
                    else
                    {
                        worldData[ta.cell.x, ta.cell.y, ta.cell.z] = BlockType.Grass;
                        if (blockObjects.ContainsKey(ta.cell))
                        {
                            Destroy(blockObjects[ta.cell]);
                            blockObjects.Remove(ta.cell);
                        }
                        CreateBlock(ta.cell, BlockType.Grass);
                    }
                    // Plants should appear later on new grass
                    SchedulePlantRespawn(ta.cell, newGrassPlantDelayTicks);
                    UpdateNeighboringBlocks(ta.cell);
                }
            }
        }
    }

    public void TrySpawnPlantClusterAt(Vector3Int grassCell, Transform parentOverride = null)
    {
        if (IsOutOfBounds(grassCell)) return;
        var above = grassCell + Vector3Int.up;
        if (GetBlockType(grassCell) != BlockType.Grass) return;
        if (GetBlockType(above) != BlockType.Air) return;
        if (plantObjects.TryGetValue(above, out var existing))
        {
            // If an entry exists but the object was already destroyed (Unity-null), clean it up
            if (existing == null)
            {
                plantObjects.Remove(above);
            }
            else
            {
                return;
            }
        }
    if (!ShouldRenderBlock(grassCell.x, grassCell.y, grassCell.z)) return;
    if (!HasAnyPlants()) return;

    System.Random rng = new System.Random(grassCell.GetHashCode() ^ (_currentTick * 997));
    float desired = Mathf.Max(0f, plantDensity); // use configured density directly
        int count = Mathf.FloorToInt(desired);
        double remainder = desired - count;
        if (rng.NextDouble() < remainder) count++;
        if (count <= 0) return;

        var parent = new GameObject($"Plants ({grassCell.x},{grassCell.y+1},{grassCell.z})");
        parent.transform.parent = parentOverride != null ? parentOverride : this.transform;
    Vector3 placePos = new Vector3(grassCell.x + 0.5f, grassCell.y + 1.001f, grassCell.z + 0.5f);
        if (blockObjects.TryGetValue(grassCell, out var grassGo))
        {
            var rend = grassGo.GetComponent<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds;
        placePos = new Vector3(b.center.x, b.max.y + 0.001f, b.center.z);
            }
        }
        // Snap to actual surface if a collider exists under the spawn point to avoid intersection
        var rayOrigin = placePos + new Vector3(0, 2f, 0);
        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore))
        {
            placePos = new Vector3(placePos.x, hit.point.y + 0.001f, placePos.z);
        }
        parent.transform.position = placePos;
        var def = PlantDB_PickByWeight(rng);
        var tex = PlantDef_GetTexture(def);
        if (tex != null)
        {
            var hr = PlantDef_GetHeightRange(def);
            var w = PlantDef_GetWidth(def);
            var yo = PlantDef_GetYOffset(def);
            SpawnPlantChildrenIntoParent(parent, grassCell, count, rng, tex, hr, w, yo);
        }
        plantObjects[above] = parent;
    }

    // Called by Chunk to unload its rendered cells safely
    public void UnloadCells(IEnumerable<Vector3Int> worldCells, GameObject chunkParent)
    {
        foreach (var cell in worldCells)
        {
            if (blockObjects.TryGetValue(cell, out var go) && go != null) Destroy(go);
            blockObjects.Remove(cell);
            var above = cell + Vector3Int.up;
            if (plantObjects.TryGetValue(above, out var plant) && plant != null) Destroy(plant);
            plantObjects.Remove(above);
        }
        // In meshed mode, renderedCells may be empty; sweep plantObjects by chunk area to remove stale entries
        if (chunkParent != null)
        {
            var basePos = chunkParent.transform.position;
            int baseX = Mathf.FloorToInt(basePos.x);
            int baseZ = Mathf.FloorToInt(basePos.z);
            int maxX = baseX + chunkSizeX;
            int maxZ = baseZ + chunkSizeZ;
            var toRemove = new List<Vector3Int>();
            foreach (var kv in plantObjects)
            {
                var p = kv.Key;
                if (p.x >= baseX && p.x < maxX && p.z >= baseZ && p.z < maxZ)
                {
                    if (kv.Value != null) Destroy(kv.Value);
                    toRemove.Add(p);
                }
            }
            foreach (var k in toRemove) plantObjects.Remove(k);
            Destroy(chunkParent);
        }
    }

    private void SpawnPlantChildrenIntoParent(GameObject parent, Vector3Int grassCell, int count, System.Random rng, Texture2D texture, Vector2 heightRange, float width, float yOffset)
    {
        // Cap quads per cluster for performance
        const int MaxPlantQuadsPerCluster = 8;
        count = Mathf.Clamp(count, 1, MaxPlantQuadsPerCluster);
        if (texture == null) return;

        // Build a combined mesh with 'count' crossed-quads
        var mf = parent.AddComponent<MeshFilter>();
        var mr = parent.AddComponent<MeshRenderer>();
        var mesh = new Mesh { name = $"PlantCluster_{grassCell.x}_{grassCell.y}_{grassCell.z}" };

        var vertices = new List<Vector3>(count * 8);
        var uvs = new List<Vector2>(count * 8);
        var tris = new List<int>(count * 24);

        float maxH = 0.0f;
        for (int i = 0; i < count; i++)
        {
            // Random offset and rotation per crossed-quad set
            float off = 0.18f;
            float ox = (float)(rng.NextDouble()*2 - 1) * off;
            float oz = (float)(rng.NextDouble()*2 - 1) * off;
            float h = Mathf.Lerp(heightRange.x, heightRange.y, (float)rng.NextDouble());
            maxH = Mathf.Max(maxH, h);

            // Two quads centered on origin; we build them aligned (like PlantBillboard) and offset
            int vStart = vertices.Count;
            float halfW = Mathf.Clamp(width, 0.1f, 2f) * 0.5f;
            float y0 = yOffset; // small lift to avoid z-fighting/embedding
            float y1 = h + yOffset;

            // Quad A (along Z)
            vertices.Add(new Vector3(-halfW + ox, y0, 0 + oz));
            vertices.Add(new Vector3( halfW + ox, y0, 0 + oz));
            vertices.Add(new Vector3( halfW + ox, y1, 0 + oz));
            vertices.Add(new Vector3(-halfW + ox, y1, 0 + oz));
            // Quad B (along X)
            vertices.Add(new Vector3(0 + ox, y0, -halfW + oz));
            vertices.Add(new Vector3(0 + ox, y0,  halfW + oz));
            vertices.Add(new Vector3(0 + ox, y1,  halfW + oz));
            vertices.Add(new Vector3(0 + ox, y1, -halfW + oz));

            // UVs
            uvs.Add(new Vector2(0,0)); uvs.Add(new Vector2(1,0)); uvs.Add(new Vector2(1,1)); uvs.Add(new Vector2(0,1));
            uvs.Add(new Vector2(0,0)); uvs.Add(new Vector2(1,0)); uvs.Add(new Vector2(1,1)); uvs.Add(new Vector2(0,1));

            // Triangles (double-sided)
            // Quad A
            tris.Add(vStart + 0); tris.Add(vStart + 2); tris.Add(vStart + 1);
            tris.Add(vStart + 0); tris.Add(vStart + 3); tris.Add(vStart + 2);
            tris.Add(vStart + 1); tris.Add(vStart + 2); tris.Add(vStart + 0);
            tris.Add(vStart + 2); tris.Add(vStart + 3); tris.Add(vStart + 0);
            // Quad B
            tris.Add(vStart + 4); tris.Add(vStart + 6); tris.Add(vStart + 5);
            tris.Add(vStart + 4); tris.Add(vStart + 7); tris.Add(vStart + 6);
            tris.Add(vStart + 5); tris.Add(vStart + 6); tris.Add(vStart + 4);
            tris.Add(vStart + 6); tris.Add(vStart + 7); tris.Add(vStart + 4);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        // Assign shared material (cached by texture)
    mr.sharedMaterial = GetPlantSharedMaterial(texture);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Single trigger collider on the parent to allow interaction
        var col = parent.AddComponent<BoxCollider>();
        col.isTrigger = true;
        float radius = 0.6f; // encompass offsets
        col.size = new Vector3(radius, Mathf.Max(0.6f, maxH), radius);
        col.center = new Vector3(0f, 0.01f + col.size.y * 0.5f, 0f);
    }

    // --- Plant reflection helpers ---
    private bool PlantDB_HasAny()
    {
        if (plantDatabase == null) return false;
        var dbType = plantDatabase.GetType();
        // Try property HasAny
        var hasAnyProp = dbType.GetProperty("HasAny");
        if (hasAnyProp != null && hasAnyProp.PropertyType == typeof(bool))
        {
            return (bool)hasAnyProp.GetValue(plantDatabase);
        }
        // Fallback: iterate plants array and check texture
        var plantsField = dbType.GetField("plants");
        if (plantsField == null) return false;
        var arr = plantsField.GetValue(plantDatabase) as System.Array;
        if (arr == null) return false;
        foreach (var def in arr)
        {
            if (def == null) continue;
            var tex = PlantDef_GetTexture(def);
            var weightField = def.GetType().GetField("weight");
            float w = weightField != null ? Mathf.Max(0f, (float)(weightField.GetValue(def) ?? 0f)) : 1f;
            if (tex != null && w > 0f) return true;
        }
        return false;
    }

    private object PlantDB_PickByWeight(System.Random rng)
    {
        if (plantDatabase == null) return null;
        var dbType = plantDatabase.GetType();
        var pick = dbType.GetMethod("PickByWeight", new System.Type[] { typeof(System.Random) });
        if (pick != null)
        {
            return pick.Invoke(plantDatabase, new object[] { rng });
        }
        // Fallback: simple first non-null
        var plantsField = dbType.GetField("plants");
        var arr = plantsField?.GetValue(plantDatabase) as System.Array;
        if (arr == null || arr.Length == 0) return null;
        foreach (var def in arr) if (def != null) return def;
        return null;
    }

    private Texture2D PlantDef_GetTexture(object def)
    {
        if (def == null) return null;
        return def.GetType().GetField("texture")?.GetValue(def) as Texture2D;
    }

    private Vector2 PlantDef_GetHeightRange(object def)
    {
        if (def == null) return new Vector2(0.6f, 1.2f);
        var f = def.GetType().GetField("heightRange");
        return f != null ? (Vector2)f.GetValue(def) : new Vector2(0.6f, 1.2f);
    }

    private float PlantDef_GetWidth(object def)
    {
        if (def == null) return 0.9f;
        var f = def.GetType().GetField("width");
        return f != null ? Mathf.Clamp((float)(f.GetValue(def) ?? 0.9f), 0.1f, 2f) : 0.9f;
    }

    private float PlantDef_GetYOffset(object def)
    {
        if (def == null) return 0.02f;
        var f = def.GetType().GetField("yOffset");
        return f != null ? (float)(f.GetValue(def) ?? 0.02f) : 0.02f;
    }

    private Material GetPlantSharedMaterial(Texture2D texture)
    {
        if (texture == null) return null;
        if (_plantMaterialCache.TryGetValue(texture, out var mat)) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader)
        {
            name = $"Plant_Unlit_{texture.name}",
            color = Color.white,
            enableInstancing = true
        };
        mat.SetTexture("_BaseMap", texture);
        mat.mainTexture = texture;
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.5f);
        mat.SetFloat("_Cull", 0f); // double-sided
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        _plantMaterialCache[texture] = mat;
        return mat;
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

    // Grid raycast using 3D DDA. Returns the first solid block hit and the empty cell just before it.
    public bool TryVoxelRaycast(Ray ray, float maxDistance, out Vector3Int hitCell, out Vector3Int placeCell, out Vector3 hitNormal)
    {
        hitCell = default;
        placeCell = default;
        hitNormal = Vector3.zero;

        Vector3 pos = ray.origin;
        Vector3 dir = ray.direction;
        if (dir.sqrMagnitude < 1e-8f) return false;
        dir.Normalize();

        // Current cell is floor of position
        Vector3Int cell = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        Vector3Int step = new Vector3Int(dir.x > 0 ? 1 : -1, dir.y > 0 ? 1 : -1, dir.z > 0 ? 1 : -1);

        float t = 0f;
        // Distance to first boundary along each axis
        float nextBoundaryX = (step.x > 0 ? cell.x + 1 : cell.x) - pos.x;
        float nextBoundaryY = (step.y > 0 ? cell.y + 1 : cell.y) - pos.y;
        float nextBoundaryZ = (step.z > 0 ? cell.z + 1 : cell.z) - pos.z;
        float tMaxX = (Mathf.Abs(dir.x) < 1e-8f) ? float.PositiveInfinity : nextBoundaryX / dir.x;
        float tMaxY = (Mathf.Abs(dir.y) < 1e-8f) ? float.PositiveInfinity : nextBoundaryY / dir.y;
        float tMaxZ = (Mathf.Abs(dir.z) < 1e-8f) ? float.PositiveInfinity : nextBoundaryZ / dir.z;
        if (tMaxX < 0) tMaxX = 0; if (tMaxY < 0) tMaxY = 0; if (tMaxZ < 0) tMaxZ = 0;
        float tDeltaX = (Mathf.Abs(dir.x) < 1e-8f) ? float.PositiveInfinity : Mathf.Abs(1f / dir.x);
        float tDeltaY = (Mathf.Abs(dir.y) < 1e-8f) ? float.PositiveInfinity : Mathf.Abs(1f / dir.y);
        float tDeltaZ = (Mathf.Abs(dir.z) < 1e-8f) ? float.PositiveInfinity : Mathf.Abs(1f / dir.z);

        Vector3Int prevCell = cell;
        Vector3 lastNormal = Vector3.zero;

        while (t <= maxDistance)
        {
            // Step to next cell
            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                prevCell = cell;
                cell.x += step.x;
                t = tMaxX;
                tMaxX += tDeltaX;
                lastNormal = new Vector3(-step.x, 0, 0); // face normal of the block entered
            }
            else if (tMaxY < tMaxZ)
            {
                prevCell = cell;
                cell.y += step.y;
                t = tMaxY;
                tMaxY += tDeltaY;
                lastNormal = new Vector3(0, -step.y, 0);
            }
            else
            {
                prevCell = cell;
                cell.z += step.z;
                t = tMaxZ;
                tMaxZ += tDeltaZ;
                lastNormal = new Vector3(0, 0, -step.z);
            }

            // Stop if out of vertical bounds
            if (cell.y < 0 || cell.y >= worldHeight) return false;

            // Check for solid block
            if (GetBlockType(cell) != BlockType.Air)
            {
                hitCell = cell;
                placeCell = prevCell;
                hitNormal = lastNormal;
                return true;
            }
        }

        return false;
    }
}
