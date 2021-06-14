using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Indicators : MonoBehaviour
{
    public Image healthBar, foodBar, waterBar;
    private Camera mainCamera;

    public float healthAmount = 100;
    private float uiHealthAmount = 100;

    public float foodAmount = 100;
    private float uiFoodAmount = 100;

    public float waterAmount = 100;
    public float uiWaterAmount = 100;

    public float secondsToEmptyFood = 60f;
    public float secondsToEmptyWater = 30f;
    public float secondsToEmptyHealth = 60f;

    private float changeFactor = 6f;
    public bool isInWater = false;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        healthBar.fillAmount = healthAmount / 100;
        foodBar.fillAmount = foodAmount / 100;
        waterBar.fillAmount = waterAmount / 100;
    }

    // Update is called once per frame
    void Update()
    {
        if (isInWater)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ChangeWaterAmount(50);
            }
        }
        if (foodAmount > 0)
        {
            foodAmount -= 100 / secondsToEmptyFood * Time.deltaTime;
            uiFoodAmount = Mathf.Lerp(uiFoodAmount, foodAmount, Time.deltaTime * changeFactor);
            foodBar.fillAmount = uiFoodAmount / 100;
            
        }
        else
        {
            uiFoodAmount = 0;
            foodBar.fillAmount = uiFoodAmount / 100;
        }
        if (waterAmount > 0)
        {
            waterAmount -= 100 / secondsToEmptyWater * Time.deltaTime;
            uiWaterAmount = Mathf.Lerp(uiWaterAmount, waterAmount, Time.deltaTime * changeFactor);
            waterBar.fillAmount = uiWaterAmount / 100;
        }
        else
        {
            uiWaterAmount = 0;
            waterBar.fillAmount = uiWaterAmount / 100;
        }

        if (foodAmount <= 0)
        {
            healthAmount -= 100 / secondsToEmptyHealth * Time.deltaTime;
        }
        if (waterAmount <= 0)
        {
            healthAmount -= 100 / secondsToEmptyHealth * Time.deltaTime;
        }
        uiHealthAmount = Mathf.Lerp(uiHealthAmount, healthAmount, Time.deltaTime * changeFactor);
        healthBar.fillAmount = uiHealthAmount / 100;
    }

    public void ChangeFoodAmount(float changeValue)
    {
        if (foodAmount + changeValue > 100)
        {
            foodAmount = 100;
        }
        else
        {
            foodAmount += changeValue;
        }
    }
    public void ChangeWaterAmount(float changeValue)
    {
        if (waterAmount + changeValue > 100)
        {
            waterAmount = 100;
            
        }
        else
        {
            waterAmount += changeValue;
        }
    }
    public void ChangeHealthAmount(float changeValue)
    {
        if (healthAmount + changeValue > 100)
        {
            healthAmount = 100;
        }
        else
        {
            healthAmount += changeValue;
        }
    }
}
