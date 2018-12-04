using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

[RequireComponent(typeof(TagCollectionContainer))]
public class ModTagCategoryDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public bool capitalizeCategory;

    [Header("UI Components")]
    public Text nameDisplay;

    // ---------[ ACCESSORS ]---------
    public TagCollectionContainer tagContainer
    { get { return this.gameObject.GetComponent<TagCollectionContainer>(); } }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(nameDisplay != null);
        Debug.Assert(tagContainer != null);

        tagContainer.Initialize();
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
        tagContainer.DisplayTags(category.tags, new ModTagCategory[]{ category });
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
