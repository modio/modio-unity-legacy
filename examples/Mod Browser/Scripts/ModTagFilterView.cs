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
    public ModTagCategory[] categories;
    public List<string> selectedTags;

    [Header("Runtime Data")]
    private ModTagCategoryDisplay[] m_categoryDisplayComponents;

    public void Initialize()
    {
        // asserts
        Debug.Assert(tagCategoryPrefab != null);
        Debug.Assert(tagCategoryPrefab.GetComponent<ModTagCategoryDisplay>() != null);
        Debug.Assert(categoryContainer != null);
    }

    public void UpdateDisplay()
    {
        // clear existing
        if(m_categoryDisplayComponents != null
           && m_categoryDisplayComponents.Length > 0)
        {
            foreach(ModTagCategoryDisplay cat in m_categoryDisplayComponents)
            {
                cat.onSelectedTagsChanged -= this.OnTagsChanged;
                GameObject.Destroy(cat.gameObject);
            }
        }

        // setup categories
        m_categoryDisplayComponents = new ModTagCategoryDisplay[categories.Length];
        for(int i = 0; i < categories.Length; ++i)
        {
            ModTagCategory category = categories[i];
            GameObject categoryGO = GameObject.Instantiate(tagCategoryPrefab,
                                                            new Vector3(),
                                                            Quaternion.identity,
                                                            categoryContainer);
            categoryGO.name = category.name;

            ModTagCategoryDisplay categoryDisp = categoryGO.GetComponent<ModTagCategoryDisplay>();
            categoryDisp.Initialize();
            categoryDisp.DisplayCategory(category);
            categoryDisp.selectedTags = this.selectedTags;
            categoryDisp.onSelectedTagsChanged += this.OnTagsChanged;

            m_categoryDisplayComponents[i] = categoryDisp;
        }
    }

    private void OnTagsChanged(ModTagCategoryDisplay displayComponent)
    {
        if(displayComponent.selectedTags != this.selectedTags)
        {
            this.selectedTags = displayComponent.selectedTags;

            foreach(var catDisp in m_categoryDisplayComponents)
            {
                catDisp.selectedTags = this.selectedTags;
            }
        }

        if(onSelectedTagsChanged != null)
        {
            onSelectedTagsChanged();
        }
    }
}
