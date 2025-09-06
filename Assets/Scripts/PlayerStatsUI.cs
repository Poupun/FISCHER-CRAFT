using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Health Bar")]
    public Transform healthBarParent;
    public GameObject healthIconPrefab;
    
    [Header("Hunger Bar")]
    public Transform hungerBarParent;
    public GameObject hungerIconPrefab;
    
    [Header("Icons")]
    public Sprite healthFullSprite;
    public Sprite healthEmptySprite;
    public Sprite hungerFullSprite;
    public Sprite hungerEmptySprite;
    
    private List<Image> healthIcons = new List<Image>();
    private List<Image> hungerIcons = new List<Image>();
    
    void Start()
    {
        // Subscribe to player stats events
        PlayerStats.OnHealthChanged += UpdateHealthBar;
        PlayerStats.OnHungerChanged += UpdateHungerBar;
        
        // Initialize bars
        InitializeHealthBar();
        InitializeHungerBar();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        PlayerStats.OnHealthChanged -= UpdateHealthBar;
        PlayerStats.OnHungerChanged -= UpdateHungerBar;
    }
    
    private void InitializeHealthBar()
    {
        // Find PlayerStats to get max health
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;
        
        int maxHealth = playerStats.MaxHealth;
        int heartsToShow = Mathf.CeilToInt(maxHealth / 2f); // 2 health per heart (like Minecraft)
        
        // Clear existing icons
        foreach (Transform child in healthBarParent)
        {
            DestroyImmediate(child.gameObject);
        }
        healthIcons.Clear();
        
        // Create health icons
        for (int i = 0; i < heartsToShow; i++)
        {
            GameObject heartIcon = Instantiate(healthIconPrefab, healthBarParent);
            Image heartImage = heartIcon.GetComponent<Image>();
            if (heartImage != null)
            {
                healthIcons.Add(heartImage);
            }
        }
        
        UpdateHealthBar(playerStats.CurrentHealth, maxHealth);
    }
    
    private void InitializeHungerBar()
    {
        // Find PlayerStats to get max hunger
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;
        
        int maxHunger = playerStats.MaxHunger;
        int foodToShow = Mathf.CeilToInt(maxHunger / 2f); // 2 hunger per food icon
        
        // Clear existing icons
        foreach (Transform child in hungerBarParent)
        {
            DestroyImmediate(child.gameObject);
        }
        hungerIcons.Clear();
        
        // Create hunger icons
        for (int i = 0; i < foodToShow; i++)
        {
            GameObject foodIcon = Instantiate(hungerIconPrefab, hungerBarParent);
            Image foodImage = foodIcon.GetComponent<Image>();
            if (foodImage != null)
            {
                hungerIcons.Add(foodImage);
            }
        }
        
        UpdateHungerBar(playerStats.CurrentHunger, maxHunger);
    }
    
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthIcons.Count == 0) return;
        
        for (int i = 0; i < healthIcons.Count; i++)
        {
            int healthForThisHeart = currentHealth - (i * 2);
            
            if (healthForThisHeart >= 2)
            {
                // Full heart
                healthIcons[i].sprite = healthFullSprite;
                healthIcons[i].color = Color.white;
            }
            else if (healthForThisHeart == 1)
            {
                // Half heart (use full sprite but different color to indicate half)
                healthIcons[i].sprite = healthFullSprite;
                healthIcons[i].color = new Color(1f, 0.5f, 0.5f, 1f); // Slightly dimmed
            }
            else
            {
                // Empty heart
                healthIcons[i].sprite = healthEmptySprite;
                healthIcons[i].color = Color.white;
            }
        }
    }
    
    private void UpdateHungerBar(float currentHunger, int maxHunger)
    {
        if (hungerIcons.Count == 0) return;
        
        for (int i = 0; i < hungerIcons.Count; i++)
        {
            float hungerForThisIcon = currentHunger - (i * 2f);
            
            if (hungerForThisIcon >= 2f)
            {
                // Full hunger icon
                hungerIcons[i].sprite = hungerFullSprite;
                hungerIcons[i].color = Color.white;
            }
            else if (hungerForThisIcon > 0f)
            {
                // Partial hunger (use full sprite but different color)
                hungerIcons[i].sprite = hungerFullSprite;
                hungerIcons[i].color = new Color(1f, 0.8f, 0.5f, 1f); // Slightly dimmed
            }
            else
            {
                // Empty hunger icon
                hungerIcons[i].sprite = hungerEmptySprite;
                hungerIcons[i].color = Color.white;
            }
        }
    }
}