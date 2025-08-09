using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For Grass blocks only: splits the cube mesh into 3 submeshes (Top/Bottom/Sides)
/// and assigns materials so that top uses grass.png, bottom uses dirt.png, sides use side_normal.png.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GrassBlockRenderer : MonoBehaviour
{
    // Cached materials so we don't recreate them for every block
    private static Material cachedTopMat;
    private static Material cachedBottomMat;
    private static Material cachedSideBaseMat;
    private static Material cachedSideOverlayMat;
    private static Texture2D cachedTopTex;
    private static Texture2D cachedBottomTex;
    private static Texture2D cachedSideTex;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        // Auto-apply if a WorldGenerator is found in parents
        var world = GetComponentInParent<WorldGenerator>();
        if (world != null)
        {
            Apply(world);
        }
    }

    public void Apply(WorldGenerator world)
    {
        if (world == null) return;

        // Determine textures from the world generator
        var topTex = world.grassTexture;
        var bottomTex = world.dirtTexture;
        var sideTex = world.grassSideTexture != null ? world.grassSideTexture : world.grassTexture; // fallback

        EnsureMaterials(topTex, bottomTex, sideTex);
    EnsureSubmeshes();

    // Assign materials in the same order as submeshes: Top, Bottom, SideBase, SideOverlay
    meshRenderer.materials = new[] { cachedTopMat, cachedBottomMat, cachedSideBaseMat, cachedSideOverlayMat };
    }

    private void EnsureMaterials(Texture2D topTex, Texture2D bottomTex, Texture2D sideTex)
    {
        // Rebuild cache if any texture changed or materials missing
    bool needRebuild = cachedTopMat == null || cachedBottomMat == null || cachedSideBaseMat == null || cachedSideOverlayMat == null
                           || cachedTopTex != topTex || cachedBottomTex != bottomTex || cachedSideTex != sideTex;

        if (!needRebuild) return;

        cachedTopTex = topTex;
        cachedBottomTex = bottomTex;
        cachedSideTex = sideTex;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

    cachedTopMat = new Material(shader) { name = "Grass_Top_Mat", color = Color.white };
    cachedBottomMat = new Material(shader) { name = "Grass_Bottom_Mat", color = Color.white };
    cachedSideBaseMat = new Material(shader) { name = "Grass_SideBase_Mat", color = Color.white };
    // Overlay uses URP/Lit with Alpha Clipping to avoid transparent sorting/black fringes
    cachedSideOverlayMat = new Material(shader) { name = "Grass_SideOverlay_Mat", color = Color.white };

        // Configure textures for pixel-art look
        ConfigureTexture(cachedTopTex);
        ConfigureTexture(cachedBottomTex);
        ConfigureTexture(cachedSideTex);

    // Assign textures to both mainTexture and _BaseMap for URP
    cachedTopMat.mainTexture = cachedTopTex;
    cachedTopMat.SetTexture("_BaseMap", cachedTopTex);
    cachedBottomMat.mainTexture = cachedBottomTex;
    cachedBottomMat.SetTexture("_BaseMap", cachedBottomTex);
    cachedSideBaseMat.mainTexture = cachedBottomTex; // base dirt for sides
    cachedSideBaseMat.SetTexture("_BaseMap", cachedBottomTex);
    cachedSideOverlayMat.mainTexture = cachedSideTex; // overlay
    cachedSideOverlayMat.SetTexture("_BaseMap", cachedSideTex);

        // Flat, non-metallic
    cachedTopMat.SetFloat("_Smoothness", 0f);
    cachedBottomMat.SetFloat("_Smoothness", 0f);
    cachedSideBaseMat.SetFloat("_Smoothness", 0f);
    cachedSideOverlayMat.SetFloat("_Smoothness", 0f);
    cachedTopMat.SetFloat("_Metallic", 0f);
    cachedBottomMat.SetFloat("_Metallic", 0f);
    cachedSideBaseMat.SetFloat("_Metallic", 0f);
    cachedSideOverlayMat.SetFloat("_Metallic", 0f);

    // Overlay as alpha-clipped (cutout)
    cachedSideOverlayMat.SetFloat("_AlphaClip", 1f);
    cachedSideOverlayMat.SetFloat("_Cutoff", 0.5f);
    cachedSideOverlayMat.EnableKeyword("_ALPHATEST_ON");
    cachedSideOverlayMat.DisableKeyword("_ALPHABLEND_ON");
    cachedSideOverlayMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    cachedSideOverlayMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
    }

    private void ConfigureTexture(Texture2D tex)
    {
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.anisoLevel = 0;
    }

    private void EnsureSubmeshes()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        // Duplicate the mesh so we don't mutate the shared asset
        Mesh source = meshFilter.sharedMesh;
        Mesh mesh = Instantiate(source);
        mesh.name = source.name + "_GrassSubmeshes";

    var normals = mesh.normals;
    var tris = mesh.triangles;
    var verts = mesh.vertices;

    List<int> top = new List<int>(12);
    List<int> bottom = new List<int>(12);
    List<int> sideBase = new List<int>(24);
    List<int> sideOverlay = new List<int>(24);

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];
            Vector3 n = (normals[i0] + normals[i1] + normals[i2]) / 3f;

            if (n.y > 0.5f)
            {
                top.Add(i0); top.Add(i1); top.Add(i2);
            }
            else if (n.y < -0.5f)
            {
                bottom.Add(i0); bottom.Add(i1); bottom.Add(i2);
            }
            else
            {
                // duplicate side triangles for base and overlay passes
                sideBase.Add(i0); sideBase.Add(i1); sideBase.Add(i2);
                sideOverlay.Add(i0); sideOverlay.Add(i1); sideOverlay.Add(i2);
            }
        }

        // Recompute UVs so that side V goes bottom->top consistently
        Vector3 min = verts[0], max = verts[0];
        for (int i = 1; i < verts.Length; i++)
        {
            min = Vector3.Min(min, verts[i]);
            max = Vector3.Max(max, verts[i]);
        }
        float rx = Mathf.Max(0.0001f, max.x - min.x);
        float ry = Mathf.Max(0.0001f, max.y - min.y);
        float rz = Mathf.Max(0.0001f, max.z - min.z);
        var uvs = new Vector2[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 n = normals[i];
            Vector3 v = verts[i];
            if (n.y > 0.5f)
            {
                // Top: map X/Z
                uvs[i] = new Vector2((v.x - min.x) / rx, (v.z - min.z) / rz);
            }
            else if (n.y < -0.5f)
            {
                // Bottom: map X/Z
                uvs[i] = new Vector2((v.x - min.x) / rx, (v.z - min.z) / rz);
            }
            else if (Mathf.Abs(n.x) >= Mathf.Abs(n.z))
            {
                // +/-X faces: U by Z, V by Y (upwards)
                uvs[i] = new Vector2((v.z - min.z) / rz, (v.y - min.y) / ry);
            }
            else
            {
                // +/-Z faces: U by X, V by Y (upwards)
                uvs[i] = new Vector2((v.x - min.x) / rx, (v.y - min.y) / ry);
            }
        }
        mesh.uv = uvs;

        mesh.subMeshCount = 4;
        mesh.SetTriangles(top, 0);
        mesh.SetTriangles(bottom, 1);
        mesh.SetTriangles(sideBase, 2);
        mesh.SetTriangles(sideOverlay, 3);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }
}
