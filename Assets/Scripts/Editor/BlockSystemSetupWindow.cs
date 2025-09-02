using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BlockSystemSetupWindow : EditorWindow
{
    [MenuItem("Tools/Block System/Open Setup Window")]
    public static void ShowWindow()
    {
        GetWindow<BlockSystemSetupWindow>("Block System Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Block System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Create Block Assets", GUILayout.Height(30)))
        {
            CreateBlockAssets();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Setup BlockManager", GUILayout.Height(30)))
        {
            SetupBlockManager();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Do Everything (Full Setup)", GUILayout.Height(40)))
        {
            CreateBlockAssets();
            AssetDatabase.Refresh();
            EditorApplication.delayCall += SetupBlockManager;
        }
        
        GUILayout.Space(20);
        
        // Status
        BlockManager blockManager = FindObjectOfType<BlockManager>();
        if (blockManager != null)
        {
            SerializedObject so = new SerializedObject(blockManager);
            SerializedProperty allBlocksProperty = so.FindProperty("allBlocks");
            
            GUILayout.Label($"BlockManager Status: Found with {allBlocksProperty.arraySize} blocks configured", EditorStyles.helpBox);
        }
        else
        {
            GUILayout.Label("BlockManager Status: Not found in scene!", EditorStyles.helpBox);
        }
        
        // Show available block assets
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        GUILayout.Label($"Block Assets Found: {assetGUIDs.Length}", EditorStyles.helpBox);
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                GUILayout.Label($"  • {blockConfig.blockType} - {blockConfig.displayName}", EditorStyles.miniLabel);
            }
        }
    }
    
    void CreateBlockAssets()
    {
        CreateBlockAssets createBlockAssets = FindObjectOfType<CreateBlockAssets>();
        if (createBlockAssets != null)
        {
            createBlockAssets.CreateAllBlockAssets();
            Debug.Log("Block assets created successfully!");
        }
        else
        {
            Debug.LogError("No CreateBlockAssets component found in the scene!");
        }
    }
    
    void SetupBlockManager()
    {
        BlockManager blockManager = FindObjectOfType<BlockManager>();
        if (blockManager == null)
        {
            Debug.LogError("No BlockManager found in scene!");
            return;
        }
        
        // Load all BlockConfiguration assets
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        var blockConfigs = new List<BlockConfiguration>();
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                blockConfigs.Add(blockConfig);
            }
        }
        
        if (blockConfigs.Count == 0)
        {
            Debug.LogWarning("No BlockConfiguration assets found in Assets/Data/Blocks");
            return;
        }
        
        // Sort by block type for consistent ordering
        blockConfigs.Sort((a, b) => a.blockType.CompareTo(b.blockType));
        
        // Use SerializedObject to modify the BlockManager
        SerializedObject serializedBlockManager = new SerializedObject(blockManager);
        SerializedProperty allBlocksProperty = serializedBlockManager.FindProperty("allBlocks");
        
        // Set the array size
        allBlocksProperty.arraySize = blockConfigs.Count;
        
        // Assign each block configuration
        for (int i = 0; i < blockConfigs.Count; i++)
        {
            SerializedProperty elementProperty = allBlocksProperty.GetArrayElementAtIndex(i);
            elementProperty.objectReferenceValue = blockConfigs[i];
        }
        
        // Apply the changes
        serializedBlockManager.ApplyModifiedProperties();
        
        Debug.Log($"✅ BlockManager configured with {blockConfigs.Count} block assets:");
        foreach (var config in blockConfigs)
        {
            Debug.Log($"  • {config.blockType} - {config.displayName}");
        }
        
        // Mark the scene as dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(blockManager.gameObject.scene);
        
        // Refresh the window
        Repaint();
    }
}
