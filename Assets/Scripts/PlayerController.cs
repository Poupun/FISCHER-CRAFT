using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")] public float walkSpeed = 5f; public float sprintSpeed = 8f; public float crouchSpeed = 1f; public float mouseSensitivity = 2f; public float jumpForce = 5f;
    [Header("Sprint Settings")] public KeyCode sprintKey = KeyCode.LeftShift; public float sprintFOVIncrease = 10f; public float fovTransitionSpeed = 8f;
    [Header("Crouch Settings")] public KeyCode crouchKey = KeyCode.LeftControl; public float crouchHeight = 1.3f; public float standHeight = 1.8f; public float crouchTransitionSpeed = 10f;
    [Header("Interaction")] public float interactionRange = 6f; public LayerMask blockLayerMask = -1;
    [Header("Drops")] public bool grassDropsDirt = true; // if true, breaking grass gives dirt (Minecraft-like); otherwise drops grass block

    // Components
    private CharacterController characterController; private Camera playerCamera; private WorldGenerator worldGenerator; private PlayerInventory playerInventory;

    // Movement state
    private Vector3 velocity; private float xRotation = 0f; private bool isSprinting; private bool isCrouching; private float currentSpeed; private float baseFOV; private float targetHeight;

    // Target highlight removed

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        playerInventory = GetComponent<PlayerInventory>() ?? gameObject.AddComponent<PlayerInventory>();
        baseFOV = playerCamera.fieldOfView; targetHeight = standHeight; currentSpeed = walkSpeed; Cursor.lockState = CursorLockMode.Locked;
    }

    // Update method moved below to include dropped item hover checking

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity; float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity; xRotation -= mouseY; xRotation = Mathf.Clamp(xRotation,-90f,90f); playerCamera.transform.localRotation = Quaternion.Euler(xRotation,0f,0f); transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovementState()
    {
        float horizontal = Input.GetAxis("Horizontal"); float vertical = Input.GetAxis("Vertical"); bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        if (Input.GetKeyDown(crouchKey)) { isCrouching = !isCrouching; targetHeight = isCrouching ? crouchHeight : standHeight; }
        if (Input.GetKey(sprintKey) && !isCrouching && isMoving && characterController.isGrounded) { isSprinting = true; currentSpeed = sprintSpeed; }
        else if (isCrouching) { isSprinting = false; currentSpeed = crouchSpeed; }
        else { isSprinting = false; currentSpeed = walkSpeed; }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal"); float vertical = Input.GetAxis("Vertical"); Vector3 direction = (transform.right * horizontal + transform.forward * vertical).normalized; Vector3 move = direction * currentSpeed;
        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f; if (Input.GetButtonDown("Jump") && characterController.isGrounded && !isCrouching) velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y); velocity.y += Physics.gravity.y * Time.deltaTime; move.y = velocity.y; characterController.Move(move * Time.deltaTime);
    }

    void UpdateCrouchHeight()
    {
        float newHeight = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed); characterController.height = newHeight; Vector3 c = characterController.center; c.y = newHeight/2f; characterController.center = c; Vector3 camPos = playerCamera.transform.localPosition; float targetY = (isCrouching?crouchHeight:standHeight)*0.9f; camPos.y = Mathf.Lerp(camPos.y,targetY,Time.deltaTime * crouchTransitionSpeed); playerCamera.transform.localPosition = camPos;
    }

    void UpdateSprintFOV()
    { float targetFOV = isSprinting ? baseFOV + sprintFOVIncrease : baseFOV; playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView,targetFOV,Time.deltaTime * fovTransitionSpeed); }

    void HandleInteraction()
    { 
        // Mining is now handled by MiningSystem component - no more instant breaking
        // Handle right-click for block placement with dropped item priority
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("PlayerController: Right-click detected");
            
            // First check if we're clicking on a dropped item
            if (TryPickupDroppedItem())
            {
                Debug.Log("PlayerController: Picked up dropped item, skipping block placement");
                return;
            }
            
            // If no dropped item was clicked, place a block
            Debug.Log("PlayerController: No dropped item clicked, placing block");
            PlaceBlock();
        }
    }
    
    private bool TryPickupDroppedItem()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange);
        
        Debug.Log($"PlayerController: Checking {hits.Length} raycast hits for dropped items");
        
        // Sort hits by distance (closest first)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        foreach (RaycastHit hit in hits)
        {
            DroppedItem droppedItem = hit.collider.GetComponent<DroppedItem>();
            if (droppedItem != null && !droppedItem.isBeingPickedUp)
            {
                Debug.Log($"PlayerController: Found dropped item {droppedItem.itemType} at distance {hit.distance}");
                droppedItem.TryPickup();
                return true;
            }
        }
        
        Debug.Log("PlayerController: No dropped items found in raycast");
        return false;
    }
    
    // Add hover checking for dropped items
    void Update()
    {
        HandleMouseLook(); HandleMovementState(); HandleMovement(); HandleInteraction(); UpdateCrouchHeight(); UpdateSprintFOV();
        
        // Check for dropped item hover every frame
        CheckDroppedItemHover();
    }
    
    private void CheckDroppedItemHover()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange);
        
        // Find the closest dropped item being looked at
        DroppedItem closestDroppedItem = null;
        float closestDistance = float.MaxValue;
        
        foreach (RaycastHit hit in hits)
        {
            DroppedItem droppedItem = hit.collider.GetComponent<DroppedItem>();
            if (droppedItem != null && !droppedItem.isBeingPickedUp && hit.distance < closestDistance)
            {
                closestDroppedItem = droppedItem;
                closestDistance = hit.distance;
            }
        }
        
        // Reset all dropped items hover state first, then set the closest one
        DroppedItem[] allDroppedItems = FindObjectsByType<DroppedItem>(FindObjectsSortMode.None);
        foreach (DroppedItem item in allDroppedItems)
        {
            bool shouldBeHovered = (item == closestDroppedItem);
            if (item.isHovered != shouldBeHovered)
            {
                if (shouldBeHovered)
                {
                    Debug.Log($"PlayerController: Setting {item.itemType} to hovered");
                }
                else
                {
                    Debug.Log($"PlayerController: Removing hover from {item.itemType}");
                }
                item.SetHovered(shouldBeHovered);
            }
        }
    }
    
    public void PlaceBlockFromInteractionManager(Ray ray)
    {
        PlaceBlock(); // Use existing block placement logic
    }

    void BreakBlock()
    {
        // Remove plant first if cursor hits a plant cell
        if (TryRemovePlant()) return;
        if (AcquireTargetCell(out var cell,out var type))
    { 
        BlockType drop = (type == BlockType.Grass && grassDropsDirt) ? BlockType.Dirt : type; 
        worldGenerator.PlaceBlock(cell, BlockType.Air); 
        playerInventory?.AddBlock(drop,1); 
        return; 
    }
    }

    bool TryRemovePlant()
    {
        if (worldGenerator == null) return false; Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2,Screen.height/2,0)); const float step=0.05f; float maxD = Mathf.Min(interactionRange,8f); for(float d=0; d<=maxD; d+=step){ Vector3 p = ray.origin + ray.direction*d; Vector3Int c = new Vector3Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z)); if (worldGenerator.HasBatchedPlantAt(c)){ worldGenerator.RemoveBatchedPlantAt(c); return true; } }
        return false;
    }

    bool AcquireTargetCell(out Vector3Int cell, out BlockType type)
    {
        cell = default; type = BlockType.Air; if (worldGenerator == null) return false; Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2,Screen.height/2,0));
        if (worldGenerator.useChunkStreaming && worldGenerator.useChunkMeshing)
        { Vector3Int hitCell, placeCell; Vector3 hitNormal; if (worldGenerator.TryVoxelRaycast(ray, interactionRange, out hitCell, out placeCell, out hitNormal)) { var t = worldGenerator.GetBlockType(hitCell); if (t != BlockType.Air) { cell = hitCell; type = t; return true; } } }
        const float step=0.075f; float maxD = Mathf.Min(interactionRange,8f); for(float d=0; d<=maxD; d+=step){ Vector3 p = ray.origin + ray.direction*d; Vector3Int c = new Vector3Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z)); if (c.y < 0 || c.y >= worldGenerator.worldHeight) continue; var bt = worldGenerator.GetBlockType(c); if (bt != BlockType.Air){ cell = c; type = bt; return true; } }
        return false;
    }

    void PlaceBlock()
    {
        Debug.Log("PlayerController: PlaceBlock called");
        
        if (playerInventory == null) 
        {
            Debug.Log("PlayerController: No playerInventory found");
            return; 
        }
        
        if (!playerInventory.HasBlockForPlacement(out BlockType placeType)) 
        {
            Debug.Log("PlayerController: No block available for placement");
            return; 
        }
        
        Debug.Log($"PlayerController: Attempting to place {placeType}");
        
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2,Screen.height/2,0));
        if (worldGenerator != null && worldGenerator.useChunkStreaming && worldGenerator.useChunkMeshing)
        { 
            Debug.Log("PlayerController: Using voxel raycast for block placement");
            Vector3Int hitCell, placeCell; Vector3 hitNormal; 
            if (worldGenerator.TryVoxelRaycast(ray, interactionRange, out hitCell, out placeCell, out hitNormal)) 
            { 
                Vector3Int pos = placeCell; 
                Debug.Log($"PlayerController: Voxel raycast hit, placing at {pos}");
                if (characterController){ Bounds bb = new Bounds((Vector3)pos, Vector3.one); if (bb.Intersects(characterController.bounds)) { Debug.Log("PlayerController: Cannot place - intersects player bounds"); return; } } 
                if (worldGenerator.GetBlockType(pos) == BlockType.Air)
                { 
                    Debug.Log($"PlayerController: Placing {placeType} at {pos}");
                    worldGenerator.PlaceBlock(pos, placeType); 
                    playerInventory.ConsumeOneFromSelected(); 
                } 
                else
                {
                    Debug.Log($"PlayerController: Cannot place - position occupied by {worldGenerator.GetBlockType(pos)}");
                }
                return; 
            } 
            else
            {
                Debug.Log("PlayerController: Voxel raycast missed");
            }
        }
        // Physics fallback (plants / colliders)
        Debug.Log("PlayerController: Trying physics raycast fallback");
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, blockLayerMask))
        { 
            Debug.Log($"PlayerController: Physics raycast hit {hit.collider.name}");
            Vector3Int pos; 
            BlockInfo bi = hit.collider.GetComponent<BlockInfo>(); 
            if (bi != null)
            { 
                Vector3 hp = hit.point + hit.normal*0.5f; 
                pos = Vector3Int.RoundToInt(hp); 
            } 
            else 
            { 
                pos = Vector3Int.RoundToInt(hit.collider.bounds.center); 
            } 
            if (characterController)
            { 
                Bounds bb = new Bounds((Vector3)pos,Vector3.one); 
                if (bb.Intersects(characterController.bounds)) 
                {
                    Debug.Log("PlayerController: Physics fallback - Cannot place, intersects player bounds");
                    return; 
                }
            } 
            if (worldGenerator != null && worldGenerator.GetBlockType(pos) == BlockType.Air)
            { 
                Debug.Log($"PlayerController: Physics fallback - Placing {placeType} at {pos}");
                worldGenerator.PlaceBlock(pos, placeType); 
                playerInventory.ConsumeOneFromSelected(); 
            }
            else
            {
                Debug.Log($"PlayerController: Physics fallback - Cannot place, position occupied by {worldGenerator?.GetBlockType(pos)}");
            }
        }
        else
        {
            Debug.Log("PlayerController: Physics raycast fallback missed");
        }
    }

    // Block highlighting system removed

    // Public getters
    public bool IsSprinting() => isSprinting; public bool IsCrouching() => isCrouching; public float GetCurrentSpeed() => currentSpeed;
}