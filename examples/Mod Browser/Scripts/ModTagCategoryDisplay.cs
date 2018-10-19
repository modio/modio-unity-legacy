using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModTagCategoryDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<ModTagCategoryDisplay> onSelectedTagsChanged;

    [Header("Settings")]
    public GameObject modTagPrefab;
    public bool textToUpper;

    [Header("UI Components")]
    public Text nameText;
    public RectTransform tagContainer;

    [Header("Display Data")]
    public ModTagCategory modTagCategory;
    public List<string> selectedTags;

    [Header("Runtime Data")]
    public ModTagToggle[] tagToggles;


    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        Debug.Assert(modTagPrefab != null);
        Debug.Assert(modTagPrefab.GetComponent<ModTagToggle>() != null);
        Debug.Assert(nameText != null);
        Debug.Assert(tagContainer != null);
        Debug.Assert(modTagCategory != null);
        Debug.Assert(selectedTags != null);

        // clear existing
        foreach(ModTagToggle toggle in this.tagToggles)
        {
            toggle.onToggled -= OnTagToggled;
            GameObject.Destroy(toggle.gameObject);
        }

        // setup
        nameText.text = (textToUpper ? modTagCategory.name.ToUpper() : modTagCategory.name);

        tagToggles = new ModTagToggle[modTagCategory.tags.Length];
        for(int i = 0; i < modTagCategory.tags.Length; ++i)
        {
            GameObject itemGO = GameObject.Instantiate(modTagPrefab,
                                                       new Vector3(),
                                                       Quaternion.identity,
                                                       tagContainer);

            ModTagToggle item = itemGO.GetComponent<ModTagToggle>();
            item.categoryName = modTagCategory.name;
            item.tagName = modTagCategory.tags[i];
            item.isSelected = IsTagSelected(item.tagName);
            item.onToggled += OnTagToggled;
            item.Initialize();

            tagToggles[i] = item;
        }
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void Refresh()
    {
        foreach(ModTagToggle tag in tagToggles)
        {
            tag.isSelected = IsTagSelected(tag.tagName);
            tag.Refresh();
        }
    }

    public void OnTagToggled(ModTagToggle toggleComponent)
    {
        bool isTagSelected = IsTagSelected(toggleComponent.tagName);

        if(!isTagSelected && toggleComponent.isSelected)
        {
            this.selectedTags.Add(toggleComponent.tagName);

            if(onSelectedTagsChanged != null)
            {
                onSelectedTagsChanged(this);
            }
        }
        else if(isTagSelected && !toggleComponent.isSelected)
        {
            this.selectedTags.Remove(toggleComponent.tagName);

            if(onSelectedTagsChanged != null)
            {
                onSelectedTagsChanged(this);
            }
        }
    }

    // ---------[ UTILITY ]---------
    private bool IsTagSelected(string tagName)
    {
        Debug.Assert(selectedTags != null);

        return this.selectedTags.Contains(tagName);
    }
}
