using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlInputValue : MonoBehaviour
{
    private TMP_InputField inputField;

    private void Start()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void ValueChanged()
    {
        if (inputField.text.Contains("-") || inputField.text.Length <= 0 || int.Parse(inputField.text) <= 0)
            inputField.text = "1";
    }
}
