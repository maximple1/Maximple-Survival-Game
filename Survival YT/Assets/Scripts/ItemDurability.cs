using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDurability : MonoBehaviour
{
    private float _currentDurability;
    [SerializeField] private int _hitsTillZero;
    public InventorySlot inventorySlot;


    private void Start()
    {
        _currentDurability = 100;
    }

    public void SubtractDurabilityPerHit()
    {
        _currentDurability -= 100f / _hitsTillZero;
        UpdateDurability();
    }
    private void UpdateDurability()
    {
        inventorySlot.itemDurability = _currentDurability;
        inventorySlot.UpdateDurabilityBar();
    }
}
