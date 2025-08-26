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

    // Target highlight
    private GameObject highlightCube; private Vector3Int currentTargetCell = new Vector3Int(int.MinValue,int.MinValue,int.MinValue); private BlockType currentTargetType = BlockType.Air;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        playerInventory = GetComponent<PlayerInventory>() ?? gameObject.AddComponent<PlayerInventory>();
        baseFOV = playerCamera.fieldOfView; targetHeight = standHeight; currentSpeed = walkSpeed; Cursor.lockState = CursorLockMode.Locked; SetupHighlight();
    }

    void Update()
    {
        HandleMouseLook(); HandleMovementState(); HandleMovement(); HandleInteraction(); UpdateCrouchHeight(); UpdateSprintFOV(); UpdateTargetHighlight();
    }

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
    { if (Input.GetMouseButtonDown(0)) BreakBlock(); if (Input.GetMouseButtonDown(1)) PlaceBlock(); }

    void BreakBlock()
    {
        // Remove plant first if cursor hits a plant cell
        if (TryRemovePlant()) return;
        if (AcquireTargetCell(out var cell,out var type))
    { BlockType drop = (type == BlockType.Grass && grassDropsDirt) ? BlockType.Dirt : type; worldGenerator.PlaceBlock(cell, BlockType.Air); playerInventory?.AddBlock(drop,1); currentTargetType = BlockType.Air; return; }
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
        if (playerInventory == null) return; if (!playerInventory.HasBlockForPlacement(out BlockType placeType)) return; Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2,Screen.height/2,0));
        if (worldGenerator != null && worldGenerator.useChunkStreaming && worldGenerator.useChunkMeshing)
        { Vector3Int hitCell, placeCell; Vector3 hitNormal; if (worldGenerator.TryVoxelRaycast(ray, interactionRange, out hitCell, out placeCell, out hitNormal)) { Vector3Int pos = placeCell; if (characterController){ Bounds bb = new Bounds((Vector3)pos, Vector3.one); if (bb.Intersects(characterController.bounds)) return; } if (worldGenerator.GetBlockType(pos) == BlockType.Air){ worldGenerator.PlaceBlock(pos, placeType); playerInventory.ConsumeOneFromSelected(); } return; } }
        // Physics fallback (plants / colliders)
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, blockLayerMask))
        { Vector3Int pos; BlockInfo bi = hit.collider.GetComponent<BlockInfo>(); if (bi != null){ Vector3 hp = hit.point + hit.normal*0.5f; pos = Vector3Int.RoundToInt(hp); } else { pos = Vector3Int.RoundToInt(hit.collider.bounds.center); } if (characterController){ Bounds bb = new Bounds((Vector3)pos,Vector3.one); if (bb.Intersects(characterController.bounds)) return; } if (worldGenerator != null && worldGenerator.GetBlockType(pos) == BlockType.Air){ worldGenerator.PlaceBlock(pos, placeType); playerInventory.ConsumeOneFromSelected(); } }
    }

    void SetupHighlight()
    {
        highlightCube = GameObject.CreatePrimitive(PrimitiveType.Cube); highlightCube.name = "BlockHighlight"; var col = highlightCube.GetComponent<Collider>(); if (col) Destroy(col); var mr = highlightCube.GetComponent<MeshRenderer>(); Shader sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh==null) sh = Shader.Find("Unlit/Color"); var mat = new Material(sh){color=new Color(0f,1f,0f,0.25f)}; mat.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); mat.SetInt("_ZWrite",0); mr.material = mat; highlightCube.transform.localScale = Vector3.one * 1.001f; highlightCube.SetActive(false);
    }

    void UpdateTargetHighlight()
    {
        if (worldGenerator == null || highlightCube == null) return; if (AcquireTargetCell(out var cell, out var type)){ if (cell != currentTargetCell || type != currentTargetType){ currentTargetCell = cell; currentTargetType = type; highlightCube.transform.position = cell + Vector3.one*0.5f; } if (!highlightCube.activeSelf) highlightCube.SetActive(true); } else { if (highlightCube.activeSelf) highlightCube.SetActive(false); currentTargetCell = new Vector3Int(int.MinValue,int.MinValue,int.MinValue); currentTargetType = BlockType.Air; }
    }

    // Public getters
    public bool IsSprinting() => isSprinting; public bool IsCrouching() => isCrouching; public float GetCurrentSpeed() => currentSpeed;
}