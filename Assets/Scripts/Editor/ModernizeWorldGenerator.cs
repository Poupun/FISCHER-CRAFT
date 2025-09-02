using UnityEngine;
using UnityEditor;

public class ModernizeWorldGenerator : EditorWindow
{
    [MenuItem("Tools/Block System/Modernize WorldGenerator")]
    public static void ShowWindow()
    {
        GetWindow<ModernizeWorldGenerator>("Modernize WorldGenerator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Modernize WorldGenerator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This will update the WorldGenerator to use the BlockConfiguration system instead of hardcoded texture references for crafting tables.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Quick Fix: Assign WorldGenerator Textures", GUILayout.Height(40)))
        {
            QuickFixWorldGeneratorTextures();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Future: Update WorldGenerator to use BlockManager", GUILayout.Height(30)))
        {
            Debug.Log("This would modify WorldGenerator.cs to use BlockManager.GetBlockConfiguration() instead of hardcoded textures. For now, use the quick fix above.");
        }
    }
    
    void QuickFixWorldGeneratorTextures()
    {
        Debug.Log("=== QUICK FIX: ASSIGNING WORLDGENERATOR TEXTURES ===");
        
        WorldGenerator worldGen = FindObjectOfType<WorldGenerator>();
        if (worldGen == null)
        {
            Debug.LogError("No WorldGenerator found!");
            return;
        }
        
        // Use reflection to set private fields if needed
        var worldGenType = typeof(WorldGenerator);
        
        // Load textures
        var craftingTableTop = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_top.png");
        var craftingTableFront = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
        var craftingTableSide = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        
        if (craftingTableTop == null || craftingTableFront == null || craftingTableSide == null)
        {
            Debug.LogError("Could not find all required crafting table textures!");
            return;
        }
        
        // Set the public fields directly
        worldGen.craftingTableTexture = craftingTableTop;
        worldGen.craftingTableFrontTexture = craftingTableFront;
        worldGen.craftingTableSideTexture = craftingTableSide;
        
        // Mark as dirty
        EditorUtility.SetDirty(worldGen);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(worldGen.gameObject.scene);
        
        Debug.Log("✅ Fixed WorldGenerator textures:");
        Debug.Log($"   • craftingTableTexture: {craftingTableTop.name}");
        Debug.Log($"   • craftingTableFrontTexture: {craftingTableFront.name}");
        Debug.Log($"   • craftingTableSideTexture: {craftingTableSide.name}");
        
        Debug.Log("✅ WorldGenerator should now render crafting tables with proper side textures!");
        Debug.Log("   You may need to destroy and place crafting tables again to see the changes.");
    }
}
