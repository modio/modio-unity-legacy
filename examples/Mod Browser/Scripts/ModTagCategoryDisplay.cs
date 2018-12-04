using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

[RequireComponent(typeof(TagCollectionDisplayBase))]
public class ModTagCategoryDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public bool capitalizeCategory;

    [Header("UI Components")]
    public Text nameDisplay;

    // ---------[ ACCESSORS ]---------
    public TagCollectionDisplayBase tagDisplay
    { get { return this.gameObject.GetComponent<TagCollectionDisplayBase>(); } }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(nameDisplay != null);
        Debug.Assert(tagDisplay != null);

        tagDisplay.Initialize();
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayCategory(string categoryName, IEnumerable<string> tags)
    {
        Debug.Assert(categoryName != null);
        Debug.Assert(tags != null);

        ModTagCategory category = new ModTagCategory()
        {
            name = categoryName,
            tags = tags.ToArray(),
        };
        DisplayCategory(category);
    }
    public void DisplayCategory(ModTagCategory category)
    {
        Debug.Assert(category != null);

        nameDisplay.text = (capitalizeCategory ? category.name.ToUpper() : category.name);
        tagDisplay.DisplayTags(category.tags, new ModTagCategory[]{ category });
    }

    // ---------[ EVENTS ]---------
    // public void OnTagToggled(SelectableModTag modTag)
    // {
    //     bool isTagSelected = IsTagSelected(modTag.tagName);

    //     if(!isTagSelected && modTag.isSelected)
    //     {
    //         this.selectedTags.Add(modTag.tagName);

    //         if(onSelectedTagsChanged != null)
    //         {
    //             onSelectedTagsChanged(this);
    //         }
    //     }
    //     else if(isTagSelected && !modTag.isSelected)
    //     {
    //         this.selectedTags.Remove(modTag.tagName);

    //         if(onSelectedTagsChanged != null)
    //         {
    //             onSelectedTagsChanged(this);
    //         }
    //     }
    // }
}
