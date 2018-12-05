using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModTagFilterView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<string> tagFilterAdded;
    public event Action<string> tagFilterRemoved;

    [Header("Settings")]
    public GameObject tagCategoryPrefab;

    [Header("UI Components")]
    public RectTransform tagCategoryContainer;

    // --- RUNTIME DATA ---
    private List<ModTagCategoryDisplay> m_categoryDisplays = new List<ModTagCategoryDisplay>();
    private List<string> m_selectedTags = new List<string>();
    private List<ModTagCategory> m_categories = new List<ModTagCategory>(0);

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
                ModTagContainer tagContainer = categoryDisplay.tagDisplay as ModTagContainer;
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

    public IEnumerable<ModTagCategory> tagCategories
    {
        get { return m_categories; }
        set
        {
            Debug.Assert(value != null);

            m_categories = new List<ModTagCategory>(value);

            // clear existing
            foreach(ModTagCategoryDisplay cat in m_categoryDisplays)
            {
                GameObject.Destroy(cat.gameObject);
            }
            m_categoryDisplays.Clear();

            // create
            foreach(ModTagCategory category in m_categories)
            {
                GameObject categoryGO = CreateCategoryDisplayInstance(category,
                                                                      m_selectedTags,
                                                                      tagCategoryPrefab,
                                                                      tagCategoryContainer);

                categoryGO.GetComponent<ModTagContainer>().tagClicked += TagClickHandler;
                m_categoryDisplays.Add(categoryGO.GetComponent<ModTagCategoryDisplay>());
            }

            if(this.isActiveAndEnabled)
            {
                StartCoroutine(LateUpdateLayouting());
            }
        }
    }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(tagCategoryContainer != null);
        Debug.Assert(tagCategoryPrefab != null);
        Debug.Assert(tagCategoryPrefab.GetComponent<ModTagCategoryDisplay>() != null);

        ModTagContainer tagContainer = tagCategoryPrefab.GetComponent<ModTagContainer>();
        Debug.Assert(tagContainer != null,
                     "[mod.io] ModTagFilterViews require the TagCategoryPrefab to have a "
                     + "ModTagContainer component. (Any other TagCollectionDisplay type "
                     + "is incompatible.)");

        Debug.Assert(tagContainer.tagDisplayPrefab != null);

        Debug.Assert(tagContainer.tagDisplayPrefab.GetComponent<Toggle>() != null,
                     "[mod.io] ModTagFilterViews require the TagDisplayPrefab in the "
                     + "FilterView.tagCategoryPrefab to have a Toggle Component.");
    }

    public void OnEnable()
    {
        StartCoroutine(LateUpdateLayouting());
    }

    public System.Collections.IEnumerator LateUpdateLayouting()
    {
        yield return null;
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tagCategoryContainer);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    private static GameObject CreateCategoryDisplayInstance(ModTagCategory category,
                                                            List<string> selectedTags,
                                                            GameObject prefab,
                                                            RectTransform container)
    {
        GameObject displayGO = GameObject.Instantiate(prefab,
                                                      new Vector3(),
                                                      Quaternion.identity,
                                                      container);
        displayGO.name = category.name;

        ModTagCategoryDisplay display = displayGO.GetComponent<ModTagCategoryDisplay>();
        display.Initialize();
        display.DisplayCategory(category);

        ToggleGroup toggleGroup = null;
        if(!category.isMultiTagCategory)
        {
            toggleGroup = display.gameObject.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = true;
        }

        ModTagContainer tagContainer = displayGO.GetComponent<ModTagContainer>();
        foreach(ModTagDisplay tagDisplay in tagContainer.tagDisplays)
        {
            Toggle tagToggle = tagDisplay.GetComponent<Toggle>();
            tagToggle.isOn = selectedTags.Contains(tagDisplay.tagName);
            tagToggle.group = toggleGroup;
            // TODO(@jackson): Need to register?
        }

        return displayGO;
    }

    // ---------[ EVENTS ]---------
    private void TagClickHandler(ModTagDisplay display, string tagName, string category)
    {
        if(m_selectedTags.Contains(tagName))
        {
            m_selectedTags.Remove(tagName);

            if(tagFilterRemoved != null)
            {
                tagFilterRemoved(tagName);
            }
        }
        else
        {
            m_selectedTags.Add(tagName);

            if(tagFilterAdded != null)
            {
                tagFilterAdded(tagName);
            }
        }
    }
}
