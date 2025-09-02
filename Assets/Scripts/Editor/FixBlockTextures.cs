using UnityEngine;
using UnityEditor;

public class FixBlockTextures : EditorWindow
{
    [MenuItem("Tools/Block System/Fix All Block Textures")]
    public static void ShowWindow()
    {
        GetWindow<FixBlockTextures>("Fix Block Textures");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Fix Block Textures", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This will automatically assign textures to all block assets based on available textures in your project.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix All Block Textures", GUILayout.Height(40)))
        {
            FixAllBlockTextures();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("List Available Textures", GUILayout.Height(30)))
        {
            ListAvailableTextures();
        }
    }
    
    static void FixAllBlockTextures()
    {
        // Find all BlockConfiguration assets
        string[] assetGUIDs = AssetDatabase.FindAssets("t:BlockConfiguration", new[] { "Assets/Data/Blocks" });
        int fixedCount = 0;
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var blockConfig = AssetDatabase.LoadAssetAtPath<BlockConfiguration>(assetPath);
            if (blockConfig != null)
            {
                bool wasFixed = FixBlockTexture(blockConfig);
                if (wasFixed)
                {
                    fixedCount++;
                    EditorUtility.SetDirty(blockConfig);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Fixed textures for {fixedCount} block assets!");
    }
    
    static bool FixBlockTexture(BlockConfiguration config)
    {
        bool wasFixed = false;
        
        switch (config.blockType)
        {
            case BlockType.Grass:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Resources/Textures/top_grass.png") ?? LoadTexture("Assets/Textures/grass_block_top.png");
                    wasFixed = true;
                }
                if (config.hasMultipleSides)
                {
                    if (config.sideTexture == null)
                    {
                        config.sideTexture = LoadTexture("Assets/Resources/Textures/side_grass.png") ?? LoadTexture("Assets/Textures/grass_block_side.png");
                        wasFixed = true;
                    }
                    if (config.bottomTexture == null)
                    {
                        config.bottomTexture = LoadTexture("Assets/Resources/Textures/dirt.png") ?? LoadTexture("Assets/Textures/dirt.png");
                        wasFixed = true;
                    }
                }
                break;
                
            case BlockType.Dirt:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Resources/Textures/dirt.png") ?? LoadTexture("Assets/Textures/dirt.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Stone:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Resources/Textures/stone.png") ?? LoadTexture("Assets/Textures/stone.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Sand:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Resources/Textures/sand.png") ?? LoadTexture("Assets/Textures/sand.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Log:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Resources/Textures/oak_log.png") ?? LoadTexture("Assets/Textures/oak_log.png");
                    wasFixed = true;
                }
                if (config.hasMultipleSides)
                {
                    if (config.topTexture == null)
                    {
                        config.topTexture = LoadTexture("Assets/Textures/oak_log_top.png");
                        wasFixed = true;
                    }
                    if (config.sideTexture == null)
                    {
                        config.sideTexture = config.mainTexture;
                        wasFixed = true;
                    }
                    if (config.bottomTexture == null)
                    {
                        config.bottomTexture = config.topTexture;
                        wasFixed = true;
                    }
                }
                break;
                
            case BlockType.Leaves:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Resources/Textures/leaves.png") ?? LoadTexture("Assets/Textures/oak_leaves.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.WoodPlanks:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/oak_planks.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Stick:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/item/stick.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.CraftingTable:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/crafting_table_front.png");
                    wasFixed = true;
                }
                if (config.hasMultipleSides)
                {
                    if (config.topTexture == null)
                    {
                        config.topTexture = LoadTexture("Assets/Textures/crafting_table_top.png");
                        wasFixed = true;
                    }
                    if (config.sideTexture == null)
                    {
                        config.sideTexture = LoadTexture("Assets/Textures/crafting_table_side.png");
                        wasFixed = true;
                    }
                    if (config.bottomTexture == null)
                    {
                        config.bottomTexture = LoadTexture("Assets/Textures/oak_planks.png");
                        wasFixed = true;
                    }
                    
                    // Use reflection to access new face texture fields
                    var configType = typeof(BlockConfiguration);
                    
                    // Front face (main crafting table texture)
                    var frontField = configType.GetField("frontTexture");
                    if (frontField != null && frontField.GetValue(config) == null)
                    {
                        frontField.SetValue(config, LoadTexture("Assets/Textures/crafting_table_front.png"));
                        wasFixed = true;
                    }
                    
                    // Side faces (generic side texture)
                    var leftField = configType.GetField("leftTexture");
                    if (leftField != null && leftField.GetValue(config) == null)
                    {
                        leftField.SetValue(config, LoadTexture("Assets/Textures/crafting_table_side.png"));
                        wasFixed = true;
                    }
                    
                    var rightField = configType.GetField("rightTexture");
                    if (rightField != null && rightField.GetValue(config) == null)
                    {
                        rightField.SetValue(config, LoadTexture("Assets/Textures/crafting_table_side.png"));
                        wasFixed = true;
                    }
                    
                    var backField = configType.GetField("backTexture");
                    if (backField != null && backField.GetValue(config) == null)
                    {
                        backField.SetValue(config, LoadTexture("Assets/Textures/crafting_table_side.png"));
                        wasFixed = true;
                    }
                }
                break;
                
            case BlockType.Coal:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/coal_ore.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Iron:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/iron_ore.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Gold:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/gold_ore.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Diamond:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/diamond_ore.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Bedrock:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/bedrock.png");
                    wasFixed = true;
                }
                break;
                
            case BlockType.Gravel:
                if (config.mainTexture == null)
                {
                    config.mainTexture = LoadTexture("Assets/Textures/gravel.png");
                    wasFixed = true;
                }
                break;
        }
        
        if (wasFixed)
        {
            Debug.Log($"Fixed textures for {config.blockType} ({config.displayName})");
        }
        
        return wasFixed;
    }
    
    static Texture2D LoadTexture(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    
    static void ListAvailableTextures()
    {
        Debug.Log("=== Available Block Textures ===");
        
        string[] resourceTextures = { "dirt", "stone", "sand", "top_grass", "side_grass", "leaves", "log", "oak_log" };
        foreach (string texName in resourceTextures)
        {
            var tex = LoadTexture($"Assets/Resources/Textures/{texName}.png");
            Debug.Log($"Resources/Textures/{texName}.png: {(tex != null ? "✅ Found" : "❌ Missing")}");
        }
        
        string[] mainTextures = { "oak_planks", "oak_log", "oak_log_top", "oak_leaves", "coal_ore", "iron_ore", "gold_ore", "diamond_ore", "bedrock", "gravel" };
        foreach (string texName in mainTextures)
        {
            var tex = LoadTexture($"Assets/Textures/{texName}.png");
            Debug.Log($"Assets/Textures/{texName}.png: {(tex != null ? "✅ Found" : "❌ Missing")}");
        }
        
        string[] itemTextures = { "stick" };
        foreach (string texName in itemTextures)
        {
            var tex = LoadTexture($"Assets/Textures/item/{texName}.png");
            Debug.Log($"Assets/Textures/item/{texName}.png: {(tex != null ? "✅ Found" : "❌ Missing")}");
        }
        
        string[] craftingTextures = { "crafting_table_top", "crafting_table_front", "crafting_table_side" };
        foreach (string texName in craftingTextures)
        {
            var tex = LoadTexture($"Assets/Textures/{texName}.png");
            Debug.Log($"Assets/Textures/{texName}.png: {(tex != null ? "✅ Found" : "❌ Missing")}");
        }
    }
}
