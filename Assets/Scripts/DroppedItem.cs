using UnityEngine;
using System.Collections;

public class DroppedItem : MonoBehaviour
{
    [Header("Item Properties")]
    public BlockType itemType;
    public int quantity = 1;
    
    [Header("Physics")]
    public float bounceForce = 3f;
    public float despawnTime = 300f; // 5 minutes like Minecraft
    
    [Header("Pickup")]
    public float interactionRange = 3f; // Range for right-click interaction
    public bool requiresRightClick = true; // Enable manual pickup
    
    [Header("Visual")]
    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;
    
    [Header("Hover Animation")]
    public float hoverScale = 1.1f; // Reduced from 1.2f to be more subtle
    public float hoverAnimSpeed = 3f;
    
    private Rigidbody itemRigidbody;
    private Collider itemCollider;
    private MeshRenderer meshRenderer;
    private Vector3 startPosition;
    private Vector3 baseScale;
    private float lifeTime = 0f;
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private Camera playerCamera;
    public bool isBeingPickedUp = false;
    public bool isHovered = false;
    private Coroutine hoverCoroutine;
    
    void Start()
    {
        // Get components
        itemRigidbody = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Setup physics
        if (itemRigidbody == null)
        {
            itemRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // Create physics collider for solid collision with ground
        if (itemCollider == null)
        {
            itemCollider = gameObject.AddComponent<BoxCollider>();
            itemCollider.isTrigger = false; // Solid for physics
        }
        
        // Add initial bounce
        Vector3 randomBounce = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * bounceForce;
        
        itemRigidbody.AddForce(randomBounce, ForceMode.Impulse);
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = FindFirstObjectByType<FirstPersonController>()?.gameObject;
        
        if (player != null)
        {
            playerTransform = player.transform;
            playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            
            // Get player camera
            playerCamera = player.GetComponentInChildren<Camera>();
        }
        
        startPosition = transform.position;
        
        // Set up visual appearance based on item type FIRST
        SetupVisualAppearance();
        
        // THEN store the actual scale after it's been set to 0.3f
        baseScale = transform.localScale;
        Debug.Log($"DroppedItem: {itemType} baseScale set to {baseScale}");
        
        // Start despawn timer
        StartCoroutine(DespawnAfterTime());
    }
    
    void Update()
    {
        lifeTime += Time.deltaTime;
        
        // Check for block collision and slide away if stuck
        CheckBlockCollision();
        
        // Visual effects
        UpdateVisualEffects();
        
        // Hover state is now handled by PlayerController for better reliability
        // No need to check hover state here anymore
    }
    
    private void CheckHoverState()
    {
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange);
        
        bool wasHovered = isHovered;
        isHovered = false;
        
        // Check all hits for this dropped item
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.collider == itemCollider)
            {
                isHovered = true;
                if (!wasHovered) // Only log when we start hovering
                {
                    Debug.Log($"DroppedItem: Now hovering over {itemType} item at distance {hit.distance}");
                }
                break;
            }
        }
        
        // Apply hover animation
        if (isHovered != wasHovered)
        {
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
            }
            hoverCoroutine = StartCoroutine(HoverAnimation(isHovered));
            Debug.Log($"DroppedItem: Started hover animation for {itemType}, hovering: {isHovered}");
        }
    }
    
    private void CheckBlockCollision()
    {
        // Only check occasionally to avoid constant teleporting
        if (Time.fixedTime % 0.5f > 0.1f) return; // Check every 0.5 seconds
        
        // Check if we're inside a solid block
        Vector3Int blockPos = Vector3Int.FloorToInt(transform.position);
        WorldGenerator worldGen = FindFirstObjectByType<WorldGenerator>();
        
        if (worldGen != null)
        {
            BlockType blockAtPos = worldGen.GetBlockType(blockPos);
            
            // If we're inside a solid block, teleport to safety
            if (blockAtPos != BlockType.Air && blockAtPos != BlockType.Water)
            {
                Debug.Log($"DroppedItem: {itemType} detected inside {blockAtPos} block, finding safe position");
                
                // Try to find a safe position nearby
                Vector3 safePosition = FindSafePosition(transform.position, worldGen);
                if (safePosition != Vector3.zero)
                {
                    Debug.Log($"DroppedItem: Teleporting {itemType} from {transform.position} to {safePosition}");
                    transform.position = safePosition;
                    
                    // Reset velocity to prevent weird physics
                    itemRigidbody.linearVelocity = Vector3.zero;
                    itemRigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    Debug.Log($"DroppedItem: No safe position found, teleporting {itemType} upward");
                    // Last resort: move up until we find air
                    for (int y = 1; y <= 10; y++)
                    {
                        Vector3Int testPos = blockPos + Vector3Int.up * y;
                        if (worldGen.GetBlockType(testPos) == BlockType.Air)
                        {
                            transform.position = new Vector3(transform.position.x, testPos.y + 0.5f, transform.position.z);
                            itemRigidbody.linearVelocity = Vector3.zero;
                            itemRigidbody.angularVelocity = Vector3.zero;
                            break;
                        }
                    }
                }
            }
        }
    }
    
    private Vector3 FindSafePosition(Vector3 currentPos, WorldGenerator worldGen)
    {
        Vector3Int centerBlock = Vector3Int.FloorToInt(currentPos);
        
        // Check positions in expanding radius
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    for (int y = -1; y <= 2; y++) // Check a bit below and above
                    {
                        Vector3Int testPos = centerBlock + new Vector3Int(x, y, z);
                        if (worldGen.GetBlockType(testPos) == BlockType.Air)
                        {
                            // Found an air block, return a position in the center of it
                            return new Vector3(testPos.x + 0.5f, testPos.y + 0.5f, testPos.z + 0.5f);
                        }
                    }
                }
            }
        }
        
        return Vector3.zero; // No safe position found
    }
    
    // Interaction handling moved to InteractionManager for better priority control
    
    public void SetHovered(bool hovered)
    {
        if (isHovered != hovered)
        {
            isHovered = hovered;
            
            Debug.Log($"DroppedItem: {itemType} hover state changed to {hovered}");
            
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
            }
            hoverCoroutine = StartCoroutine(HoverAnimation(isHovered));
        }
    }
    
    private IEnumerator HoverAnimation(bool hovering)
    {
        Vector3 targetScale = hovering ? baseScale * hoverScale : baseScale;
        Vector3 startScale = transform.localScale;
        
        Debug.Log($"DroppedItem: HoverAnimation {itemType} - hovering: {hovering}, from scale {startScale} to {targetScale}");
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }
        
        transform.localScale = targetScale;
        Debug.Log($"DroppedItem: HoverAnimation {itemType} complete - final scale: {targetScale}");
    }
    
    private void UpdateVisualEffects()
    {
        // Rotation
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        // Bobbing motion (only when not hovering to avoid conflicting animations)
        if (!isHovered && itemRigidbody.linearVelocity.magnitude < 0.1f) // Only bob when nearly stationary
        {
            Vector3 newPos = startPosition;
            newPos.y += Mathf.Sin(lifeTime * bobSpeed) * bobHeight;
            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 2f);
        }
        else
        {
            startPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        }
        
        // Blinking effect when close to despawn (last 30 seconds)
        if (lifeTime > despawnTime - 30f)
        {
            float blinkSpeed = Mathf.Lerp(1f, 10f, (lifeTime - (despawnTime - 30f)) / 30f);
            bool visible = Mathf.Sin(lifeTime * blinkSpeed) > 0;
            meshRenderer.enabled = visible;
        }
    }
    
    private void SetupVisualAppearance()
    {
        // Use existing block materials/textures
        BlockData blockData = BlockDatabase.GetBlockData(itemType);
        
        // Create a smaller version of the block (like Minecraft dropped items)
        transform.localScale = Vector3.one * 0.3f;
        
        // Apply block material if available
        if (blockData.blockMaterial != null)
        {
            meshRenderer.material = blockData.blockMaterial;
        }
        else
        {
            // Create simple colored material
            Material itemMaterial = new Material(Shader.Find("Standard"));
            itemMaterial.color = blockData.blockColor;
            meshRenderer.material = itemMaterial;
        }
    }
    
    public void TryPickup()
    {
        Debug.Log($"DroppedItem: TryPickup called for {itemType} x{quantity}");
        
        if (isBeingPickedUp)
        {
            Debug.Log("DroppedItem: Already being picked up, ignoring");
            return;
        }
        
        if (playerInventory == null)
        {
            Debug.Log("DroppedItem: No playerInventory found, trying to find one");
            
            // Try multiple ways to find the player and inventory
            GameObject player = null;
            
            // Method 1: Try Player tag
            player = GameObject.FindGameObjectWithTag("Player");
            Debug.Log($"DroppedItem: FindGameObjectWithTag('Player') result: {(player != null ? player.name : "null")}");
            
            // Method 2: Try FirstPersonController
            if (player == null)
            {
                FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
                player = fpc?.gameObject;
                Debug.Log($"DroppedItem: FindFirstObjectByType<FirstPersonController>() result: {(player != null ? player.name : "null")}");
            }
            
            // Method 3: Try PlayerController
            if (player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                player = pc?.gameObject;
                Debug.Log($"DroppedItem: FindFirstObjectByType<PlayerController>() result: {(player != null ? player.name : "null")}");
            }
            
            // Method 4: Direct search for PlayerInventory component
            if (player == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>();
                Debug.Log($"DroppedItem: FindFirstObjectByType<PlayerInventory>() result: {(playerInventory != null ? playerInventory.gameObject.name : "null")}");
            }
            else
            {
                // Try to get PlayerInventory from the player GameObject
                playerInventory = player.GetComponent<PlayerInventory>();
                Debug.Log($"DroppedItem: GetComponent<PlayerInventory>() on {player.name} result: {(playerInventory != null ? "found" : "null")}");
                
                // If not on player, try to find it anywhere
                if (playerInventory == null)
                {
                    playerInventory = FindFirstObjectByType<PlayerInventory>();
                    Debug.Log($"DroppedItem: Fallback FindFirstObjectByType<PlayerInventory>() result: {(playerInventory != null ? playerInventory.gameObject.name : "null")}");
                }
            }
            
            if (playerInventory == null)
            {
                Debug.Log("DroppedItem: Still no playerInventory found after all attempts!");
                return;
            }
            else
            {
                Debug.Log($"DroppedItem: Found playerInventory on GameObject: {playerInventory.gameObject.name}");
            }
        }
        
        Debug.Log($"DroppedItem: Attempting to add {itemType} x{quantity} to inventory");
        
        // Try to add to inventory
        bool wasPickedUp = playerInventory.AddItem(itemType, quantity);
        
        Debug.Log($"DroppedItem: AddItem returned {wasPickedUp}");
        
        if (wasPickedUp)
        {
            Debug.Log($"DroppedItem: Successfully picked up {itemType} x{quantity}");
            isBeingPickedUp = true;
            
            // Play pickup sound if available
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
            
            // Pickup animation
            Debug.Log($"DroppedItem: Starting pickup animation for {itemType}");
            StartCoroutine(PickupAnimation());
        }
        else
        {
            Debug.Log($"DroppedItem: Failed to pick up {itemType} x{quantity} - inventory might be full");
        }
    }
    
    private IEnumerator PickupAnimation()
    {
        Debug.Log($"DroppedItem: PickupAnimation started for {itemType}");
        
        // Move toward player and shrink
        float duration = 0.3f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position + Vector3.up * 1f : startPos + Vector3.up * 2f;
        Vector3 startScale = transform.localScale;
        
        Debug.Log($"DroppedItem: Animation from {startPos} to {targetPos}, scale {startScale} to zero");
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            yield return null;
        }
        
        Debug.Log($"DroppedItem: PickupAnimation complete, destroying {itemType}");
        
        // Destroy the item
        Destroy(gameObject);
    }
    
    private IEnumerator DespawnAfterTime()
    {
        yield return new WaitForSeconds(despawnTime);
        Destroy(gameObject);
    }
    
    public static GameObject CreateDroppedItem(Vector3 position, BlockType itemType, int quantity = 1)
    {
        // Create dropped item GameObject
        GameObject droppedItem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        droppedItem.name = $"Dropped_{itemType}";
        droppedItem.transform.position = position;
        
        // Add DroppedItem component
        DroppedItem dropComponent = droppedItem.AddComponent<DroppedItem>();
        dropComponent.itemType = itemType;
        dropComponent.quantity = quantity;
        
        // Remove default collider and let DroppedItem handle it
        Collider defaultCollider = droppedItem.GetComponent<Collider>();
        if (defaultCollider != null)
            DestroyImmediate(defaultCollider);
        
        return droppedItem;
    }
    
    // Right-click interaction system replaces automatic pickup
}

// Simple manager to handle right-click consumption
public static class DroppedItemManager
{
    public static bool consumedRightClick = false;
    
    public static void ResetRightClick()
    {
        consumedRightClick = false;
    }
}