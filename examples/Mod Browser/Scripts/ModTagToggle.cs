using System;
using UnityEngine;
using UnityEngine.UI;

public class ModTagToggle : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<bool> onToggled;

    [Header("Settings")]
    public bool displayTagUpperCase;

    [Header("UI Components")]
    public Text nameText;
    public Toggle toggleComponent;

    [Header("Display Data")]
    public string tagName;
    public bool isSelected;


    // ---------[ INTIALIZATION ]---------
    public void Initialize()
    {
        // assert
        Debug.Assert(nameText != null);
        Debug.Assert(toggleComponent != null);

        // setup
        nameText.text = (displayTagUpperCase ? tagName.ToUpper() : tagName);
        toggleComponent.isOn = isSelected;
        toggleComponent.onValueChanged.RemoveListener(OnToggleChanged);
        toggleComponent.onValueChanged.AddListener(OnToggleChanged);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void Refresh()
    {
        nameText.text = (displayTagUpperCase ? tagName.ToUpper() : tagName);
        toggleComponent.isOn = isSelected;
    }

    private void OnToggleChanged(bool value)
    {
        isSelected = value;

        if(onToggled != null)
        {
            onToggled(isSelected);
        }
    }
}
