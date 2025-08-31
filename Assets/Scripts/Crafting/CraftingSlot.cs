using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CraftingSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Components")]
    public Image background;
    public Image icon;
    public TextMeshProUGUI countText;
    
    [Header("Settings")]
    public Color normalColor = new Color(0, 0, 0, 0.5f);
    public bool isResultSlot = false;
    public int gridX = 0;
    public int gridY = 0;
    
    private CraftingSystem craftingSystem;
    private ItemStack currentStack;
    private Canvas parentCanvas;
    private static CraftingSlot draggedSlot;
    private static GameObject dragPreview;
    
    void Start()
    {
        craftingSystem = FindFirstObjectByType<CraftingSystem>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        if (background != null)
            background.color = normalColor;
            
        RefreshSlot();
    }
    
    public void Initialize(CraftingSystem system, int x, int y, bool resultSlot = false)
    {
        craftingSystem = system;
        gridX = x;
        gridY = y;
        isResultSlot = resultSlot;
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
                icon.preserveAspect = true;
            }
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
                    case BlockType.WoodPlanks: tex = worldGenerator.logTexture; break;
                }
            }
        }
        
        if (tex == null) return null;
        
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isResultSlot) return;
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
        if (isResultSlot)
        {
            if (craftingSystem != null)
            {
                craftingSystem.TryCollectResult();
            }
            return;
        }
        
        if (draggedSlot != null && draggedSlot != this)
        {
            var inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                var inventorySlot = draggedSlot.GetComponent<InventorySlot>();
                if (inventorySlot != null)
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
                    }
                }
                else if (draggedSlot is CraftingSlot draggedCraftSlot && !draggedCraftSlot.isResultSlot)
                {
                    var temp = currentStack;
                    SetStack(draggedCraftSlot.currentStack);
                    draggedCraftSlot.SetStack(temp);
                    
                    if (craftingSystem != null)
                    {
                        craftingSystem.OnCraftingGridChanged();
                    }
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