using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class InventoryCursor
{
    private static ItemStack cursorStack = new ItemStack();
    private static GameObject cursorVisual;
    private static TextMeshProUGUI countText;
    
    public static ItemStack GetCursorStack()
    {
        return cursorStack;
    }
    
    public static void SetCursorStack(ItemStack stack)
    {
        cursorStack = stack;
        Debug.Log($"InventoryCursor.SetCursorStack: Set to {stack.blockType} x{stack.count}");
        
        // Create or update visual cursor
        UpdateCursorVisual();
    }
    
    public static bool HasItem()
    {
        bool hasItem = !cursorStack.IsEmpty;
        if (Time.frameCount % 120 == 0) // Every 2 seconds
        {
            Debug.Log($"InventoryCursor.HasItem: {hasItem} (stack: {cursorStack.blockType} x{cursorStack.count})");
        }
        return hasItem;
    }
    
    public static void Clear()
    {
        cursorStack = new ItemStack();
        UpdateCursorVisual();
    }
    
    public static ItemStack TakeItems(int count)
    {
        if (cursorStack.IsEmpty) return new ItemStack();
        
        int takeAmount = Mathf.Min(count, cursorStack.count);
        var result = new ItemStack(cursorStack.blockType, takeAmount);
        
        cursorStack.count -= takeAmount;
        if (cursorStack.count <= 0)
        {
            Clear();
        }
        
        return result;
    }
    
    public static bool CanStackWith(ItemStack other)
    {
        if (cursorStack.IsEmpty || other.IsEmpty) return true;
        return cursorStack.blockType == other.blockType;
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
        image.sprite = BlockManager.GetBlockSprite(cursorStack.blockType);
        image.color = new Color(1f, 1f, 1f, 0.8f);
        image.raycastTarget = false;
        
        var rectTransform = cursorVisual.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(32, 32);
        
        // Count text as child
        var countObj = new GameObject("Count");
        countObj.transform.SetParent(cursorVisual.transform, false);
        
        countText = countObj.AddComponent<TextMeshProUGUI>();
        countText.text = cursorStack.count > 1 ? cursorStack.count.ToString() : "";
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
        
        Debug.Log($"InventoryCursor: Created cursor visual for {cursorStack.blockType} x{cursorStack.count}");
    }
    
    static void RefreshCursorVisual()
    {
        if (cursorVisual == null) return;
        
        // Update the image sprite
        var image = cursorVisual.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = BlockManager.GetBlockSprite(cursorStack.blockType);
        }
        
        // Update the count text
        if (countText != null)
        {
            countText.text = cursorStack.count > 1 ? cursorStack.count.ToString() : "";
        }
        
        Debug.Log($"InventoryCursor: Refreshed cursor visual for {cursorStack.blockType} x{cursorStack.count}");
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