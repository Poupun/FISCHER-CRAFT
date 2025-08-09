using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 5f;
    
    [Header("Interaction")]
    public float interactionRange = 5f;
    public LayerMask blockLayerMask = -1;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private float xRotation = 0f;
    private PlayerInventory inventory;
    private WorldGenerator worldGenerator;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<PlayerInventory>();
    worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate movement direction
        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction = direction.normalized;
        
        // Apply movement
        Vector3 move = direction * moveSpeed;
        
        // Apply gravity
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Jump
        if (Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * -9.81f);
        }
        
        velocity.y += -9.81f * Time.deltaTime;
        move.y = velocity.y;
        
        characterController.Move(move * Time.deltaTime);
    }
    
    void HandleInteraction()
    {
        // Left click - break block
        if (Input.GetMouseButtonDown(0))
        {
            BreakBlock();
        }
        
        // Right click - place block
        if (Input.GetMouseButtonDown(1))
        {
            PlaceBlock();
        }
    }
    
    void BreakBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange, blockLayerMask))
        {
            BlockInfo blockInfo = hit.collider.GetComponent<BlockInfo>();
            if (blockInfo != null)
            {
                if (worldGenerator != null)
                {
                    worldGenerator.PlaceBlock(blockInfo.position, BlockType.Air);
                    
                    // Add block to inventory
                    if (inventory != null)
                    {
                        inventory.AddBlock(blockInfo.blockType);
                    }

                }
            }
        }
    }
    
    void PlaceBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange, blockLayerMask))
        {
            Vector3 hitPoint = hit.point + hit.normal * 0.5f;
            Vector3Int blockPosition = Vector3Int.RoundToInt(hitPoint);
            
            if (inventory != null && worldGenerator != null)
            {
                BlockType blockToPlace = inventory.GetCurrentBlock();
                if (blockToPlace != BlockType.Air && inventory.RemoveBlock(blockToPlace))
                {
                    worldGenerator.PlaceBlock(blockPosition, blockToPlace);
                }
            }
        }
    }
}