using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var wg = (WorldGenerator)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload World"))
            {
                if (Application.isPlaying)
                {
                    wg.ReloadWorld();
                }
                else
                {
                    Debug.LogWarning("Reload only works in Play Mode.");
                }
            }
            if (GUILayout.Button("Reseed + Reload"))
            {
                if (Application.isPlaying)
                {
                    wg.reseedOnReload = true;
                    wg.ReloadWorld();
                }
            }
        }
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Use the buttons above to rebuild all streamed chunks. 'Reseed + Reload' changes the seed first if reseedOnReload is enabled.", MessageType.Info);
        }
    }
}
