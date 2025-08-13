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
        private readonly Dictionary<Material, List<int>> _tris = new();
        private readonly Dictionary<Material, Mesh> _meshes = new();
        private readonly Dictionary<Material, GameObject> _children = new();

        private static readonly Vector2[] QuadUV =
        {
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
        };

        public void Clear()
        {
            foreach (var child in _children.Values)
            {
                if (child != null) Destroy(child);
            }
            _children.Clear();
            _verts.Clear(); _uvs.Clear(); _tris.Clear(); _meshes.Clear();
        }

    public void AddPlantCluster(Vector3 localPos, PlantDefinition def, Material mat, System.Random rng)
        {
            if (def == null || mat == null) return;
            if (!_verts.TryGetValue(mat, out var v)) { v = new List<Vector3>(); _verts[mat] = v; }
            if (!_uvs.TryGetValue(mat, out var u)) { u = new List<Vector2>(); _uvs[mat] = u; }
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
                // Triangles (double-sided)
                // A
                t.Add(vStart + 0); t.Add(vStart + 2); t.Add(vStart + 1);
                t.Add(vStart + 0); t.Add(vStart + 3); t.Add(vStart + 2);
                t.Add(vStart + 1); t.Add(vStart + 2); t.Add(vStart + 0);
                t.Add(vStart + 2); t.Add(vStart + 3); t.Add(vStart + 0);
                // B
                t.Add(vStart + 4); t.Add(vStart + 6); t.Add(vStart + 5);
                t.Add(vStart + 4); t.Add(vStart + 7); t.Add(vStart + 6);
                t.Add(vStart + 5); t.Add(vStart + 6); t.Add(vStart + 4);
                t.Add(vStart + 6); t.Add(vStart + 7); t.Add(vStart + 4);
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
                mr.receiveShadows = false;

                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetUVs(0, u);
                mesh.SetTriangles(t, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mf.sharedMesh = mesh;

                _meshes[mat] = mesh;
                _children[mat] = child;
            }

            _verts.Clear(); _uvs.Clear(); _tris.Clear();
        }
        // Overload that accepts primitive parameters instead of PlantDefinition (for reflection-friendly callers)
        public void AddPlantCluster(Vector3 localPos, Vector2 heightRange, float width, float yOffset, int quadsPerInstance, Material mat, System.Random rng)
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
                t.Add(vStart + 0); t.Add(vStart + 2); t.Add(vStart + 1);
                t.Add(vStart + 0); t.Add(vStart + 3); t.Add(vStart + 2);
                t.Add(vStart + 1); t.Add(vStart + 2); t.Add(vStart + 0);
                t.Add(vStart + 2); t.Add(vStart + 3); t.Add(vStart + 0);
                // B
                t.Add(vStart + 4); t.Add(vStart + 6); t.Add(vStart + 5);
                t.Add(vStart + 4); t.Add(vStart + 7); t.Add(vStart + 6);
                t.Add(vStart + 5); t.Add(vStart + 6); t.Add(vStart + 4);
                t.Add(vStart + 6); t.Add(vStart + 7); t.Add(vStart + 4);
            }
        }
    }
