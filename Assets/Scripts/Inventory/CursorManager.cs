using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Update()
    {
        // Update cursor position every frame
        InventoryCursor.UpdateCursorPosition();
    }
}