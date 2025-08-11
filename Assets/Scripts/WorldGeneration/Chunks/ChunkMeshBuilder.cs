using System.Collections.Generic;
using UnityEngine;

namespace WorldGeneration.Chunks
{
    /// <summary>
    /// Builds a single mesh per chunk.
    /// Submeshes are grouped by the actual Material used per-face, allowing
    /// per-face textures (e.g., Grass: top=grass, sides=grass_side, bottom=dirt).
    /// </summary>
    public static class ChunkMeshBuilder
    {
        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

        // For each dir, vertices of a unit quad at origin facing that dir
        private static readonly Vector3[][] FaceVerts =
        {
            // +X
            new [] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
            // -X
            new [] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
            // +Y
            new [] { new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(0,1,0) },
            // -Y
            new [] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
            // +Z
            new [] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
            // -Z
            new [] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) }
        };

        private static readonly Vector2[] QuadUV =
        {
            new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0)
        };

        public static void BuildMesh(WorldGenerator world, Chunk chunk, bool addCollider)
        {
            if (world == null || chunk == null) return;
            var parent = chunk.parent;
            if (parent == null) return;

            var mf = parent.GetComponent<MeshFilter>();
            if (mf == null) mf = parent.gameObject.AddComponent<MeshFilter>();
            var mr = parent.GetComponent<MeshRenderer>();
            if (mr == null) mr = parent.gameObject.AddComponent<MeshRenderer>();

            // Triangles grouped by material used for each face
            var trisByMaterial = new Dictionary<Material, List<int>>();
            var verts = new List<Vector3>(chunk.sizeX * chunk.sizeY * chunk.sizeZ);
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();

            // Helper to get/create tri list for a material
            List<int> GetList(Material m)
            {
                if (!trisByMaterial.TryGetValue(m, out var list))
                {
                    list = new List<int>(1024);
                    trisByMaterial[m] = list;
                }
                return list;
            }

            // Iterate all blocks and emit faces against air
            for (int x = 0; x < chunk.sizeX; x++)
            {
                for (int y = 0; y < chunk.sizeY; y++)
                {
                    for (int z = 0; z < chunk.sizeZ; z++)
                    {
                        var t = chunk.GetLocal(x, y, z);
                        if (t == BlockType.Air) continue;

                        // Local pos of this block's origin (mesh is in chunk parent's local space)
                        Vector3 basePos = new Vector3(x, y, z);

                        for (int d = 0; d < 6; d++)
                        {
                            var dir = Directions[d];
                            int nx = x + dir.x, ny = y + dir.y, nz = z + dir.z;
                            BlockType neighbor = BlockType.Air;
                            if (nx >= 0 && nx < chunk.sizeX && ny >= 0 && ny < chunk.sizeY && nz >= 0 && nz < chunk.sizeZ)
                                neighbor = chunk.GetLocal(nx, ny, nz);
                            // Emit face only if neighbor is Air
                            if (neighbor != BlockType.Air) continue;

                            var f = FaceVerts[d];
                            int vi = verts.Count;
                            verts.Add(basePos + f[0]);
                            verts.Add(basePos + f[1]);
                            verts.Add(basePos + f[2]);
                            verts.Add(basePos + f[3]);
                            // Normal is dir
                            var n = (Vector3)dir;
                            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
                            // Simple UVs
                            uvs.Add(QuadUV[0]); uvs.Add(QuadUV[1]); uvs.Add(QuadUV[2]); uvs.Add(QuadUV[3]);

                            // Special-case: Grass side faces are composed of Dirt base + GrassSide overlay (alpha-cutout).
                            if (world != null && t == BlockType.Grass && (d == 0 || d == 1 || d == 4 || d == 5))
                            {
                                var dirtMat = world.GetBlockMaterial(BlockType.Dirt);
                                if (dirtMat != null)
                                {
                                    var triD = GetList(dirtMat);
                                    triD.Add(vi + 0); triD.Add(vi + 1); triD.Add(vi + 2);
                                    triD.Add(vi + 0); triD.Add(vi + 2); triD.Add(vi + 3);
                                }
                                var overlayMat = world.GetGrassSideOverlayMaterial();
                                if (overlayMat != null)
                                {
                                    var triO = GetList(overlayMat);
                                    triO.Add(vi + 0); triO.Add(vi + 1); triO.Add(vi + 2);
                                    triO.Add(vi + 0); triO.Add(vi + 2); triO.Add(vi + 3);
                                }
                                continue;
                            }

                            // Choose single material per face (e.g., top/bottom or non-grass blocks)
                            Material faceMat = null;
                            if (world != null)
                            {
                                // face index mapping matches Directions order
                                faceMat = world.GetFaceMaterial(t, d);
                            }
                            if (faceMat == null)
                            {
                                faceMat = world != null ? world.GetBlockMaterial(t) : null;
                            }
                            if (faceMat == null) continue; // skip if no material configured

                            var tri = GetList(faceMat);
                            tri.Add(vi + 0); tri.Add(vi + 1); tri.Add(vi + 2);
                            tri.Add(vi + 0); tri.Add(vi + 2); tri.Add(vi + 3);
                        }
                    }
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            // Stable ordering by material name (fallback to instanceID)
            var materials = new List<Material>(trisByMaterial.Keys);
            materials.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return -1;
                if (b == null) return 1;
                int byName = string.Compare(a.name, b.name, System.StringComparison.Ordinal);
                if (byName != 0) return byName;
                return a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            mesh.subMeshCount = materials.Count;
            for (int i = 0; i < materials.Count; i++)
            {
                mesh.SetTriangles(trisByMaterial[materials[i]], i);
            }
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mr.sharedMaterials = materials.ToArray();

            if (addCollider)
            {
                var mc = parent.GetComponent<MeshCollider>();
                if (mc == null) mc = parent.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = null; // force refresh
                mc.sharedMesh = mesh;
            }
        }
    }
}
