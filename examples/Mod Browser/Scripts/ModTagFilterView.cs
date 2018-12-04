using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModTagFilterView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    // TODO(@jackson): Separate
    public event Action onSelectedTagsChanged;

    [Header("Settings")]
    public GameObject tagCategoryPrefab;

    [Header("UI Components")]
    public RectTransform categoryContainer;

    // --- RUNTIME DATA ---
    private IEnumerable<ModTagCategory> TEMP_categories;
    private List<ModTagCategoryDisplay> m_categoryDisplays = new List<ModTagCategoryDisplay>();
    private List<string> m_selectedTags = new List<string>();

    // --- ACCESSORS ---
    public IEnumerable<ModTagCategoryDisplay> categoryDisplays
    { get { return m_categoryDisplays; } }
    public IEnumerable<string> selectedTags
    {
        get { return m_selectedTags; }
        set
        {
            Debug.Assert(value != null);

            m_selectedTags = new List<string>(value);

            foreach(ModTagCategoryDisplay categoryDisplay in m_categoryDisplays)
            {
                TagCollectionContainer tagContainer = categoryDisplay.tagDisplay as TagCollectionContainer;
                tagContainer.tagClicked -= TagClickHandler;

                foreach(ModTagDisplay tagDisplay in tagContainer.tagDisplays)
                {
                    Toggle tagToggle = tagDisplay.GetComponent<Toggle>();
                    tagToggle.isOn = m_selectedTags.Contains(tagDisplay.tagName);
                }

                tagContainer.tagClicked += TagClickHandler;
            }
        }
    }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(categoryContainer != null);
        Debug.Assert(tagCategoryPrefab != null);
        Debug.Assert(tagCategoryPrefab.GetComponent<ModTagCategoryDisplay>() != null);

        TagCollectionContainer tagContainer = tagCategoryPrefab.GetComponent<TagCollectionContainer>();
        Debug.Assert(tagContainer != null,
                     "[mod.io] ModTagFilterViews require the Tag Category Prefab to have a "
                     + "TagCollectionContainer component. (Any other TagCollectionDisplay type "
                     + "is incompatible.)");

        Debug.Assert(tagContainer.tagDisplayPrefab != null);

        Debug.Assert(tagContainer.tagDisplayPrefab.GetComponent<Toggle>() != null,
                     "[mod.io] The TagDisplayPrefab in the FilterView.tagCategoryPrefab requires a "
                     + "Toggle component for the purpose of indicating the tag as selected or not.");
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
        foreach(ModTagCategoryDisplay cat in m_categoryDisplays)
        {
            GameObject.Destroy(cat.gameObject);
        }
        m_categoryDisplays.Clear();

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

            TagCollectionContainer tagContainer = displayGO.GetComponent<TagCollectionContainer>();
            foreach(ModTagDisplay tagDisplay in tagContainer.tagDisplays)
            {
                Toggle tagToggle = tagDisplay.GetComponent<Toggle>();
                tagToggle.isOn = m_selectedTags.Contains(tagDisplay.tagName);
            }

            tagContainer.tagClicked += TagClickHandler;

            m_categoryDisplays.Add(display);
        }
    }

    // ---------[ EVENTS ]---------
    private void TagClickHandler(ModTagDisplay display, string tagName, string category)
    {
        if(m_selectedTags.Contains(tagName))
        {
            m_selectedTags.Remove(tagName);
        }
        else
        {
            m_selectedTags.Add(tagName);
        }

        if(onSelectedTagsChanged != null)
        {
            onSelectedTagsChanged();
        }
    }
}
