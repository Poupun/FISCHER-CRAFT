using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public Transform slotsParent;
    
    private GameObject[] slotObjects;
    private Image[] slotImages;
    private TextMeshProUGUI[] slotTexts;
    
    void Start()
    {
        CreateInventorySlots();
    }
    
    void CreateInventorySlots()
    {
        if (slotPrefab == null || slotsParent == null) return;
        
        slotObjects = new GameObject[5];
        slotImages = new Image[5];
        slotTexts = new TextMeshProUGUI[5];
        
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotsParent);
            slotObjects[i] = slot;
            
            // Get components
            slotImages[i] = slot.GetComponent<Image>();
            slotTexts[i] = slot.GetComponentInChildren<TextMeshProUGUI>();
            
            // Set initial state
            if (slotImages[i] != null)
            {
                slotImages[i].color = Color.gray;
            }
        }
    }
    
    public void UpdateInventoryDisplay(InventorySlot[] inventory, int currentSlot)
    {
        if (slotImages == null || slotTexts == null) return;
        
        for (int i = 0; i < inventory.Length && i < slotImages.Length; i++)
        {
            // Update slot appearance
            if (inventory[i].blockType != BlockType.Air)
            {
                BlockData blockData = BlockDatabase.GetBlockData(inventory[i].blockType);
                slotImages[i].color = blockData.blockColor;
                
                if (slotTexts[i] != null)
                {
                    slotTexts[i].text = inventory[i].count.ToString();
                    slotTexts[i].gameObject.SetActive(true);
                }
            }
            else
            {
                slotImages[i].color = Color.gray;
                if (slotTexts[i] != null)
                {
                    slotTexts[i].gameObject.SetActive(false);
                }
            }
            
            // Highlight current slot
            if (i == currentSlot)
            {
                slotImages[i].transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                slotImages[i].transform.localScale = Vector3.one;
            }
        }
    }
}