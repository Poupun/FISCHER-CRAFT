using UnityEngine;
using UnityEditor;

public class RecreateBlockAssets : EditorWindow
{
    [MenuItem("Tools/Block System/Recreate CraftingTable Asset")]
    public static void ShowWindow()
    {
        RecreateCraftingTableAsset();
    }
    
    static void RecreateCraftingTableAsset()
    {
        Debug.Log("=== RECREATING CRAFTING TABLE ASSET ===");
        
        // Create new BlockConfiguration asset
        var asset = ScriptableObject.CreateInstance<BlockConfiguration>();
        asset.blockType = BlockType.CraftingTable;
        asset.displayName = "Crafting Table";
        asset.tintColor = Color.white;
        asset.hardness = 0.8f;
        asset.hasMultipleSides = true;
        asset.isUnbreakable = false;
        asset.isPlaceable = true;
        asset.isCraftable = true;
        asset.isMineable = true;
        
        // Assign textures
        asset.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
        asset.topTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_top.png");
        asset.sideTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        asset.bottomTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/oak_planks.png");
        
        // Set face-specific textures
        asset.frontTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
        asset.leftTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        asset.rightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        asset.backTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        
        // Save the asset
        string assetPath = "Assets/Data/Blocks/CraftingTableBlock.asset";
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Recreated CraftingTable asset with face textures:");
        Debug.Log($"   • Main: {asset.mainTexture?.name}");
        Debug.Log($"   • Top: {asset.topTexture?.name}");
        Debug.Log($"   • Front: {asset.frontTexture?.name}");
        Debug.Log($"   • Sides: {asset.leftTexture?.name}");
        Debug.Log($"   • Bottom: {asset.bottomTexture?.name}");
    }
}
