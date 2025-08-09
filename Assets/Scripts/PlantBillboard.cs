using UnityEngine;

/// <summary>
/// Builds a crossed-quad mesh (like Minecraft plants) and applies a URP/Lit alpha-cutout material.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PlantBillboard : MonoBehaviour
{
    private MeshFilter mf;
    private MeshRenderer mr;

    [HideInInspector] public WorldGenerator world;
    [HideInInspector] public Vector3Int supportCell; // cell of the supporting block (usually Grass)
    public float checkInterval = 0.25f;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
    }

    public void Configure(Texture2D texture, float width = 0.8f, float height = 1.0f, float groundYOffset = 0.001f)
    {
        if (texture == null) return;

        // Ensure texture sampling looks crisp
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.anisoLevel = 0;

        // Build crossed quads centered on the block
        Mesh mesh = new Mesh();
        mesh.name = "PlantCrossedQuads";

        float halfW = width * 0.5f;
        // Ground at Y = groundYOffset; top at height + groundYOffset
        float y0 = groundYOffset;
        float y1 = height + groundYOffset;

        // Two quads: one on X-axis (facing Z), one on Z-axis (facing X)
        Vector3[] v = new Vector3[8]
        {
            // Quad A (aligned with X, spans along Z)
            new Vector3(-halfW, y0, 0),
            new Vector3( halfW, y0, 0),
            new Vector3( halfW, y1, 0),
            new Vector3(-halfW, y1, 0),
            // Quad B (aligned with Z, spans along X)
            new Vector3(0, y0, -halfW),
            new Vector3(0, y0,  halfW),
            new Vector3(0, y1,  halfW),
            new Vector3(0, y1, -halfW)
        };

        // Triangles for both quads (double-sided by duplicating with reversed winding)
        int[] t = new int[24]
        {
            // Quad A front
            0,2,1, 0,3,2,
            // Quad A back
            1,2,0, 2,3,0,
            // Quad B front
            4,6,5, 4,7,6,
            // Quad B back
            5,6,4, 6,7,4
        };

        Vector2[] uv = new Vector2[8]
        {
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
        };

        mesh.vertices = v;
        mesh.triangles = t;
        mesh.uv = uv;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

    // Create URP/Lit alpha-cutout material instance
    var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader) { name = "Plant_Lit_Cutout", color = Color.white };
        mat.SetTexture("_BaseMap", texture);
        mat.mainTexture = texture;
    // Unlit, so no smoothness/metallic needed
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.5f);
    mat.SetFloat("_Cull", 0f); // double-sided
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

    mr.sharedMaterial = mat;
    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // avoid harsh black shadows from thin quads
    mr.receiveShadows = false;
    }

    void Start()
    {
        if (world == null) world = GetComponentInParent<WorldGenerator>();
        if (supportCell == default)
        {
            Vector3 p = transform.position;
            supportCell = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y - 0.1f), Mathf.FloorToInt(p.z));
        }
        StartCoroutine(SupportWatchdog());
    }

    private System.Collections.IEnumerator SupportWatchdog()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            if (world != null)
            {
                // If support is not Grass or plant cell is occupied, remove this plant
                var support = world.GetBlockType(supportCell);
                var above = world.GetBlockType(supportCell + Vector3Int.up);
                if (support != BlockType.Grass || above != BlockType.Air)
                {
                    world.RemovePlantAt(supportCell + Vector3Int.up);
                    yield break;
                }
            }
            yield return wait;
        }
    }

    void OnDestroy()
    {
        if (world != null)
        {
            world.RemovePlantAt(supportCell + Vector3Int.up);
        }
    }
}
