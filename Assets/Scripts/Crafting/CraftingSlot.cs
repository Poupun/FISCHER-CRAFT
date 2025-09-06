using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CraftingSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Components")]
    public UnityEngine.UI.Graphic background; // Can be Image or RawImage
    public Image icon;
    public TextMeshProUGUI countText;
    
    [Header("Settings")]
    public Color normalColor = new Color(0, 0, 0, 0.5f);
    public bool isResultSlot = false;
    public int gridX = 0;
    public int gridY = 0;
    
    private CraftingSystem craftingSystem;
    private TableCraftingSystem tableCraftingSystem;
    private ItemStack currentStack;
    private Canvas parentCanvas;
    private static CraftingSlot draggedSlot;
    private static MonoBehaviour draggedComponent; // Can hold either CraftingSlot or InventorySlot
    private static GameObject dragPreview;
    
    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Auto-setup UI components if they're not assigned
        SetupUIComponents();
        
        if (background != null)
            background.color = normalColor;
            
        RefreshSlot();
    }
    
    void SetupUIComponents()
    {
        // Auto-find UI components by name if not already assigned
        if (background == null || icon == null || countText == null)
        {
            // Check for both Image and RawImage components
            var images = GetComponentsInChildren<UnityEngine.UI.Image>();
            var rawImages = GetComponentsInChildren<UnityEngine.UI.RawImage>();
            
            // Check regular Images first
            foreach (var img in images)
            {
                if (background == null && (img.name.ToLower().Contains("background") || img.name.ToLower().Contains("bg")))
                    background = img;
                else if (icon == null && img.name.ToLower().Contains("icon"))
                    icon = img;
            }
            
            // If background is still null, check RawImages
            if (background == null)
            {
                foreach (var rawImg in rawImages)
                {
                    if (rawImg.name.ToLower().Contains("background") || rawImg.name.ToLower().Contains("bg"))
                    {
                        background = rawImg;
                        break;
                    }
                }
            }
            
            if (countText == null)
            {
                countText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
        }
    }
    
    public void Initialize(CraftingSystem system, int x, int y, bool resultSlot = false)
    {
        craftingSystem = system;
        tableCraftingSystem = null;
        gridX = x;
        gridY = y;
        isResultSlot = resultSlot;
        
        // Ensure UI components are set up
        SetupUIComponents();
        
        // Initialize with empty stack
        currentStack = new ItemStack();
        RefreshSlot();
    }
    
    public void Initialize(TableCraftingSystem system, int x, int y, bool resultSlot = false)
    {
        tableCraftingSystem = system;
        craftingSystem = null;
        gridX = x;
        gridY = y;
        isResultSlot = resultSlot;
        
        // Ensure UI components are set up
        SetupUIComponents();
        
        // Initialize with empty stack
        currentStack = new ItemStack();
        RefreshSlot();
    }
    
    public void SetStack(ItemStack stack)
    {
        currentStack = stack;
        RefreshSlot();
    }
    
    public ItemStack GetStack()
    {
        return currentStack;
    }
    
    void RefreshSlot()
    {
        // Try to setup components again if they're missing
        if (icon == null || countText == null || background == null)
        {
            SetupUIComponents();
        }
        
        if (icon != null)
        {
            if (currentStack.IsEmpty)
            {
                icon.enabled = false;
            }
            else
            {
                icon.enabled = true;
                icon.sprite = BlockManager.GetBlockSprite(currentStack.blockType);
                icon.color = Color.white;
                icon.preserveAspect = true;
            }
        }
        else
        {
            Debug.LogWarning($"CraftingSlot {gameObject.name}: Icon component not found!");
        }
        
        if (countText != null)
        {
            if (currentStack.IsEmpty)
            {
                countText.text = string.Empty;
            }
            else
            {
                countText.text = (currentStack.count > 1 ? currentStack.count.ToString() : string.Empty);
            }
        }
        else
        {
            Debug.LogWarning($"CraftingSlot {gameObject.name}: CountText component not found!");
        }
    }
    
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isResultSlot) return;
        if (currentStack.IsEmpty) return;
        
        draggedSlot = this;
        draggedComponent = this;
        CreateDragPreview();
    }
    
    public static void SetDraggedSlot(MonoBehaviour draggedItem)
    {
        draggedComponent = draggedItem;
        if (draggedItem is CraftingSlot craftSlot)
            draggedSlot = craftSlot;
        else
            draggedSlot = null;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview != null)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out position);
            dragPreview.transform.position = parentCanvas.transform.TransformPoint(position);
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragPreview();
        draggedSlot = null;
        draggedComponent = null;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        if (isResultSlot)
        {
            if (craftingSystem != null)
            {
                craftingSystem.TryCollectResult();
            }
            else if (tableCraftingSystem != null)
            {
                tableCraftingSystem.TryCollectResult();
            }
            return;
        }
        
        if (draggedComponent != null && draggedComponent != this)
        {
            // Handle dragging from InventorySlot to CraftingSlot
            if (draggedComponent is InventorySlot inventorySlot)
            {
                var unifiedInventory = FindFirstObjectByType<UnifiedPlayerInventory>();
                if (unifiedInventory != null)
                {
                    var draggedEntry = unifiedInventory.GetSlot(inventorySlot.slotIndex);
                    if (!draggedEntry.IsEmpty && draggedEntry.entryType == InventoryEntryType.Block)
                    {
                        SetStack(new ItemStack(draggedEntry.blockType, 1));
                        
                        // Remove one item from the inventory slot
                        var newEntry = draggedEntry;
                        newEntry.count--;
                        if (newEntry.count <= 0)
                        {
                            newEntry = InventoryEntry.Empty;
                        }
                        unifiedInventory.SetSlot(inventorySlot.slotIndex, newEntry);
                        
                        if (craftingSystem != null)
                        {
                            craftingSystem.OnCraftingGridChanged();
                        }
                        else if (tableCraftingSystem != null)
                        {
                            tableCraftingSystem.OnCraftingGridChanged();
                        }
                    }
                }
                else
                {
                    // Fallback to old PlayerInventory system
                    var inventory = FindFirstObjectByType<PlayerInventory>();
                    if (inventory != null)
                    {
                        var draggedStack = inventory.GetSlot(inventorySlot.slotIndex);
                        if (!draggedStack.IsEmpty)
                        {
                            SetStack(new ItemStack(draggedStack.blockType, 1));
                            inventory.RemoveItem(draggedStack.blockType, 1);
                            
                            if (craftingSystem != null)
                            {
                                craftingSystem.OnCraftingGridChanged();
                            }
                            else if (tableCraftingSystem != null)
                            {
                                tableCraftingSystem.OnCraftingGridChanged();
                            }
                        }
                    }
                }
            }
            // Handle dragging between CraftingSlots
            else if (draggedComponent is CraftingSlot draggedCraftSlot && !draggedCraftSlot.isResultSlot)
            {
                var temp = currentStack;
                SetStack(draggedCraftSlot.currentStack);
                draggedCraftSlot.SetStack(temp);
                
                if (craftingSystem != null)
                {
                    craftingSystem.OnCraftingGridChanged();
                }
                else if (tableCraftingSystem != null)
                {
                    tableCraftingSystem.OnCraftingGridChanged();
                }
            }
        }
    }
    
    void CreateDragPreview()
    {
        if (parentCanvas == null) return;
        
        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(parentCanvas.transform, false);
        
        var image = dragPreview.AddComponent<Image>();
        image.sprite = icon.sprite;
        image.color = new Color(1, 1, 1, 0.8f);
        image.raycastTarget = false;
        
        var rectTransform = dragPreview.GetComponent<RectTransform>();
        rectTransform.sizeDelta = icon.rectTransform.sizeDelta;
        
        dragPreview.transform.SetAsLastSibling();
    }
    
    void DestroyDragPreview()
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }
}