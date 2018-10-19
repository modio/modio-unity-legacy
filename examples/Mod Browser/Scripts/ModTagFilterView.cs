using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

// TODO(@jackson): Single-select categories
public class ModTagFilterView : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action onSelectedTagsChanged;

    [Header("Settings")]
    public GameObject tagCategoryPrefab;

    [Header("UI Components")]
    public RectTransform categoryContainer;

    [Header("Display Data")]
    public ModTagCategory[] tagCategories;
    public List<string> selectedTags;

    [Header("Runtime Data")]
    public ModTagCategoryDisplay[] categoryDisplayComponents;

    public void Initialize()
    {
        // asserts
        Debug.Assert(tagCategoryPrefab != null);
        Debug.Assert(tagCategoryPrefab.GetComponent<ModTagCategoryDisplay>() != null);
        Debug.Assert(categoryContainer != null);

        // clear existing
        if(categoryDisplayComponents != null
           && categoryDisplayComponents.Length > 0)
        {
            foreach(ModTagCategoryDisplay cat in categoryDisplayComponents)
            {
                cat.onSelectedTagsChanged -= this.OnTagsChanged;
                GameObject.Destroy(cat.gameObject);
            }
        }

        // setup categories
        categoryDisplayComponents = new ModTagCategoryDisplay[tagCategories.Length];
        for(int i = 0; i < tagCategories.Length; ++i)
        {
            ModTagCategory category = tagCategories[i];
            GameObject categoryGO = GameObject.Instantiate(tagCategoryPrefab,
                                                            new Vector3(),
                                                            Quaternion.identity,
                                                            categoryContainer);
            categoryGO.name = category.name;

            ModTagCategoryDisplay categoryDisp = categoryGO.GetComponent<ModTagCategoryDisplay>();
            categoryDisp.modTagCategory = category;
            categoryDisp.selectedTags = this.selectedTags;
            categoryDisp.onSelectedTagsChanged += this.OnTagsChanged;
            categoryDisp.Initialize();
        }
    }

    private void OnTagsChanged(ModTagCategoryDisplay displayComponent)
    {
        if(displayComponent.selectedTags != this.selectedTags)
        {
            this.selectedTags = displayComponent.selectedTags;

            foreach(var catDisp in categoryDisplayComponents)
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
