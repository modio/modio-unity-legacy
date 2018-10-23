using System;
using UnityEngine;
using UnityEngine.UI;

public class ModTagDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<ModTagDisplay> toggled;

    [Header("Settings")]
    public bool textToUpper;

    [Header("UI Components")]
    public Text nameText;
    public Text categoryText;
    public Toggle toggleComponent;

    [Header("Display Data")]
    public string tagName = string.Empty;
    public string categoryName = string.Empty;
    public bool isSelected = false;


    // ---------[ INTIALIZATION ]---------
    public void Initialize()
    {
        // assert
        Debug.Assert(nameText != null);

        // events
        if(toggleComponent != null)
        {
            toggleComponent.onValueChanged.RemoveListener(OnToggleChanged);
            toggleComponent.onValueChanged.AddListener(OnToggleChanged);
        }

        UpdateDisplay();
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void UpdateDisplay()
    {
        // display
        nameText.text = (textToUpper ? tagName.ToUpper() : tagName);
        if(categoryText != null)
        {
            categoryText.text = (textToUpper ? categoryName.ToUpper() : categoryName);
        }
        if(toggleComponent != null)
        {
            toggleComponent.isOn = isSelected;
        }
    }

    private void OnToggleChanged(bool value)
    {
        isSelected = value;

        if(toggled != null)
        {
            toggled(this);
        }
    }
}
