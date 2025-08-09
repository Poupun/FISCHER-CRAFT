using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public GameObject crosshairPrefab;
    public Canvas targetCanvas;
    
    private GameObject crosshairInstance;
    
    void Start()
    {
        CreateCrosshair();
    }
    
    void CreateCrosshair()
    {
        if (crosshairPrefab == null || targetCanvas == null)
        {
            // Create a simple crosshair programmatically
            CreateSimpleCrosshair();
            return;
        }
        
        crosshairInstance = Instantiate(crosshairPrefab, targetCanvas.transform);
        
        // Center the crosshair
        RectTransform rectTransform = crosshairInstance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
    
    void CreateSimpleCrosshair()
    {
        if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null) return;
        
        // Create crosshair gameobject
        crosshairInstance = new GameObject("Crosshair");
        crosshairInstance.transform.SetParent(targetCanvas.transform);
        
        // Add RectTransform
        RectTransform rectTransform = crosshairInstance.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(20, 20);
        
        // Add Image component
        Image image = crosshairInstance.AddComponent<Image>();
        image.color = Color.white;
        
        // Create a simple cross texture
        CreateCrossTexture(image);
    }
    
    void CreateCrossTexture(Image image)
    {
        // Create a simple white texture for the crosshair
        Texture2D texture = new Texture2D(20, 20);
        Color32[] pixels = new Color32[400];
        
        // Fill with transparent
        for (int i = 0; i < 400; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Draw vertical line
        for (int y = 8; y < 12; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                if (x == 9 || x == 10)
                {
                    pixels[y * 20 + x] = Color.white;
                }
            }
        }
        
        // Draw horizontal line  
        for (int y = 0; y < 20; y++)
        {
            for (int x = 8; x < 12; x++)
            {
                if (y == 9 || y == 10)
                {
                    pixels[y * 20 + x] = Color.white;
                }
            }
        }
        
        texture.SetPixels32(pixels);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 20, 20), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;
    }
}