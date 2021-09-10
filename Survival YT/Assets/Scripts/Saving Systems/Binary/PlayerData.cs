using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // дает возможность сохранить этот класс в файл
public class PlayerData
{
  public float health;
  public float food;
  public float water;
  
  public float[] position;
  
  public string[] itemNames;
  public int[] itemAmounts;

  public PlayerData (Indicators indicators, CustomCharacterController player, InventoryManager inventoryManager)
  {
    health = indicators.healthAmount;
    food = indicators.foodAmount;
    water = indicators.waterAmount;
    
    position = new float[3];
    var playerPosition = player.transform.position;
    position[0] = playerPosition.x;
    position[1] = playerPosition.y;
    position[2] = playerPosition.z;

    itemNames = new string[inventoryManager.slots.Count];
    itemAmounts = new int[inventoryManager.slots.Count];
    
    for (int i = 0; i < inventoryManager.slots.Count; i++)
    {
      if(inventoryManager.slots[i].item != null)
        itemNames[i] = inventoryManager.slots[i].item.name;
    }
    
    for (int i = 0; i < inventoryManager.slots.Count; i++)
    {
      if(inventoryManager.slots[i].item != null)
        itemAmounts[i] = inventoryManager.slots[i].amount;
    }
  }
}
