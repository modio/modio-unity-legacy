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
    public event Action<ModTagCategoryDisplay> onSelectedTagsChanged;

    [Header("Settings")]
    public bool capitalizeCategory;

    [Header("UI Components")]
    public Text nameDisplay;

    // --- RUNTIME DATA ---
    public List<string> selectedTags = new List<string>();

    private TagCollectionContainer m_tagContainer;
    public TagCollectionContainer tagContainer { get { return m_tagContainer; } }

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        Debug.Assert(nameDisplay != null);

        m_tagContainer = this.gameObject.GetComponent<TagCollectionContainer>();
        Debug.Assert(m_tagContainer != null);

        m_tagContainer.Initialize();
        m_tagContainer.tagClicked -= TagClickHandler;
        m_tagContainer.tagClicked += TagClickHandler;
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
        m_tagContainer.DisplayTags(category.tags, new ModTagCategory[]{ category });
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
            onSelectedTagsChanged(this);
        }
    }

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
