using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Batches all plants for a single chunk into one MeshRenderer per unique material (texture).
/// Mesh is built in chunk-local space and attached as a child to the chunk parent.
/// </summary>
public class PlantChunkRenderer : MonoBehaviour
{
        public Vector2Int chunkCoord;
        public int chunkSizeX;
        public int chunkSizeZ;
        public int worldHeight;

    private readonly Dictionary<Material, List<Vector3>> _verts = new();
    private readonly Dictionary<Material, List<Vector2>> _uvs = new();
    // Per-material vertex colors (used for wind bend weight: bottom=0, top=1)
    private readonly Dictionary<Material, List<Color>> _colors = new();
    private readonly Dictionary<Material, List<int>> _tris = new();
    private readonly Dictionary<Material, Mesh> _meshes = new();
    private readonly Dictionary<Material, GameObject> _children = new();
    // Per plant cell tracking for surgical removal: plantCell -> list of (material, startIndex, indexCount)
    private readonly Dictionary<Vector3Int, List<(Material mat,int triStart,int triCount)>> _cellToRanges = new();
    // Per material dummy vertex index (offscreen) used to collapse removed triangles safely
    private readonly Dictionary<Material, int> _dummyIndex = new();

        private static readonly Vector2[] QuadUV =
        {
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
        };

        public void Clear(bool preserveTracking = false)
        {
            foreach (var child in _children.Values)
            {
                if (child != null) Destroy(child);
            }
            _children.Clear();
            _verts.Clear(); _uvs.Clear(); _colors.Clear(); _tris.Clear(); _meshes.Clear(); _dummyIndex.Clear();
            if (!preserveTracking) _cellToRanges.Clear();
        }

    public void AddPlantCluster(Vector3 localPos, PlantDefinition def, Material mat, System.Random rng, Vector3Int? plantCell = null)
        {
            if (def == null || mat == null) return;
            if (!_verts.TryGetValue(mat, out var v)) { v = new List<Vector3>(); _verts[mat] = v; }
            if (!_uvs.TryGetValue(mat, out var u)) { u = new List<Vector2>(); _uvs[mat] = u; }
            if (!_colors.TryGetValue(mat, out var c)) { c = new List<Color>(); _colors[mat] = c; }
            if (!_tris.TryGetValue(mat, out var t)) { t = new List<int>(); _tris[mat] = t; }

            int cluster = Mathf.Clamp(def.quadsPerInstance, 1, 4);
            float w = Mathf.Clamp(def.width, 0.1f, 2f);
            float halfW = w * 0.5f;

            for (int i = 0; i < cluster; i++)
            {
                float h = Mathf.Lerp(def.heightRange.x, def.heightRange.y, (float)rng.NextDouble());
                float off = 0.18f;
                float ox = (float)(rng.NextDouble() * 2 - 1) * off;
                float oz = (float)(rng.NextDouble() * 2 - 1) * off;
                float y0 = def.yOffset;
                float y1 = h + def.yOffset;

                int vStart = v.Count;
                // Quad A (along Z)
                v.Add(localPos + new Vector3(-halfW + ox, y0, 0 + oz));
                v.Add(localPos + new Vector3( halfW + ox, y0, 0 + oz));
                v.Add(localPos + new Vector3( halfW + ox, y1, 0 + oz));
                v.Add(localPos + new Vector3(-halfW + ox, y1, 0 + oz));
                // Quad B (along X)
                v.Add(localPos + new Vector3(0 + ox, y0, -halfW + oz));
                v.Add(localPos + new Vector3(0 + ox, y0,  halfW + oz));
                v.Add(localPos + new Vector3(0 + ox, y1,  halfW + oz));
                v.Add(localPos + new Vector3(0 + ox, y1, -halfW + oz));
                // UVs
                u.Add(QuadUV[0]); u.Add(QuadUV[1]); u.Add(QuadUV[2]); u.Add(QuadUV[3]);
                u.Add(QuadUV[0]); u.Add(QuadUV[1]); u.Add(QuadUV[2]); u.Add(QuadUV[3]);
                // Colors (weights) bottom verts weight 0, top verts weight 1
                c.Add(new Color(0,0,0,1)); c.Add(new Color(0,0,0,1)); c.Add(new Color(1,1,1,1)); c.Add(new Color(1,1,1,1));
                c.Add(new Color(0,0,0,1)); c.Add(new Color(0,0,0,1)); c.Add(new Color(1,1,1,1)); c.Add(new Color(1,1,1,1));
                // Triangles (double-sided)
                // A
                int triStart = t.Count; // capture start for cell mapping
                t.Add(vStart + 0); t.Add(vStart + 2); t.Add(vStart + 1);
                t.Add(vStart + 0); t.Add(vStart + 3); t.Add(vStart + 2);
                t.Add(vStart + 1); t.Add(vStart + 2); t.Add(vStart + 0);
                t.Add(vStart + 2); t.Add(vStart + 3); t.Add(vStart + 0);
                // B
                t.Add(vStart + 4); t.Add(vStart + 6); t.Add(vStart + 5);
                t.Add(vStart + 4); t.Add(vStart + 7); t.Add(vStart + 6);
                t.Add(vStart + 5); t.Add(vStart + 6); t.Add(vStart + 4);
                t.Add(vStart + 6); t.Add(vStart + 7); t.Add(vStart + 4);
                int triAdded = 24; // 8 triangles * 3 indices
                if (plantCell.HasValue)
                {
                    if (!_cellToRanges.TryGetValue(plantCell.Value, out var list))
                        list = _cellToRanges[plantCell.Value] = new List<(Material,int,int)>();
                    list.Add((mat, triStart, triAdded));
                }
            }
        }

