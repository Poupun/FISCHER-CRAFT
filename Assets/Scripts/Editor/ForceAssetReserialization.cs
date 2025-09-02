using UnityEngine;
using UnityEditor;

public class ForceAssetReserialization : EditorWindow
{
    [MenuItem("Tools/Block System/Force Asset Reserialization")]
    public static void ShowWindow()
    {
        GetWindow<ForceAssetReserialization>("Force Reserialization");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Force Asset Reserialization", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This will force Unity to re-serialize all BlockConfiguration assets with the new face texture fields.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Force Reserialization", GUILayout.Height(40)))
        {
            ForceReserializeAssets();
        }
    }
    
    static void ForceReserializeAssets()
    {
        Debug.Log("=== FORCING ASSET RESERIALIZATION ===");
        
        // Find all BlockConfiguration assets
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                Debug.Log($"Re-serializing {blockConfig.blockType} asset...");
                
                // Force dirty and save to trigger reserialization
                EditorUtility.SetDirty(blockConfig);
                
                // For CraftingTable, manually set the face textures
                if (blockConfig.blockType == BlockType.CraftingTable)
                {
                    blockConfig.frontTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
                    blockConfig.leftTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
                    blockConfig.rightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
                    blockConfig.backTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
                    
                    Debug.Log($"✅ Set face textures for CraftingTable:");
                    Debug.Log($"   • Front: {blockConfig.frontTexture?.name}");
                    Debug.Log($"   • Sides: {blockConfig.leftTexture?.name}");
                }
            }
        }
        
        // Force save all assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ RESERIALIZATION COMPLETE!");
    }
}
