using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModTagDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModTagDisplay component, string tagName, string category);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public bool capitalizeName;
    public bool capitalizeCategory;

    [Header("UI Components")]
    public Text nameText;
    public Text categoryText;
    public GameObject loadingDisplay;

    // --- DISPLAY DATA ---
    private string m_tag = string.Empty;
    private string m_category = string.Empty;

    // ---------[ INTIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(nameText != null);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayTag(ModTag tag, string category)
    {
        Debug.Assert(tag != null);
        DisplayTag(tag.name, category);
    }

    public void DisplayTag(string tagName, string category)
    {
        m_tag = tagName;
        m_category = category;

        nameText.text = (capitalizeName ? tagName.ToUpper() : tagName);
        if(categoryText != null)
        {
            categoryText.text = (capitalizeCategory ? category.ToUpper() : category);
        }
    }


    // ---------[ EVENT HANDLING ]---------
    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_tag, m_category);
        }
    }
}