    public void Flush(WorldGenerator world)
        {
            foreach (var kv in _verts)
            {
                var mat = kv.Key;
                var v = kv.Value;
                var u = _uvs[mat];
                var t = _tris[mat];
                if (v.Count == 0) continue;

                var child = new GameObject($"Plants[{mat.name}]");
                child.transform.SetParent(transform, false);
                var mf = child.AddComponent<MeshFilter>();
                var mr = child.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;

                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetUVs(0, u);
                // Safe color weight application: handle legacy data where colors might be missing
                if (!_colors.TryGetValue(mat, out var cList) || cList == null || cList.Count != v.Count)
                {
                    // Reconstruct weights assuming vertex pattern added in groups of 8:
                    // indices [0,1]=bottom, [2,3]=top, [4,5]=bottom, [6,7]=top
                    cList = new List<Color>(v.Count);
                    for (int i = 0; i < v.Count; i += 8)
                    {
                        // Guard if final partial group (should not happen but be safe)
                        int remain = v.Count - i;
                        if (remain >= 8)
                        {
                            cList.Add(new Color(0,0,0,1)); // 0
                            cList.Add(new Color(0,0,0,1)); // 1
                            cList.Add(new Color(1,1,1,1)); // 2
                            cList.Add(new Color(1,1,1,1)); // 3
                            cList.Add(new Color(0,0,0,1)); // 4
                            cList.Add(new Color(0,0,0,1)); // 5
                            cList.Add(new Color(1,1,1,1)); // 6
                            cList.Add(new Color(1,1,1,1)); // 7
                        }
                        else
                        {
                            // Fallback: linear gradient by Y within this leftover slice
                            for (int j = 0; j < remain; j++)
                            {
                                var y = v[i + j].y;
                                float minY = v[i].y;
                                float maxY = v[i].y;
                                for (int k = 0; k < remain; k++) { float yy = v[i + k].y; if (yy < minY) minY = yy; if (yy > maxY) maxY = yy; }
                                float w = maxY > minY ? (y - minY) / (maxY - minY) : 0f;
                                cList.Add(new Color(w,w,w,1));
                            }
                        }
                    }
                }
                mesh.SetColors(cList);
                mesh.SetTriangles(t, 0);
                // Provide explicit normals pointing up for consistent lighting on crossed quads
                var norms = new List<Vector3>(v.Count);
                for (int i = 0; i < v.Count; i++) norms.Add(Vector3.up);
                mesh.SetNormals(norms);
                mesh.RecalculateBounds();
                mf.sharedMesh = mesh;

                _meshes[mat] = mesh;
                _children[mat] = child;
            }

            _verts.Clear(); _uvs.Clear(); _tris.Clear();
            _colors.Clear();
        }
        // Overload that accepts primitive parameters instead of PlantDefinition (for reflection-friendly callers)
    public void AddPlantCluster(Vector3 localPos, Vector2 heightRange, float width, float yOffset, int quadsPerInstance, Material mat, System.Random rng, Vector3Int? plantCell = null)
        {
            if (mat == null) return;
            if (!_verts.TryGetValue(mat, out var v)) { v = new List<Vector3>(); _verts[mat] = v; }
            if (!_uvs.TryGetValue(mat, out var u)) { u = new List<Vector2>(); _uvs[mat] = u; }
            if (!_tris.TryGetValue(mat, out var t)) { t = new List<int>(); _tris[mat] = t; }

            int cluster = Mathf.Clamp(quadsPerInstance, 1, 4);
            float w = Mathf.Clamp(width, 0.1f, 2f);
            float halfW = w * 0.5f;

            for (int i = 0; i < cluster; i++)
            {
                float h = Mathf.Lerp(heightRange.x, heightRange.y, (float)rng.NextDouble());
                float off = 0.18f;
                float ox = (float)(rng.NextDouble() * 2 - 1) * off;
                float oz = (float)(rng.NextDouble() * 2 - 1) * off;
                float y0 = yOffset;
                float y1 = h + yOffset;

                int vStart = v.Count;
                // Quad A (along Z)
                v.Add(localPos + new Vector3(-halfW + ox, y0, 0 + oz));
                v.Add(localPos + new Vector3( halfW + ox, y0, 0 + oz));
                v.Add(localPos + new Vector3( halfW + ox, y1, 0 + oz));
                v.Add(localPos + new Vector3(-halfW + ox, y1, 0 + oz));
                // Quad B (along X)
                v.Add(localPos + new Vector3(0 + ox, y0, -halfW + oz));
                v.Add(localPos + new Vector3(0 + ox, y0,  halfW + oz));
                v.Add(localPos + new Vector3(0 + ox, y1,  halfW + oz));
                v.Add(localPos + new Vector3(0 + ox, y1, -halfW + oz));
                // UVs
                u.Add(QuadUV[0]); u.Add(QuadUV[1]); u.Add(QuadUV[2]); u.Add(QuadUV[3]);
                u.Add(QuadUV[0]); u.Add(QuadUV[1]); u.Add(QuadUV[2]); u.Add(QuadUV[3]);
                // Triangles (double-sided)
                // A
                int triStart = t.Count;
                t.Add(vStart + 0); t.Add(vStart + 2); t.Add(vStart + 1);
                t.Add(vStart + 0); t.Add(vStart + 3); t.Add(vStart + 2);
                t.Add(vStart + 1); t.Add(vStart + 2); t.Add(vStart + 0);
                t.Add(vStart + 2); t.Add(vStart + 3); t.Add(vStart + 0);
                // B
                t.Add(vStart + 4); t.Add(vStart + 6); t.Add(vStart + 5);
                t.Add(vStart + 4); t.Add(vStart + 7); t.Add(vStart + 6);
                t.Add(vStart + 5); t.Add(vStart + 6); t.Add(vStart + 4);
                t.Add(vStart + 6); t.Add(vStart + 7); t.Add(vStart + 4);
                int triAdded = 24;
                if (plantCell.HasValue)
                {
                    if (!_cellToRanges.TryGetValue(plantCell.Value, out var list))
                        list = _cellToRanges[plantCell.Value] = new List<(Material,int,int)>();
                    list.Add((mat, triStart, triAdded));
                }
            }
        }

