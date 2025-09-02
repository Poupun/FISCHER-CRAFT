using UnityEngine;
using UnityEditor;

public class FixWorldGeneratorTextures : EditorWindow
{
    [MenuItem("Tools/Block System/Fix WorldGenerator Crafting Table Textures")]
    public static void ShowWindow()
    {
        FixWorldGeneratorCraftingTableTextures();
    }
    
    static void FixWorldGeneratorCraftingTableTextures()
    {
        Debug.Log("=== FIXING WORLDGENERATOR CRAFTING TABLE TEXTURES ===");
        
        // Find WorldGenerator in scene
        WorldGenerator worldGenerator = Object.FindObjectOfType<WorldGenerator>();
        if (worldGenerator == null)
        {
            Debug.LogError("No WorldGenerator found in scene!");
            return;
        }
        
        // Load the correct textures
        var frontTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
        var sideTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        var topTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_top.png");
        
        if (frontTexture == null)
        {
            Debug.LogError("Could not find crafting_table_front.png!");
            return;
        }
        
        if (sideTexture == null)
        {
            Debug.LogError("Could not find crafting_table_side.png!");
            return;
        }
        
        if (topTexture == null)
        {
            Debug.LogError("Could not find crafting_table_top.png!");
            return;
        }
        
        // Use SerializedObject to modify the WorldGenerator
        SerializedObject serializedWorldGenerator = new SerializedObject(worldGenerator);
        
        // Set the crafting table textures
        SerializedProperty craftingTableTextureProperty = serializedWorldGenerator.FindProperty("craftingTableTexture");
        SerializedProperty craftingTableFrontTextureProperty = serializedWorldGenerator.FindProperty("craftingTableFrontTexture");
        SerializedProperty craftingTableSideTextureProperty = serializedWorldGenerator.FindProperty("craftingTableSideTexture");
        
        if (craftingTableTextureProperty != null)
        {
            craftingTableTextureProperty.objectReferenceValue = topTexture;
        }
        
        if (craftingTableFrontTextureProperty != null)
        {
            craftingTableFrontTextureProperty.objectReferenceValue = frontTexture;
        }
        
        if (craftingTableSideTextureProperty != null)
        {
            craftingTableSideTextureProperty.objectReferenceValue = sideTexture;
        }
        
        // Apply the changes
        serializedWorldGenerator.ApplyModifiedProperties();
        
        Debug.Log("✅ Fixed WorldGenerator crafting table textures:");
        Debug.Log($"   • Top: {topTexture.name}");
        Debug.Log($"   • Front: {frontTexture.name}");
        Debug.Log($"   • Side: {sideTexture.name}");
        
        // Mark the scene as dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(worldGenerator.gameObject.scene);
        
        Debug.Log("✅ WORLDGENERATOR TEXTURES FIXED!");
        Debug.Log("Now crafting tables in the world should have proper side textures!");
    }
}
