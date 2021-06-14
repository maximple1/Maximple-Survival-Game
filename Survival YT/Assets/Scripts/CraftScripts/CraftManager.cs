using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using TMPro;

public class CraftManager : MonoBehaviour
{
    public bool isOpened;
    public GameObject craftingPanel;
    public GameObject inventoryPanel;


    public Transform craftItemsPanel;
    public GameObject craftItemButtonPrefab;

    public GameObject UIBG;
    public GameObject crosshair;
    public CinemachineVirtualCamera CVC;
    public Button craftBtn;
    public FillCraftItemDetails currentCraftItem;

    public KeyCode openCloseCraftButton;

    public List<CraftScriptableObject> allCrafts;

    [Header("Craft Item Details")]
    public TMP_Text craftItemName;
    public TMP_Text craftItemDescription;
    public Image craftItemImage;
    public TMP_Text craftItemDuration;
    public TMP_Text craftItemAmount;
    // Start is called before the first frame update
    void Start()
    {
        GameObject craftItemButton = Instantiate(craftItemButtonPrefab, craftItemsPanel);
        craftItemButton.GetComponent<Image>().sprite = allCrafts[0].finalCraft.icon;
        craftItemButton.GetComponent<FillCraftItemDetails>().currentCraftItem = allCrafts[0];
        craftItemButton.GetComponent<FillCraftItemDetails>().FillItemDetails();
        Destroy(craftItemButton);

        craftingPanel.gameObject.SetActive(false);
    }
    public void FillItemDetailsHelper()
    {
        currentCraftItem.FillItemDetails();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(openCloseCraftButton))
        {
            isOpened = !isOpened;

            GetComponent<InventoryManager>().isOpened = false;
            inventoryPanel.gameObject.SetActive(false);
            if (isOpened)
            {
                craftingPanel.SetActive(true);
                UIBG.SetActive(true);
                crosshair.SetActive(false);

                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_InputAxisName = "";
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_InputAxisName = "";
                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_InputAxisValue = 0;
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_InputAxisValue = 0;
                // Прекрепляем курсор к середине экрана
                Cursor.lockState = CursorLockMode.None;
                // и делаем его невидимым
                Cursor.visible = true;
            }
            else
            {
                craftingPanel.SetActive(false);
                UIBG.SetActive(false);
                crosshair.SetActive(true);

                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_InputAxisName = "Mouse X";
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_InputAxisName = "Mouse Y";
                // Прекрепляем курсор к середине экрана
                Cursor.lockState = CursorLockMode.Locked;
                // и делаем его невидимым
                Cursor.visible = false;
            }

        }

    }

    public void LoadCraftItems(string craftType)
    {
        for (int i = 0; i < craftItemsPanel.childCount; i++)
        {
            Destroy(craftItemsPanel.GetChild(i).gameObject);
        }
        foreach (CraftScriptableObject cso in allCrafts)
        {
            if (cso.craftType.ToString().ToLower() == craftType.ToLower())
            {
                GameObject craftItemButton = Instantiate(craftItemButtonPrefab, craftItemsPanel);
                craftItemButton.GetComponent<Image>().sprite = cso.finalCraft.icon;
                craftItemButton.GetComponent<FillCraftItemDetails>().currentCraftItem = cso;
            }
        }
    }
}