        // Remove plant geometry for a given world plant cell (chunk local tracking via world cell key).
        public void RemovePlantCellGeometry(Vector3Int plantCell)
        {
            if (!_cellToRanges.TryGetValue(plantCell, out var ranges) || ranges.Count == 0) return;
            // Degenerate each triangle range by pointing all three indices of every triangle to a dedicated
            // offscreen dummy vertex so other ranges keep valid offsets and future removals stay correct.
            foreach (var (mat, triStart, triCount) in ranges)
            {
                if (!_meshes.TryGetValue(mat, out var mesh) || mesh == null) continue;
                // Ensure dummy vertex exists
                if (!_dummyIndex.TryGetValue(mat, out int dummy))
                {
                    var verts = new List<Vector3>();
                    mesh.GetVertices(verts);
                    verts.Add(new Vector3(0, -9999f, 0));
                    dummy = verts.Count - 1;
                    mesh.SetVertices(verts);
                    _dummyIndex[mat] = dummy;
                }
                var tris = mesh.GetTriangles(0);
                int end = Mathf.Min(triStart + triCount, tris.Length);
                // triCount is multiple of 3 (24). Replace each index with dummy.
                for (int i = triStart; i < end; i++) tris[i] = dummy;
                mesh.SetTriangles(tris, 0, true);
            }
            _cellToRanges.Remove(plantCell);
        }
    }
