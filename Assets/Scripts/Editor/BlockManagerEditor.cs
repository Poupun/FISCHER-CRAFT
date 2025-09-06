using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(BlockManager))]
public class BlockManagerEditor : Editor
{
    private SerializedProperty allBlocksProperty;
    private SerializedProperty autoLoadBlockAssetsProperty;
    private SerializedProperty blockAssetsPathProperty;
    private SerializedProperty showHardnessTweakerProperty;
    private SerializedProperty hardnessOverridesProperty;
    
    private bool showDefaultInspector = false;
    private Vector2 scrollPosition;
    
    void OnEnable()
    {
        allBlocksProperty = serializedObject.FindProperty("allBlocks");
        autoLoadBlockAssetsProperty = serializedObject.FindProperty("autoLoadBlockAssets");
        blockAssetsPathProperty = serializedObject.FindProperty("blockAssetsPath");
        showHardnessTweakerProperty = serializedObject.FindProperty("showHardnessTweaker");
        hardnessOverridesProperty = serializedObject.FindProperty("hardnessOverrides");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        BlockManager blockManager = (BlockManager)target;
        
        // Header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Block Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Basic settings
        EditorGUILayout.PropertyField(allBlocksProperty);
        EditorGUILayout.PropertyField(autoLoadBlockAssetsProperty);
        EditorGUILayout.PropertyField(blockAssetsPathProperty);
        
        EditorGUILayout.Space();
        
        // Hardness tweaking section
        EditorGUILayout.PropertyField(showHardnessTweakerProperty);
        
        if (showHardnessTweakerProperty.boolValue)
        {
            DrawHardnessTweaker(blockManager);
        }
        
        EditorGUILayout.Space();
        
        // Debug and utility buttons
        DrawUtilityButtons(blockManager);
        
        // Option to show default inspector
        EditorGUILayout.Space();
        showDefaultInspector = EditorGUILayout.Foldout(showDefaultInspector, "Show Default Inspector");
        if (showDefaultInspector)
        {
            DrawDefaultInspector();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    void DrawHardnessTweaker(BlockManager blockManager)
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("⚒️ Block Hardness Tweaker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Adjust block hardness values in real-time. Higher values = slower mining. Changes apply immediately in play mode.", MessageType.Info);
        
        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to Defaults"))
        {
            if (EditorUtility.DisplayDialog("Reset Hardness Values", 
                "This will reset all hardness values to their defaults. Are you sure?", 
                "Reset", "Cancel"))
            {
                ResetHardnessToDefaults(blockManager);
            }
        }
        
        if (Application.isPlaying && GUILayout.Button("Refresh Overrides"))
        {
            blockManager.RefreshHardnessOverrides();
        }
        
        if (GUILayout.Button("Print Debug Info"))
        {
            Debug.Log(BlockManager.GetHardnessDebugInfo());
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Hardness overrides list
        if (hardnessOverridesProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No hardness overrides found. Click 'Reset to Defaults' to create them.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField($"Block Hardness Values ({hardnessOverridesProperty.arraySize} blocks):", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            for (int i = 0; i < hardnessOverridesProperty.arraySize; i++)
            {
                SerializedProperty overrideProperty = hardnessOverridesProperty.GetArrayElementAtIndex(i);
                if (overrideProperty != null)
                {
                    DrawHardnessOverride(overrideProperty, i);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Block"))
            {
                hardnessOverridesProperty.arraySize++;
                SerializedProperty newOverride = hardnessOverridesProperty.GetArrayElementAtIndex(hardnessOverridesProperty.arraySize - 1);
                SerializedProperty blockTypeProperty = newOverride.FindPropertyRelative("blockType");
                SerializedProperty hardnessProperty = newOverride.FindPropertyRelative("hardness");
                SerializedProperty displayNameProperty = newOverride.FindPropertyRelative("displayName");
                
                blockTypeProperty.enumValueIndex = 0;
                hardnessProperty.floatValue = 1.0f;
                displayNameProperty.stringValue = "New Block";
            }
            
            if (hardnessOverridesProperty.arraySize > 0 && GUILayout.Button("Remove Last"))
            {
                hardnessOverridesProperty.arraySize--;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawHardnessOverride(SerializedProperty overrideProperty, int index)
    {
        EditorGUILayout.BeginVertical("box");
        
        SerializedProperty blockTypeProperty = overrideProperty.FindPropertyRelative("blockType");
        SerializedProperty hardnessProperty = overrideProperty.FindPropertyRelative("hardness");
        SerializedProperty isUnbreakableProperty = overrideProperty.FindPropertyRelative("isUnbreakable");
        SerializedProperty displayNameProperty = overrideProperty.FindPropertyRelative("displayName");
        
        EditorGUILayout.BeginHorizontal();
        
        // Block type
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(blockTypeProperty, GUIContent.none, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck())
        {
            // Update display name when block type changes
            displayNameProperty.stringValue = ((BlockType)blockTypeProperty.enumValueIndex).ToString();
        }
        
        // Display name
        EditorGUILayout.PropertyField(displayNameProperty, GUIContent.none, GUILayout.Width(100));
        
        // Hardness slider
        EditorGUI.BeginDisabledGroup(isUnbreakableProperty.boolValue);
        EditorGUILayout.LabelField("Hardness:", GUILayout.Width(60));
        hardnessProperty.floatValue = EditorGUILayout.Slider(hardnessProperty.floatValue, 0f, 10f);
        EditorGUI.EndDisabledGroup();
        
        // Unbreakable toggle
        isUnbreakableProperty.boolValue = EditorGUILayout.Toggle("Unbreakable", isUnbreakableProperty.boolValue, GUILayout.Width(80));
        
        // Remove button
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            hardnessOverridesProperty.DeleteArrayElementAtIndex(index);
            return;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Visual feedback
        if (isUnbreakableProperty.boolValue)
        {
            EditorGUILayout.HelpBox("This block cannot be broken", MessageType.Warning);
        }
        else if (hardnessProperty.floatValue == 0f)
        {
            EditorGUILayout.HelpBox("Instant break", MessageType.Info);
        }
        else if (hardnessProperty.floatValue > 5f)
        {
            EditorGUILayout.HelpBox("Very hard to break", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawUtilityButtons(BlockManager blockManager)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Reload Block Assets"))
        {
            // Trigger asset reload
            if (Application.isPlaying)
            {
                Debug.Log("Cannot reload assets during play mode. Stop play mode first.");
            }
            else
            {
                EditorUtility.SetDirty(blockManager);
                AssetDatabase.Refresh();
            }
        }
        
        if (Application.isPlaying && GUILayout.Button("Test Mining Speeds"))
        {
            TestMiningSpeedsWithTools();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void ResetHardnessToDefaults(BlockManager blockManager)
    {
        // This will trigger the default creation in the next initialization
        hardnessOverridesProperty.arraySize = 0;
        serializedObject.ApplyModifiedProperties();
        
        // If in play mode, immediately refresh
        if (Application.isPlaying)
        {
            System.Reflection.MethodInfo method = typeof(BlockManager).GetMethod("CreateDefaultHardnessOverrides", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(blockManager, null);
            blockManager.RefreshHardnessOverrides();
        }
        
        // Refresh the serialized object to show the new values
        serializedObject.Update();
    }
    
    void TestMiningSpeedsWithTools()
    {
        var testResults = new System.Text.StringBuilder();
        testResults.AppendLine("=== Mining Speed Test Results ===");
        
        BlockType[] testBlocks = { BlockType.Stone, BlockType.Log, BlockType.Dirt, BlockType.Diamond };
        ItemType[] testTools = { ItemType.IronPickaxe, ItemType.IronAxe, ItemType.IronShovel };
        
        foreach (var block in testBlocks)
        {
            testResults.AppendLine($"\\n{block}:");
            float baseTime = BlockManager.GetMiningTime(block);
            testResults.AppendLine($"  Hand: {baseTime:F2}s");
            
            foreach (var tool in testTools)
            {
                float multiplier = ToolEffectivenessSystem.GetMiningSpeedMultiplier(tool, block);
                float toolTime = baseTime / multiplier;
                string optimal = ToolEffectivenessSystem.IsToolOptimalForBlock(tool, block) ? " ⭐" : "";
                testResults.AppendLine($"  {tool}: {toolTime:F2}s ({multiplier:F1}x){optimal}");
            }
        }
        
        Debug.Log(testResults.ToString());
    }
}