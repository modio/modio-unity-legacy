using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

public class ModTagFilterView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action onSelectedTagsChanged;

    [Header("Settings")]
    public GameObject tagCategoryPrefab;

    [Header("UI Components")]
    public RectTransform categoryContainer;

    [Header("Display Data")]
    public List<string> selectedTags;

    // --- RUNTIME DATA ---
    private IEnumerable<ModTagCategory> TEMP_categories;
    private List<ModTagCategoryDisplay> m_categoryDisplayComponents = new List<ModTagCategoryDisplay>();

    // --- ACCESSORS ---
    public IEnumerable<ModTagCategoryDisplay> categoryDisplayComponents
    { get { return m_categoryDisplayComponents; } }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(categoryContainer != null);

        Debug.Assert(tagCategoryPrefab != null);
    }

    // ---------[ UI FUNCTINALITY ]---------
    [Obsolete]
    public void UpdateDisplay()
    {
        DisplayCategories(TEMP_categories);
    }

    public void DisplayCategories(IEnumerable<ModTagCategory> categories)
    {
        Debug.Assert(categories != null);
        TEMP_categories = categories;

        // clear existing
        foreach(ModTagCategoryDisplay cat in m_categoryDisplayComponents)
        {
            GameObject.Destroy(cat.gameObject);
        }
        m_categoryDisplayComponents.Clear();

        // create
        foreach(ModTagCategory category in categories)
        {
            GameObject displayGO = GameObject.Instantiate(tagCategoryPrefab,
                                                          new Vector3(),
                                                          Quaternion.identity,
                                                          categoryContainer);
            displayGO.name = category.name;

            ModTagCategoryDisplay display = displayGO.GetComponent<ModTagCategoryDisplay>();
            display.Initialize();
            display.DisplayCategory(category);
            display.tagContainer.tagClicked += TagClickHandler;

            m_categoryDisplayComponents.Add(display);
        }
    }

    // ---------[ EVENTS ]---------
    private void TagClickHandler(ModTagDisplay display, string tagName, string category)
    {
        if(selectedTags.Contains(tagName))
        {
            selectedTags.Remove(tagName);
        }
        else
        {
            selectedTags.Add(tagName);
        }

        if(onSelectedTagsChanged != null)
        {
            onSelectedTagsChanged();
        }
    }
}
