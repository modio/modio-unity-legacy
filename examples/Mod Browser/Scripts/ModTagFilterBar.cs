using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

public class ModTagFilterBar : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action onSelectedTagsChanged;

    [Header("UI Components")]
    public ModTagCollectionDisplay display;

    [Header("Display Data")]
    public ModTagCategory[] categories;
    public List<string> selectedTags;

    // [Header("Runtime Data")]

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        Debug.Assert(display != null);

        display.tags = CreateTagList();
        display.tagToggled += OnTagToggled;

        display.Initialize();
    }

    public void UpdateDisplay()
    {
        display.tags = CreateTagList();
        display.UpdateDisplay();
    }

    private void OnTagToggled(SelectableModTag modTag)
    {
        Debug.Assert(modTag != null);

        bool isTagSelected = selectedTags.Contains(modTag.tagName);

        if(!isTagSelected && modTag.isSelected)
        {
            this.selectedTags.Add(modTag.tagName);

            if(onSelectedTagsChanged != null)
            {
                onSelectedTagsChanged();
            }
        }
        else if(isTagSelected && !modTag.isSelected)
        {
            this.selectedTags.Remove(modTag.tagName);

            if(onSelectedTagsChanged != null)
            {
                onSelectedTagsChanged();
            }
        }

        UpdateDisplay();
    }

    private List<SelectableModTag> CreateTagList()
    {
        if(selectedTags == null) { return new List<SelectableModTag>(0); }

        List<string> uncategorizedTags = new List<string>(selectedTags);
        List<SelectableModTag> retVal = new List<SelectableModTag>(selectedTags.Count);

        if(categories != null)
        {
            foreach(ModTagCategory category in categories)
            {
                foreach(string tagName in category.tags)
                {
                    int tagIndex = uncategorizedTags.IndexOf(tagName);
                    if(tagIndex >= 0)
                    {
                        uncategorizedTags.RemoveAt(tagIndex);
                        retVal.Add(new SelectableModTag()
                        {
                            tagName = tagName,
                            categoryName = category.name,
                            isSelected = true,
                        });
                    }
                }
            }
        }

        foreach(string tagName in uncategorizedTags)
        {
            Debug.LogWarning("[mod.io] No category found for selecetd tag: " + tagName);
            retVal.Add(new SelectableModTag()
            {
                tagName = tagName,
                categoryName = string.Empty,
                isSelected = true,
            });
        }

        return retVal;
    }
}
