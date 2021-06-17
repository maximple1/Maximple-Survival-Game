using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public GameObject UIBG; // renamed
    public GameObject crosshair;
    public Transform inventoryPanel;
    public GameObject craftPanel;
    public Transform quickslotPanel;
    public List<InventorySlot> slots = new List<InventorySlot>();
    public bool isOpened;
    public float reachDistance = 3f;
    private Camera mainCamera;
    public CinemachineVirtualCamera CVC;
    private CraftManager craftManager;
    [SerializeField] private Transform player;
    // Start is called before the first frame update
    private void Awake()
    {
        UIBG.SetActive(true);
    }
    void Start()
    {
        mainCamera = Camera.main;
        craftManager = FindObjectOfType<CraftManager>();
        for (int i = 0; i < inventoryPanel.childCount; i++)
        {
            if(inventoryPanel.GetChild(i).GetComponent<InventorySlot>() != null)
            {
                slots.Add(inventoryPanel.GetChild(i).GetComponent<InventorySlot>());
            }
        }
        for (int i = 0; i < quickslotPanel.childCount; i++)
        {
            if (quickslotPanel.GetChild(i).GetComponent<InventorySlot>() != null)
            {
                slots.Add(quickslotPanel.GetChild(i).GetComponent<InventorySlot>());
            }
        }

        UIBG.SetActive(false);
        inventoryPanel.gameObject.SetActive(false);//new line
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isOpened = !isOpened;
            craftPanel.gameObject.SetActive(false);
            craftManager.isOpened = false;
            if (isOpened)
            {
                UIBG.SetActive(true);
                inventoryPanel.gameObject.SetActive(true); // new line
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
                UIBG.SetActive(false);
                inventoryPanel.gameObject.SetActive(false); // new line
                crosshair.SetActive(true);
                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_InputAxisName = "Mouse X";
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_InputAxisName = "Mouse Y";
                // Прекрепляем курсор к середине экрана
                Cursor.lockState = CursorLockMode.Locked;
                // и делаем его невидимым
                Cursor.visible = false;

                DragAndDropItem[] dadi = FindObjectsOfType<DragAndDropItem>();
                foreach(DragAndDropItem slot in dadi)
                {
                    slot.ReturnBackToSlot();
                }
                
            }
        }
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Physics.Raycast(ray, out hit, reachDistance))
            {
                if (hit.collider.gameObject.GetComponent<Item>() != null)
                {
                    AddItem(hit.collider.gameObject.GetComponent<Item>().item, hit.collider.gameObject.GetComponent<Item>().amount);
                    craftManager.currentCraftItem.FillItemDetails();
                    Destroy(hit.collider.gameObject);
                }
            }
        }
    }
    public void AddItem(ItemScriptableObject _item, int _amount)
    {
        bool allFull = true;
        foreach (InventorySlot inventorySlot in slots)
        {
            if (inventorySlot.isEmpty)
            {
                allFull = false;
                break;
            }
        }
        if (allFull)
        {
            GameObject itemObject = Instantiate(_item.itemPrefab, player.position + Vector3.up + player.forward, Quaternion.identity);
            itemObject.GetComponent<Item>().amount = _amount;
        }
        int amount = _amount;
        foreach (InventorySlot slot in slots)
        {
            // Стакаем предметы вместе
            // В слоте уже имеется этот предмет
            if (slot.item == _item)
            {
                if (slot.amount + amount <= _item.maximumAmount) {
                    slot.amount += amount;
                    slot.itemAmountText.text = slot.amount.ToString();
                    return;
                }
                else
                {
                    amount -= _item.maximumAmount - slot.amount;
                    slot.amount = _item.maximumAmount;
                    slot.itemAmountText.text = slot.amount.ToString();
                }
                //break;
                continue;
            }
        }
        foreach (InventorySlot slot in slots)
        {
            if (amount <= 0)
                return;
            // добавляем предметы в свободные ячейки
            if(slot.isEmpty == true)
            {
                slot.item = _item;
                //slot.amount = amount;
                slot.isEmpty = false;
                slot.SetIcon(_item.icon);
                
                if (amount <= _item.maximumAmount)
                {
                    slot.amount = amount;
                    if (slot.item.maximumAmount != 1) // added this if statement for single items
                    {
                        slot.itemAmountText.text = slot.amount.ToString();
                    }
                    break;
                }
                else
                {
                    slot.amount = _item.maximumAmount;
                    amount -= _item.maximumAmount;
                    if (slot.item.maximumAmount != 1) // added this if statement for single items
                    {
                        slot.itemAmountText.text = slot.amount.ToString();
                    }
                }

                allFull = true;
                foreach (InventorySlot inventorySlot in slots)
                {
                    if (inventorySlot.isEmpty)
                    {
                        allFull = false;
                        break;
                    }
                }
                if (allFull)
                {
                    GameObject itemObject = Instantiate(_item.itemPrefab, player.position + Vector3.up + player.forward, Quaternion.identity);
                    itemObject.GetComponent<Item>().amount = amount;
                    Debug.Log("Throw out");
                    return;
                }

                // continue;
            }
        }
    }
}
