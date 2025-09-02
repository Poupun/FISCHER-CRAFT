using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoBlockSetup
{
    static AutoBlockSetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Check if we need to auto-setup the block system
            SetupBlockSystemIfNeeded();
        }
    }
    
    [MenuItem("Tools/Block System/Setup Block System")]
    public static void SetupBlockSystemManual()
    {
        SetupBlockSystemIfNeeded();
    }
    
    [MenuItem("Tools/Block System/Create Block Assets")]
    public static void CreateBlockAssetsManual()
    {
        CreateBlockAssets createBlockAssets = Object.FindObjectOfType<CreateBlockAssets>();
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
    
    public static void SetupBlockSystemIfNeeded()
    {
        // Find BlockManager in current scene
        BlockManager blockManager = Object.FindObjectOfType<BlockManager>();
        if (blockManager == null)
        {
            Debug.Log("No BlockManager found in current scene, skipping auto-setup");
            return;
        }
        
        // Check if BlockManager already has blocks configured
        SerializedObject serializedBlockManager = new SerializedObject(blockManager);
        SerializedProperty allBlocksProperty = serializedBlockManager.FindProperty("allBlocks");
        
        if (allBlocksProperty.arraySize > 0)
        {
            Debug.Log("BlockManager already configured with blocks, skipping auto-setup");
            return;
        }
        
        Debug.Log("Auto-setting up block system...");
        
        // Find CreateBlockAssets component
        CreateBlockAssets createBlockAssets = Object.FindObjectOfType<CreateBlockAssets>();
        if (createBlockAssets != null)
        {
            // Create block assets
            createBlockAssets.CreateAllBlockAssets();
            
            // Refresh assets
            AssetDatabase.Refresh();
        }
        
        // Wait a frame for assets to be created
        EditorApplication.delayCall += () => {
            ConfigureBlockManager();
        };
    }
    
    static void ConfigureBlockManager()
    {
        BlockManager blockManager = Object.FindObjectOfType<BlockManager>();
        if (blockManager == null) return;
        
        // Load all BlockConfiguration assets
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        var blockConfigs = new System.Collections.Generic.List<BlockConfiguration>();
        
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
        
        Debug.Log($"✅ Block system auto-setup complete! BlockManager configured with {blockConfigs.Count} block assets");
        
        // Mark the scene as dirty
        EditorSceneManager.MarkSceneDirty(blockManager.gameObject.scene);
    }
}
