using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image background;
    public Image icon;
    public TextMeshProUGUI countText;
    
    [Header("Settings")]
    public Color normalColor = new Color(0, 0, 0, 0.5f);
    public Color highlightColor = Color.yellow;
    public int slotIndex;
    
    private PlayerInventory inventory;
    private ItemStack currentStack;
    private Canvas parentCanvas;
    private static InventorySlot draggedSlot;
    private static GameObject dragPreview;
    
    void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();
            
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
            
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshSlot;
            
        RefreshSlot();
    }
    
    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshSlot;
    }
    
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        RefreshSlot();
    }
    
    void RefreshSlot()
    {
        if (inventory == null) return;
        
        currentStack = inventory.GetSlot(slotIndex);
        
        // For hotbar slots, let HotbarUI handle the visual updates
        // For other slots, handle the updates here using the same logic as HotbarUI
        if (!inventory.IsHotbarSlot(slotIndex))
        {
            // Handle Icon component only - assign block sprites (same as HotbarUI)
            if (icon != null)
            {
                if (currentStack.IsEmpty)
                {
                    icon.enabled = false;
                }
                else
                {
                    icon.enabled = true;
                    icon.sprite = GetSpriteForBlock(currentStack.blockType);
                    icon.color = Color.white;
                    icon.preserveAspect = true;  // Match HotbarUI setting
                }
            }
            
            // Handle count text (same logic as HotbarUI)
            if (countText != null)
            {
                if (currentStack.IsEmpty)
                {
                    countText.text = string.Empty;  // Match HotbarUI
                }
                else
                {
                    countText.text = (currentStack.count > 1 ? currentStack.count.ToString() : string.Empty);  // Match HotbarUI logic
                }
            }
            
            // Background stays with normalColor (no selection highlighting for inventory slots)
            if (background != null)
            {
                background.color = normalColor;
            }
        }
    }
    
    Sprite GetSpriteForBlock(BlockType type)
    {
        if (type == BlockType.Air) return null;
        
        Texture2D tex = null;
        if ((int)type < BlockDatabase.blockTypes.Length)
        {
            tex = BlockDatabase.blockTypes[(int)type].blockTexture;
        }
        
        if (tex == null)
        {
            var worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
            if (worldGenerator != null)
            {
                switch (type)
                {
                    case BlockType.Grass: tex = worldGenerator.grassTexture; break;
                    case BlockType.Dirt: tex = worldGenerator.dirtTexture; break;
                    case BlockType.Stone: tex = worldGenerator.stoneTexture; break;
                    case BlockType.Sand: tex = worldGenerator.sandTexture; break;
                    case BlockType.Coal: tex = worldGenerator.coalTexture; break;
                    case BlockType.Log: tex = worldGenerator.logTexture; break;
                    case BlockType.Leaves: tex = worldGenerator.leavesTexture; break;
                }
            }
        }
        
        if (tex == null) return null;
        
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventory == null) return;
        
        if (inventory.IsHotbarSlot(slotIndex))
        {
            inventory.SetSelectedIndex(slotIndex);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentStack.IsEmpty) return;
        
        draggedSlot = this;
        CreateDragPreview();
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
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot != null && draggedSlot != this && inventory != null)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    inventory.TryMergeSlots(draggedSlot.slotIndex, this.slotIndex);
                }
                else
                {
                    inventory.SwapSlots(draggedSlot.slotIndex, this.slotIndex);
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