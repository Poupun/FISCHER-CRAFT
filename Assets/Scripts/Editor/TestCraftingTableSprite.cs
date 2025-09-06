using UnityEngine;
using UnityEditor;

public class TestCraftingTableSprite : EditorWindow
{
    [MenuItem("Tools/Block System/Test CraftingTable Sprite")]
    public static void ShowWindow()
    {
        TestCraftingTable();
    }
    
    static void TestCraftingTable()
    {
        Debug.Log("=== TESTING CRAFTING TABLE SPRITE ===");
        
        // Test if BlockManager can find CraftingTable
        var blockConfig = BlockManager.GetBlockConfiguration(BlockType.CraftingTable);
        if (blockConfig == null)
        {
            Debug.LogError("❌ BlockManager cannot find CraftingTable configuration!");
            return;
        }
        
        Debug.Log($"✅ Found CraftingTable configuration: {blockConfig.displayName}");
        Debug.Log($"   • Main texture: {blockConfig.mainTexture?.name}");
        Debug.Log($"   • Has multiple sides: {blockConfig.hasMultipleSides}");
        
        // Test sprite generation
        var sprite = BlockManager.GetBlockSprite(BlockType.CraftingTable);
        if (sprite == null)
        {
            Debug.LogError("❌ Failed to generate sprite for CraftingTable!");
            
            // Check why sprite generation failed
            if (blockConfig.mainTexture == null)
            {
                Debug.LogError("   • Main texture is null!");
            }
            else
            {
                Debug.Log($"   • Main texture exists: {blockConfig.mainTexture.name}");
                Debug.Log($"   • Texture size: {blockConfig.mainTexture.width}x{blockConfig.mainTexture.height}");
            }
        }
        else
        {
            Debug.Log($"✅ Successfully generated sprite: {sprite.name}");
            Debug.Log($"   • Sprite size: {sprite.rect.width}x{sprite.rect.height}");
            Debug.Log($"   • Source texture: {sprite.texture.name}");
        }
        
        // Test face textures
        if (blockConfig.hasMultipleSides)
        {
            Debug.Log("✅ Testing face textures:");
            Debug.Log($"   • Top: {blockConfig.topTexture?.name}");
            Debug.Log($"   • Front: {blockConfig.frontTexture?.name}");
            Debug.Log($"   • Side: {blockConfig.sideTexture?.name}");
            Debug.Log($"   • Bottom: {blockConfig.bottomTexture?.name}");
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }
}
