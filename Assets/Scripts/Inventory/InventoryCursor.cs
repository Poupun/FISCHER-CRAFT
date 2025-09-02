using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class InventoryCursor
{
    private static InventoryEntry cursorEntry = InventoryEntry.Empty;
    private static GameObject cursorVisual;
    private static TextMeshProUGUI countText;
    
    // New unified methods
    public static InventoryEntry GetCursorEntry()
    {
        return cursorEntry;
    }
    
    public static void SetCursorEntry(InventoryEntry entry)
    {
        cursorEntry = entry;
        string entryDesc = entry.entryType == InventoryEntryType.Block ? entry.blockType.ToString() : entry.itemType.ToString();
        Debug.Log($"InventoryCursor.SetCursorEntry: Set to {entryDesc} x{entry.count}");
        
        // Create or update visual cursor
        UpdateCursorVisual();
    }
    
    // Legacy compatibility method
    public static ItemStack GetCursorStack()
    {
        if (cursorEntry.entryType == InventoryEntryType.Block)
            return new ItemStack(cursorEntry.blockType, cursorEntry.count);
        else
            return new ItemStack(BlockType.Air, 0); // Items don't convert to ItemStack
    }
    
    public static void SetCursorStack(ItemStack stack)
    {
        if (stack.IsEmpty)
        {
            cursorEntry = InventoryEntry.Empty;
        }
        else
        {
            cursorEntry = new InventoryEntry
            {
                entryType = InventoryEntryType.Block,
                blockType = stack.blockType,
                itemType = ItemType.Stick, // Default, not used for blocks
                count = stack.count
            };
        }
        Debug.Log($"InventoryCursor.SetCursorStack: Set to {stack.blockType} x{stack.count}");
        UpdateCursorVisual();
    }
    
    public static bool HasItem()
    {
        bool hasItem = !cursorEntry.IsEmpty;
        if (Time.frameCount % 120 == 0) // Every 2 seconds
        {
            string entryDesc = cursorEntry.entryType == InventoryEntryType.Block ? cursorEntry.blockType.ToString() : cursorEntry.itemType.ToString();
            Debug.Log($"InventoryCursor.HasItem: {hasItem} (entry: {entryDesc} x{cursorEntry.count})");
        }
        return hasItem;
    }
    
    public static void Clear()
    {
        cursorEntry = InventoryEntry.Empty;
        UpdateCursorVisual();
    }
    
    public static InventoryEntry TakeEntry(int count)
    {
        if (cursorEntry.IsEmpty) return InventoryEntry.Empty;
        
        int takeAmount = Mathf.Min(count, cursorEntry.count);
        var result = new InventoryEntry
        {
            entryType = cursorEntry.entryType,
            blockType = cursorEntry.blockType,
            itemType = cursorEntry.itemType,
            count = takeAmount
        };
        
        cursorEntry.count -= takeAmount;
        if (cursorEntry.count <= 0)
        {
            Clear();
        }
        else
        {
            UpdateCursorVisual();
        }
        
        return result;
    }
    
    // Legacy compatibility method
    public static ItemStack TakeItems(int count)
    {
        var entry = TakeEntry(count);
        if (entry.entryType == InventoryEntryType.Block)
            return new ItemStack(entry.blockType, entry.count);
        else
            return new ItemStack(BlockType.Air, 0); // Items don't convert to ItemStack
    }
    
    public static bool CanStackWith(InventoryEntry other)
    {
        if (cursorEntry.IsEmpty || other.IsEmpty) return true;
        return cursorEntry.CanStackWith(other);
    }
    
    // Legacy compatibility method
    public static bool CanStackWith(ItemStack other)
    {
        if (cursorEntry.IsEmpty || other.IsEmpty) return true;
        if (cursorEntry.entryType == InventoryEntryType.Block)
            return cursorEntry.blockType == other.blockType;
        else
            return false; // Items can't stack with ItemStack
    }
    
    public static void AddEntry(InventoryEntry entry)
    {
        if (entry.IsEmpty) return;
        
        if (cursorEntry.IsEmpty)
        {
            SetCursorEntry(entry);
            return;
        }
        
        if (cursorEntry.CanStackWith(entry))
        {
            int newCount = cursorEntry.count + entry.count;
            int maxStack = cursorEntry.entryType == InventoryEntryType.Block ? 
                           ItemStack.MaxStackSize : 
                           ItemManager.GetMaxStackSize(cursorEntry.itemType);
            cursorEntry.count = Mathf.Min(newCount, maxStack);
            UpdateCursorVisual();
        }
    }
    
    static void UpdateCursorVisual()
    {
        if (HasItem())
        {
            if (cursorVisual == null)
            {
                CreateCursorVisual();
            }
            else
            {
                RefreshCursorVisual();
            }
        }
        else
        {
            DestroyCursorVisual();
        }
    }
    
    static void CreateCursorVisual()
    {
        if (cursorVisual != null) return;
        
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) 
        {
            Debug.LogWarning("InventoryCursor: No Canvas found for cursor visual");
            return;
        }
        
        // Create cursor visual with icon and count
        cursorVisual = new GameObject("AutoCursorVisual");
        cursorVisual.transform.SetParent(canvas.transform, false);
        
        // Main icon
        var image = cursorVisual.AddComponent<Image>();
        image.sprite = cursorEntry.GetSprite(); // Use unified sprite system
        image.color = new Color(1f, 1f, 1f, 0.8f);
        image.raycastTarget = false;
        
        var rectTransform = cursorVisual.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(32, 32);
        
        // Count text as child
        var countObj = new GameObject("Count");
        countObj.transform.SetParent(cursorVisual.transform, false);
        
        countText = countObj.AddComponent<TextMeshProUGUI>();
        countText.text = cursorEntry.count > 1 ? cursorEntry.count.ToString() : "";
        countText.fontSize = 10;
        countText.color = Color.white;
        countText.fontStyle = FontStyles.Bold;
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.raycastTarget = false;
        
        var countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;
        
        // Set to top of hierarchy
        cursorVisual.transform.SetAsLastSibling();
        
        string entryDesc = cursorEntry.entryType == InventoryEntryType.Block ? cursorEntry.blockType.ToString() : cursorEntry.itemType.ToString();
        Debug.Log($"InventoryCursor: Created cursor visual for {entryDesc} x{cursorEntry.count}");
    }
    
    static void RefreshCursorVisual()
    {
        if (cursorVisual == null) return;
        
        // Update the image sprite
        var image = cursorVisual.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = cursorEntry.GetSprite(); // Use unified sprite system
        }
        
        // Update the count text
        if (countText != null)
        {
            countText.text = cursorEntry.count > 1 ? cursorEntry.count.ToString() : "";
        }
        
        string entryDesc = cursorEntry.entryType == InventoryEntryType.Block ? cursorEntry.blockType.ToString() : cursorEntry.itemType.ToString();
        Debug.Log($"InventoryCursor: Refreshed cursor visual for {entryDesc} x{cursorEntry.count}");
    }
    
    static void DestroyCursorVisual()
    {
        if (cursorVisual != null)
        {
            Object.Destroy(cursorVisual);
            cursorVisual = null;
            countText = null;
            Debug.Log("InventoryCursor: Destroyed cursor visual");
        }
    }
    
    public static void UpdateCursorPosition()
    {
        if (cursorVisual != null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Vector2 mousePosition = Input.mousePosition;
                Vector2 localPosition;
                UnityEngine.RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    mousePosition,
                    canvas.worldCamera,
                    out localPosition);
                cursorVisual.transform.localPosition = localPosition;
            }
        }
    }
    
}