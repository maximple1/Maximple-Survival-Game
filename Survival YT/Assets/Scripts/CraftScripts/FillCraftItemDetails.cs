using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillCraftItemDetails : MonoBehaviour
{
    public CraftScriptableObject currentCraftItem;
    private CraftManager craftManager;
    public GameObject craftResourcePrefab;
    public string craftInfoPanelName;
    private GameObject craftInfoPanelGO;
    public CraftQueueManager craftQueueManager;

    private void Awake()
    {
        craftInfoPanelGO = GameObject.Find(craftInfoPanelName);
        craftManager = FindObjectOfType<CraftManager>();
        craftQueueManager = FindObjectOfType<CraftQueueManager>();
    }
    public void FillItemDetails()
    {
        craftManager.currentCraftItem = this;
        //for (int i = 0; i < GameObject.Find(craftInfoPanelName).transform.childCount; i++)
        for(int i = 0; i < craftInfoPanelGO.transform.childCount; i++)
        {
            Destroy(craftInfoPanelGO.transform.GetChild(i).gameObject);
        }

        craftManager.craftItemName.text = currentCraftItem.finalCraft.name;
        craftManager.craftItemDescription.text = currentCraftItem.finalCraft.itemDescription;
        craftManager.craftItemImage.sprite = currentCraftItem.finalCraft.icon;
        craftManager.craftItemDuration.text = currentCraftItem.craftTime.ToString();
        craftManager.craftItemAmount.text = currentCraftItem.craftAmount.ToString();

        bool canCraft = true;
        for (int i = 0; i < currentCraftItem.craftResources.Count; i++)
        {
            GameObject craftResourceGO = Instantiate(craftResourcePrefab, craftInfoPanelGO.transform);
            CraftResourceDetails crd = craftResourceGO.GetComponent<CraftResourceDetails>();
            crd.amountText.text = currentCraftItem.craftResources[i].craftObjectAmount.ToString();
            crd.itemTypeText.text = currentCraftItem.craftResources[i].craftObject.itemName;
            int totalAmount = currentCraftItem.craftResources[i].craftObjectAmount * int.Parse(craftQueueManager.craftAmountInputField.text);
            crd.totalText.text = totalAmount.ToString();
            int resourceAmount = 0;
            foreach(InventorySlot slot in FindObjectsOfType<InventoryManager>()[0].slots)
            {
                if (slot.isEmpty)
                    continue;
                if(slot.item.itemName == currentCraftItem.craftResources[i].craftObject.itemName)
                {
                    resourceAmount += slot.amount;
                }
            }
            crd.haveText.text = resourceAmount.ToString();

            if(resourceAmount < totalAmount)
            {
                canCraft = false;
            }
        }
        if (canCraft)
        {
            craftManager.craftBtn.interactable = true;
        }
        else
        {
            craftManager.craftBtn.interactable = false;
        }
        craftQueueManager.currentCraftItem = currentCraftItem;
    }
}
