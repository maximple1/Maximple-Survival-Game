using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName ="Item",menuName ="Inventory/Items/New Item")]
public class ItemCreator : ItemScriptableObject
{
    private void Start()
    {
        itemType = ItemType.Food;
    }


}
