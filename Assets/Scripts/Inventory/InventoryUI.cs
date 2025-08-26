using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory inventory;
    public Transform inventoryGrid;
    public Transform hotbarGrid;
    public GameObject slotPrefab;
    
    [Header("Settings")]
    public int inventoryRows = 3;
    public int inventoryColumns = 9;
    public Vector2 slotSize = new Vector2(48, 48);
    public Vector2 slotSpacing = new Vector2(4, 4);
    
    private InventorySlot[] inventorySlots;
    private InventorySlot[] hotbarSlots;
    
    void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();
            
        CreateInventorySlots();
        CreateHotbarSlots();
        
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshAllSlots;
    }
    
    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshAllSlots;
    }
    
    void CreateInventorySlots()
    {
        if (inventoryGrid == null || slotPrefab == null || inventory == null) return;
        
        int totalInventorySlots = inventory.mainInventorySize;
        inventorySlots = new InventorySlot[totalInventorySlots];
        
        var gridLayout = inventoryGrid.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = inventoryGrid.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        gridLayout.cellSize = slotSize;
        gridLayout.spacing = slotSpacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = inventoryColumns;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        
        for (int i = 0; i < totalInventorySlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, inventoryGrid);
            slotObj.name = $"MainInventorySlot_{i}";
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
                slot = slotObj.AddComponent<InventorySlot>();
                
            slot.SetSlotIndex(inventory.hotbarSize + i);
            inventorySlots[i] = slot;
            
            SetupSlotComponents(slot);
        }
    }
    
    void CreateHotbarSlots()
    {
        if (hotbarGrid == null || slotPrefab == null || inventory == null) return;
        
        hotbarSlots = new InventorySlot[inventory.hotbarSize];
        
        var gridLayout = hotbarGrid.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = hotbarGrid.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        gridLayout.cellSize = slotSize;
        gridLayout.spacing = slotSpacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayout.constraintCount = 1;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        
        for (int i = 0; i < inventory.hotbarSize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, hotbarGrid);
            slotObj.name = $"HotbarSlot_{i}";
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
                slot = slotObj.AddComponent<InventorySlot>();
                
            slot.SetSlotIndex(i);
            hotbarSlots[i] = slot;
            
            SetupSlotComponents(slot);
        }
    }
    
    void SetupSlotComponents(InventorySlot slot)
    {
        // Use the same detection logic as HotbarUI - direct name-based detection
        var images = slot.GetComponentsInChildren<Image>();
        
        foreach (var img in images)
        {
            if (img.name.ToLower().Contains("background") || img.name.ToLower().Contains("bg"))
                slot.background = img;
            else if (img.name.ToLower().Contains("icon"))
                slot.icon = img;
        }
        
        if (slot.countText == null)
        {
            slot.countText = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }
        
        // Set the same colors as HotbarUI
        slot.normalColor = Color.white;  // Match HotbarUI default
        slot.highlightColor = Color.yellow;  // Match HotbarUI default
        
        if (slot.background != null)
        {
            slot.background.color = slot.normalColor;
        }
    }
    
    void RefreshAllSlots()
    {
        if (inventorySlots != null)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot != null)
                    slot.SetSlotIndex(slot.slotIndex);
            }
        }
        
        if (hotbarSlots != null)
        {
            foreach (var slot in hotbarSlots)
            {
                if (slot != null)
                    slot.SetSlotIndex(slot.slotIndex);
            }
        }
    }
    
    public void OnInventoryClose()
    {
        var inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.CloseInventory();
        }
    }
}