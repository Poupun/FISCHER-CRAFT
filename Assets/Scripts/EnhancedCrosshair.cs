using UnityEngine;
using UnityEngine.UI;

public class EnhancedCrosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Color crosshairColor = Color.white;
    public float crosshairSize = 20f;
    public float crosshairThickness = 2f;
    public bool dynamicCrosshair = true;
    
    [Header("Dynamic Settings")]
    public float maxSpread = 15f;
    public float spreadSpeed = 5f;
    
    private RectTransform[] crosshairLines;
    private FirstPersonController playerController;
    private float currentSpread = 0f;
    private Canvas canvas;
    
    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found! Crosshair needs a Canvas to display.");
            return;
        }
        
        playerController = FindObjectOfType<FirstPersonController>();
        CreateCrosshair();
    }
    
    void Update()
    {
        if (dynamicCrosshair && crosshairLines != null && playerController != null)
        {
            UpdateDynamicCrosshair();
        }
    }
    
    void CreateCrosshair()
    {
        // Create parent object for crosshair
        GameObject crosshairParent = new GameObject("Crosshair");
        crosshairParent.transform.SetParent(canvas.transform);
        
        RectTransform parentRect = crosshairParent.AddComponent<RectTransform>();
        parentRect.anchorMin = new Vector2(0.5f, 0.5f);
        parentRect.anchorMax = new Vector2(0.5f, 0.5f);
        parentRect.anchoredPosition = Vector2.zero;
        parentRect.sizeDelta = new Vector2(crosshairSize * 2, crosshairSize * 2);
        
        // Create 4 lines for crosshair
        crosshairLines = new RectTransform[4];
        
        // Top line
        crosshairLines[0] = CreateCrosshairLine(crosshairParent, "Top", 
            new Vector2(0, crosshairSize/2), new Vector2(crosshairThickness, crosshairSize/2));
        
        // Bottom line
        crosshairLines[1] = CreateCrosshairLine(crosshairParent, "Bottom", 
            new Vector2(0, -crosshairSize/2), new Vector2(crosshairThickness, crosshairSize/2));
        
        // Left line
        crosshairLines[2] = CreateCrosshairLine(crosshairParent, "Left", 
            new Vector2(-crosshairSize/2, 0), new Vector2(crosshairSize/2, crosshairThickness));
        
        // Right line
        crosshairLines[3] = CreateCrosshairLine(crosshairParent, "Right", 
            new Vector2(crosshairSize/2, 0), new Vector2(crosshairSize/2, crosshairThickness));
    }
    
    RectTransform CreateCrosshairLine(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent.transform);
        
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image image = line.AddComponent<Image>();
        image.color = crosshairColor;
        
        return rect;
    }
    
    void UpdateDynamicCrosshair()
    {
        float targetSpread = 0f;
        
        // Calculate spread based on player state
        if (playerController.IsMoving)
        {
            targetSpread += maxSpread * 0.3f;
        }
        
        if (playerController.IsRunning)
        {
            targetSpread += maxSpread * 0.5f;
        }
        
        if (!playerController.IsGrounded)
        {
            targetSpread += maxSpread * 0.7f;
        }
        
        // Smooth the spread change
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * spreadSpeed);
        
        // Apply spread to crosshair lines
        if (crosshairLines != null)
        {
            // Top
            crosshairLines[0].anchoredPosition = new Vector2(0, (crosshairSize/2) + currentSpread);
            
            // Bottom
            crosshairLines[1].anchoredPosition = new Vector2(0, -(crosshairSize/2) - currentSpread);
            
            // Left
            crosshairLines[2].anchoredPosition = new Vector2(-(crosshairSize/2) - currentSpread, 0);
            
            // Right
            crosshairLines[3].anchoredPosition = new Vector2((crosshairSize/2) + currentSpread, 0);
        }
    }
    
    public void SetCrosshairColor(Color color)
    {
        crosshairColor = color;
        if (crosshairLines != null)
        {
            foreach (var line in crosshairLines)
            {
                if (line != null)
                {
                    Image img = line.GetComponent<Image>();
                    if (img != null) img.color = color;
                }
            }
        }
    }
    
    public void SetCrosshairVisibility(bool visible)
    {
        if (crosshairLines != null)
        {
            foreach (var line in crosshairLines)
            {
                if (line != null) line.gameObject.SetActive(visible);
            }
        }
    }
}