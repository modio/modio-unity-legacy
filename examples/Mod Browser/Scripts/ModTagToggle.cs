using System;
using UnityEngine;
using UnityEngine.UI;

public class ModTagToggle : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<ModTagToggle> onToggled;

    [Header("Settings")]
    public bool textToUpper;

    [Header("UI Components")]
    public Text nameText;
    public Text categoryText;
    public Toggle toggleComponent;

    [Header("Display Data")]
    public string tagName;
    public string categoryName;
    public bool isSelected;


    // ---------[ INTIALIZATION ]---------
    public void Initialize()
    {
        // assert
        Debug.Assert(nameText != null);
        Debug.Assert(toggleComponent != null);

        // events
        toggleComponent.onValueChanged.RemoveListener(OnToggleChanged);
        toggleComponent.onValueChanged.AddListener(OnToggleChanged);

        // display
        nameText.text = (textToUpper ? tagName.ToUpper() : tagName);
        if(categoryText != null)
        {
            categoryText.text = (textToUpper ? categoryName.ToUpper() : categoryName);
        }
        toggleComponent.isOn = isSelected;
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void Refresh()
    {
        nameText.text = (textToUpper ? tagName.ToUpper() : tagName);
        if(categoryText != null)
        {
            categoryText.text = (textToUpper ? categoryName.ToUpper() : categoryName);
        }
        toggleComponent.isOn = isSelected;
    }

    private void OnToggleChanged(bool value)
    {
        isSelected = value;

        if(onToggled != null)
        {
            onToggled(this);
        }
    }
}
