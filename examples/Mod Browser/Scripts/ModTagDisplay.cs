using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModTagDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModTagDisplay component, string tag, string category);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public bool capitalizeName;
    public bool capitalizeCategory;

    [Header("UI Components")]
    public Text nameDisplay;
    public Text categoryDisplay;
    public GameObject loadingDisplay;

    // --- DISPLAY DATA ---
    private string m_tag = string.Empty;
    private string m_category = string.Empty;

    // --- ACCESSORS ---
    public string tagName       { get { return m_tag; } }
    public string categoryName  { get { return m_category; } }

    // ---------[ INTIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(nameDisplay != null);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayTag(ModTag tag, string category)
    {
        Debug.Assert(tag != null);
        DisplayTag(tag.name, category);
    }

    public void DisplayTag(string tag, string category)
    {
        m_tag = tag;
        m_category = category;

        nameDisplay.text = (capitalizeName ? tag.ToUpper() : tag);
        nameDisplay.enabled = true;
        if(categoryDisplay != null)
        {
            categoryDisplay.text = (capitalizeCategory ? category.ToUpper() : category);
            categoryDisplay.enabled = true;
        }

        if(loadingDisplay != null)
        {
            loadingDisplay.gameObject.SetActive(false);
        }
    }

    public void DisplayLoading(string tag = null, string category = null)
    {
        m_tag = tag;
        m_category = category;

        nameDisplay.enabled = false;
        if(categoryDisplay != null)
        {
            categoryDisplay.enabled = false;
        }

        if(loadingDisplay != null)
        {
            loadingDisplay.gameObject.SetActive(true);
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
