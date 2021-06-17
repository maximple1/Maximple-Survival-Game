using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftQueueManager : MonoBehaviour
{
    public CraftScriptableObject currentCraftItem;
    public InventoryManager inventoryManager;
    public GameObject craftQueuePrefab;
    public TMP_InputField craftAmountInputField;
    //public Button addButton;
    //public Button removeButton;
    public int craftTime;
    private CraftManager craftManager;

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        craftManager = FindObjectOfType<CraftManager>();
    }
    public void RemoveButtonFunction()
    {
        if (int.Parse(craftAmountInputField.text) <= 1)
            return;
        int newAmount = int.Parse(craftAmountInputField.text) - 1;
        craftAmountInputField.text = newAmount.ToString();
    }
    public void AddButtonFunction()
    {
        if (int.Parse(craftAmountInputField.text) >= 999)
            return;
        int newAmount = int.Parse(craftAmountInputField.text) + 1;
        craftAmountInputField.text = newAmount.ToString();
    }

    public void AddToCraftQueue()
    {
        foreach (CraftResource craftResource in currentCraftItem.craftResources)
        {
            int amountToRemove = craftResource.craftObjectAmount * int.Parse(craftAmountInputField.text);
            foreach (InventorySlot slot in inventoryManager.slots)
            {
                if (amountToRemove <= 0)
                    continue;
                if(slot.item == craftResource.craftObject)
                {
                    if(amountToRemove > slot.amount)
                    {
                        amountToRemove -= slot.amount;
                        slot.GetComponentInChildren<DragAndDropItem>().NullifySlotData();
                    }
                    else
                    {
                        slot.amount -= amountToRemove;
                        amountToRemove = 0;
                        if(slot.amount <= 0)
                        {
                            slot.GetComponentInChildren<DragAndDropItem>().NullifySlotData();
                        }
                        else
                        {
                            slot.itemAmountText.text = slot.amount.ToString();
                        }
                    }
                }
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            if(transform.GetChild(i).GetComponent<CraftQueueItemDetails>().currentCraftItem == currentCraftItem)
            {
                transform.GetChild(i).GetComponent<CraftQueueItemDetails>().craftAmount += int.Parse(craftAmountInputField.text);
                transform.GetChild(i).GetComponent<CraftQueueItemDetails>().amountText.text = "X" + transform.GetChild(i).GetComponent<CraftQueueItemDetails>().craftAmount.ToString();
                craftManager.currentCraftItem.FillItemDetails();
                return;
            }
        }

        GameObject craftQueueInstance = Instantiate(craftQueuePrefab, transform);
        CraftQueueItemDetails craftQueueItemDetails = craftQueueInstance.GetComponent<CraftQueueItemDetails>();
        craftQueueItemDetails.itemImage.sprite = currentCraftItem.finalCraft.icon;
        craftQueueItemDetails.amountText.text = craftAmountInputField.text;
        craftQueueItemDetails.craftAmount = int.Parse(craftAmountInputField.text);
        craftTime = currentCraftItem.craftTime;
        int minutes = Mathf.FloorToInt(craftTime / 60);
        int seconds = craftTime - minutes * 60;
        craftQueueItemDetails.timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        craftQueueItemDetails.craftTime = craftTime;
        craftQueueItemDetails.currentCraftItem = currentCraftItem;

        
        craftManager.currentCraftItem.FillItemDetails();
    }
}
