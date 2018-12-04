using System;
using System.Collections.Generic;
using UnityEngine;
using ModIO;

public class ModTagFilterBar : ModTagContainer
{
    // ---------[ FIELDS ]---------
    public event Action onSelectedTagsChanged;

    [Header("Display Data")]
    public ModTagCategory[] categories;
    public List<string> selectedTags;

    // ---------[ INITIALIZATION ]---------
    public override void Initialize()
    {
        base.Initialize();

        this.tagClicked -= TagClickHandler;
        this.tagClicked += TagClickHandler;
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void UpdateDisplay()
    {
        this.DisplayTags(selectedTags, categories);
    }

    private void TagClickHandler(ModTagDisplay display, string tagName, string category)
    {
        selectedTags.Remove(tagName);
        UpdateDisplay();

        if(onSelectedTagsChanged != null)
        {
            onSelectedTagsChanged();
        }
    }
}
