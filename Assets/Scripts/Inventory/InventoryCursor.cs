using UnityEngine;

public static class InventoryCursor
{
    private static ItemStack cursorStack = new ItemStack();
    
    public static ItemStack GetCursorStack()
    {
        return cursorStack;
    }
    
    public static void SetCursorStack(ItemStack stack)
    {
        cursorStack = stack;
    }
    
    public static bool HasItem()
    {
        return !cursorStack.IsEmpty;
    }
    
    public static void Clear()
    {
        cursorStack = new ItemStack();
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
}