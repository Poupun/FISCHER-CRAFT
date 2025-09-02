using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(CreateBlockAssets))]
public class BlockSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CreateBlockAssets createBlockAssets = (CreateBlockAssets)target;
        
        GUILayout.Space(10);
        GUILayout.Label("Block System Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create All Block Assets", GUILayout.Height(30)))
        {
            createBlockAssets.CreateAllBlockAssets();
        }
        
        if (GUILayout.Button("Setup BlockManager with Assets", GUILayout.Height(30)))
        {
            SetupBlockManager();
        }
        
        if (GUILayout.Button("Complete Block System Setup", GUILayout.Height(40)))
        {
            // Do both operations in sequence
            createBlockAssets.CreateAllBlockAssets();
            EditorUtility.DisplayProgressBar("Setting up Block System", "Creating assets...", 0.5f);
            
            // Small delay to ensure assets are created
            System.Threading.Thread.Sleep(500);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayProgressBar("Setting up Block System", "Configuring BlockManager...", 0.8f);
            SetupBlockManager();
            
            EditorUtility.ClearProgressBar();
            Debug.Log("Block system setup complete!");
        }
    }
    
    void SetupBlockManager()
    {
        // Find the BlockManager in the scene
        BlockManager blockManager = FindObjectOfType<BlockManager>();
        if (blockManager == null)
        {
            Debug.LogError("No BlockManager found in scene!");
            return;
        }
        
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
        
        Debug.Log($"BlockManager configured with {blockConfigs.Count} block assets");
        
        // Mark the scene as dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(blockManager.gameObject.scene);
    }
}
