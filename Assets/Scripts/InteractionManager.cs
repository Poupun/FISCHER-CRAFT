using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 5f;
    
    private Camera playerCamera;
    
    void Start()
    {
        // Find player camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            HandleRightClick();
        }
    }
    
    private void HandleRightClick()
    {
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange);
        
        // Sort hits by distance (closest first)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        Debug.Log($"InteractionManager: Right click detected, found {hits.Length} hits");
        
        // Check for dropped items first (highest priority)
        foreach (RaycastHit hit in hits)
        {
            DroppedItem droppedItem = hit.collider.GetComponent<DroppedItem>();
            if (droppedItem != null && !droppedItem.isBeingPickedUp)
            {
                Debug.Log($"InteractionManager: Found dropped item {droppedItem.itemType}, attempting pickup");
                droppedItem.TryPickup();
                return; // Consumed the right-click, don't process block placement
            }
        }
        
        // If no dropped items were clicked, allow block placement
        Debug.Log("InteractionManager: No dropped items hit, allowing block placement");
        HandleBlockPlacement(ray);
    }
    
    private void HandleBlockPlacement(Ray ray)
    {
        // Find the active controller and let it handle block placement
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Call the block placement directly
            playerController.PlaceBlockFromInteractionManager(ray);
            return;
        }
        
        FirstPersonController fpController = GetComponent<FirstPersonController>();
        if (fpController != null)
        {
            // For FirstPersonController, we need to simulate the block placement logic
            HandleFirstPersonBlockPlacement(ray);
            return;
        }
    }
    
    private void HandleFirstPersonBlockPlacement(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            Vector3 hitPoint = hit.point + hit.normal * 0.5f;
            Vector3Int blockPosition = Vector3Int.RoundToInt(hitPoint);
            
            // Check distance to avoid placing inside player
            if (Vector3.Distance(blockPosition, transform.position) > 1.5f)
            {
                // Try to get selected item from hotbar/inventory
                PlayerInventory playerInventory = GetComponent<PlayerInventory>();
                if (playerInventory == null)
                    playerInventory = FindFirstObjectByType<PlayerInventory>();
                
                WorldGenerator worldGenerator = FindFirstObjectByType<WorldGenerator>();
                
                if (playerInventory != null && worldGenerator != null)
                {
                    // Check if using voxel raycast system
                    if (worldGenerator.useChunkStreaming && worldGenerator.useChunkMeshing)
                    {
                        Vector3Int hitCell, placeCell;
                        Vector3 hitNormal;
                        if (worldGenerator.TryVoxelRaycast(ray, interactionRange, out hitCell, out placeCell, out hitNormal))
                        {
                            Vector3Int pos = placeCell;
                            if (GetComponent<CharacterController>())
                            {
                                Bounds bb = new Bounds((Vector3)pos, Vector3.one);
                                if (bb.Intersects(GetComponent<CharacterController>().bounds)) return;
                            }
                            if (worldGenerator.GetBlockType(pos) == BlockType.Air)
                            {
                                if (playerInventory.HasBlockForPlacement(out BlockType placeType))
                                {
                                    worldGenerator.PlaceBlock(pos, placeType);
                                    playerInventory.ConsumeOneFromSelected();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback to regular placement
                        if (worldGenerator.GetBlockType(blockPosition) == BlockType.Air)
                        {
                            if (playerInventory.HasBlockForPlacement(out BlockType placeType))
                            {
                                worldGenerator.PlaceBlock(blockPosition, placeType);
                                playerInventory.ConsumeOneFromSelected();
                            }
                        }
                    }
                }
            }
        }
    }
}