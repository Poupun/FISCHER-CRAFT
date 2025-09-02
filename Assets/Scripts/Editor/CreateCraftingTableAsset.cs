using UnityEngine;
using UnityEditor;

public class CreateCraftingTableAsset
{
    [MenuItem("Tools/Block System/Create CraftingTable Asset")]
    public static void CreateCraftingTableAssetManual()
    {
        Debug.Log("=== CREATING CRAFTING TABLE ASSET ===");
        
        // Ensure directory exists
        string assetPath = "Assets/Data/Blocks";
        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Blocks");
        }
        
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
        
        // Set face-specific textures (these are the new fields!)
        asset.frontTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_front.png");
        asset.leftTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        asset.rightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        asset.backTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/crafting_table_side.png");
        
        // Save the asset
        string fullAssetPath = "Assets/Data/Blocks/CraftingTableBlock.asset";
        AssetDatabase.CreateAsset(asset, fullAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Created CraftingTable asset at {fullAssetPath}");
        Debug.Log($"   • Main: {asset.mainTexture?.name}");
        Debug.Log($"   • Top: {asset.topTexture?.name}");
        Debug.Log($"   • Front: {asset.frontTexture?.name}");
        Debug.Log($"   • Left: {asset.leftTexture?.name}");
        Debug.Log($"   • Right: {asset.rightTexture?.name}");
        Debug.Log($"   • Back: {asset.backTexture?.name}");
        Debug.Log($"   • Bottom: {asset.bottomTexture?.name}");
    }
}
