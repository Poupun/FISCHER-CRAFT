using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// F3-style debug overlay and chunk boundary outlines.
/// - Toggle with F3.
/// - Shows FPS, player pos, chunk coords, and biome.
/// - Draws red wireframe boxes around loaded chunks in real-time.
/// </summary>
public class DebugOverlay : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.F3;
    public bool visible = false;

    [Header("References")]
    public Transform player; // auto-resolved if null
    public WorldGenerator world; // auto-resolved if null

    [Header("UI Style")]
    public int fontSize = 14;
    public Color textColor = new Color(1f, 1f, 1f, 0.9f);
    public Vector2 margin = new Vector2(12, 12);
    public float lineHeight = 18f;

    [Header("Chunk Outline")] 
    public bool drawChunkOutlines = true;
    public Color outlineColor = new Color(1f, 0f, 0f, 1f);
    public float outlineWidth = 2f; // GL line width not widely supported; kept for future
    public bool drawInGameView = true; // draw via GL in Game view

    private float _fpsAccum;
    private int _fpsFrames;
    private float _fps;

    void Awake()
    {
        if (world == null) world = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        if (player == null)
        {
            var fpc = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
            if (fpc != null) player = fpc.transform;
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
                if (pc != null) player = pc.transform;
            }
            if (player == null && Camera.main != null) player = Camera.main.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) visible = !visible;

        // FPS accumulator (simple moving per-second)
        _fpsAccum += Time.deltaTime;
        _fpsFrames++;
        if (_fpsAccum >= 0.5f)
        {
            _fps = _fpsFrames / _fpsAccum;
            _fpsAccum = 0f;
            _fpsFrames = 0;
        }
    }

    void OnGUI()
    {
        if (!visible) return;

        var prevColor = GUI.color;
        var prevFontSize = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = Mathf.Max(10, fontSize);
        GUI.color = textColor;

        float y = margin.y;
        float x = margin.x;

        // FPS
        GUI.Label(new Rect(x, y, 500, lineHeight), $"FPS: {Mathf.RoundToInt(_fps)}");
        y += lineHeight;

        if (player != null)
        {
            var p = player.position;
            GUI.Label(new Rect(x, y, 500, lineHeight), $"Pos: {p.x:F2} {p.y:F2} {p.z:F2}");
            y += lineHeight;
        }

        if (world != null && player != null)
        {
            var cc = world.GetChunkCoord(player.position);
            GUI.Label(new Rect(x, y, 500, lineHeight), $"Chunk: {cc.x} {cc.y} (size {world.GetChunkSizeX()}x{world.GetChunkSizeZ()})");
            y += lineHeight;
            var biome = world.GetBiomeAt(player.position);
            GUI.Label(new Rect(x, y, 500, lineHeight), $"Biome: {biome}");
            y += lineHeight;
        }

        GUI.skin.label.fontSize = prevFontSize;
        GUI.color = prevColor;
    }

    void OnDrawGizmos()
    {
    if (!visible || !drawChunkOutlines || world == null) return;

        // Only draw in play mode (chunks exist when streaming)
        if (!Application.isPlaying) return;

        var loaded = world.SnapshotLoadedChunkCoords();
        if (loaded == null || loaded.Count == 0) return;

        // Draw simple wire boxes in world space for each chunk
        var size = new Vector3(world.GetChunkSizeX(), world.GetWorldHeight(), world.GetChunkSizeZ());
        var color = outlineColor;

        Gizmos.color = color;
        foreach (var c in loaded)
        {
            var origin = world.GetChunkOrigin(c);
            var center = origin + new Vector3(size.x, size.y, size.z) * 0.5f;
            Gizmos.DrawWireCube(center, size);
        }
    }

    // --- Runtime Game view lines ---
    static Material s_lineMat;
    static void EnsureLineMaterial()
    {
        if (s_lineMat != null) return;
        var shader = Shader.Find("Hidden/Internal-Colored");
        s_lineMat = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        // Enable alpha blending
        s_lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        s_lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        s_lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        s_lineMat.SetInt("_ZWrite", 0);
    }

    void OnRenderObject()
    {
        if (!Application.isPlaying) return;
        if (!visible || !drawChunkOutlines || !drawInGameView) return;
        if (world == null) return;

        var loaded = world.SnapshotLoadedChunkCoords();
        if (loaded == null || loaded.Count == 0) return;

        EnsureLineMaterial();
        if (s_lineMat == null) return;
        s_lineMat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);
        var col = outlineColor;
        col.a = Mathf.Clamp01(col.a);
        GL.Color(col);

        var size = new Vector3(world.GetChunkSizeX(), world.GetWorldHeight(), world.GetChunkSizeZ());
        foreach (var c in loaded)
        {
            var origin = world.GetChunkOrigin(c);
            DrawWireCubeGL(origin, size);
        }

        GL.End();
        GL.PopMatrix();
    }

    static void DrawWireCubeGL(Vector3 origin, Vector3 size)
    {
        Vector3 a = origin;
        Vector3 b = origin + new Vector3(size.x, 0, 0);
        Vector3 c = origin + new Vector3(size.x, 0, size.z);
        Vector3 d = origin + new Vector3(0, 0, size.z);
        Vector3 e = a + new Vector3(0, size.y, 0);
        Vector3 f = b + new Vector3(0, size.y, 0);
        Vector3 g = c + new Vector3(0, size.y, 0);
        Vector3 h = d + new Vector3(0, size.y, 0);

        // Bottom rectangle
        Line(a, b); Line(b, c); Line(c, d); Line(d, a);
        // Top rectangle
        Line(e, f); Line(f, g); Line(g, h); Line(h, e);
        // Verticals
        Line(a, e); Line(b, f); Line(c, g); Line(d, h);
    }

    static void Line(Vector3 p0, Vector3 p1)
    {
        GL.Vertex(p0); GL.Vertex(p1);
    }
}
