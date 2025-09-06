using UnityEngine;

public class CraftingTableInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 6f;
    
    private Camera playerCamera;
    private InventoryManager inventoryManager;
    private WorldGenerator worldGenerator;
    
    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        worldGenerator = FindFirstObjectByType<WorldGenerator>();
    }
    
    void Update()
    {
        // Check for right-click on crafting table
        if (Input.GetMouseButtonDown(1))
        {
            if (TryInteractWithCraftingTable())
            {
                // If we successfully interacted with a crafting table, don't process block placement
                return;
            }
        }
    }
    
    bool TryInteractWithCraftingTable()
    {
        if (playerCamera == null || inventoryManager == null || worldGenerator == null)
            return false;
            
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
        
        // Use the same voxel raycast system as the player controller
        if (worldGenerator.useChunkStreaming && worldGenerator.useChunkMeshing)
        {
            Vector3Int hitCell, placeCell;
            Vector3 hitNormal;
            if (worldGenerator.TryVoxelRaycast(ray, interactionRange, out hitCell, out placeCell, out hitNormal))
            {
                BlockType blockType = worldGenerator.GetBlockType(hitCell);
                if (blockType == BlockType.CraftingTable)
                {
                    Debug.Log("CraftingTableInteraction: Player right-clicked on crafting table, opening table crafting interface");
                    inventoryManager.OpenInventoryWithTableCrafting();
                    return true;
                }
            }
        }
        else
        {
            // Fallback raycast method
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                Vector3Int blockPosition = Vector3Int.RoundToInt(hit.collider.bounds.center);
                BlockType blockType = worldGenerator.GetBlockType(blockPosition);
                if (blockType == BlockType.CraftingTable)
                {
                    Debug.Log("CraftingTableInteraction: Player right-clicked on crafting table (fallback), opening table crafting interface");
                    inventoryManager.OpenInventoryWithTableCrafting();
                    return true;
                }
            }
        }
        
        return false;
    }
}