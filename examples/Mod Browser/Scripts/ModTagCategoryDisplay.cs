using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

// TODO(@jackson): Implement single-selection
public class ModTagCategoryDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<ModTagCategoryDisplay> onSelectedTagsChanged;

    [Header("Settings")]
    public bool textToUpper;

    [Header("UI Components")]
    public Text nameText;
    public ModTagCollectionDisplay tagDisplay;

    [Header("Display Data")]
    public ModTagCategory modTagCategory;
    public List<string> selectedTags;


    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        Debug.Assert(tagDisplay != null);

        // setup
        string categoryName = string.Empty;
        if(modTagCategory != null)
        {
            categoryName = (textToUpper ? modTagCategory.name.ToUpper() : modTagCategory.name);
        }
        if(nameText != null)
        {
            nameText.text = categoryName;
        }

        tagDisplay.tags = CreateTagList();
        tagDisplay.tagToggled += OnTagToggled;
        tagDisplay.Initialize();
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void UpdateDisplay()
    {
        string categoryName = string.Empty;
        if(modTagCategory != null)
        {
            categoryName = (textToUpper ? modTagCategory.name.ToUpper() : modTagCategory.name);
        }
        if(nameText != null)
        {
            nameText.text = categoryName;
        }

        tagDisplay.tags = CreateTagList();
        tagDisplay.UpdateDisplay();
    }

    public void OnTagToggled(SelectableModTag modTag)
    {
        bool isTagSelected = IsTagSelected(modTag.tagName);

        if(!isTagSelected && modTag.isSelected)
        {
            this.selectedTags.Add(modTag.tagName);

            if(onSelectedTagsChanged != null)
            {
                onSelectedTagsChanged(this);
            }
        }
        else if(isTagSelected && !modTag.isSelected)
        {
            this.selectedTags.Remove(modTag.tagName);

            if(onSelectedTagsChanged != null)
            {
                onSelectedTagsChanged(this);
            }
        }
    }

    // ---------[ UTILITY ]---------
    private bool IsTagSelected(string tagName)
    {
        if(this.selectedTags == null) { return false; }
        return this.selectedTags.Contains(tagName);
    }

    private List<SelectableModTag> CreateTagList()
    {
        List<SelectableModTag> tags;

        if(modTagCategory != null
           && modTagCategory.tags != null)
        {
            tags = new List<SelectableModTag>(modTagCategory.tags.Length);
            foreach(string tagName in modTagCategory.tags)
            {
                tags.Add(new SelectableModTag()
                {
                    categoryName = modTagCategory.name,
                    tagName = tagName,
                    isSelected = IsTagSelected(tagName),
                });
            }
        }
        else
        {
            tags = new List<SelectableModTag>(0);
        }

        return tags;
    }
}
