using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

[Serializable]
public class SelectableModTag
{
    public string categoryName;
    public string tagName;
    public bool isSelected;
}

public class ModTagCollectionDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public event Action<SelectableModTag> tagToggled;

    [Header("Settings")]
    public GameObject tagDisplayPrefab;

    [Header("UI Components")]
    public RectTransform tagContainer;

    [Header("Display Data")]
    public List<SelectableModTag> tags;

    [Header("Runtime Data")]
    public List<ModTagDisplay> tagDisplays;


    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        Debug.Assert(tagDisplayPrefab != null);
        Debug.Assert(tagDisplayPrefab.GetComponent<ModTagDisplay>() != null);
        Debug.Assert(tagContainer != null);

        UpdateDisplay();
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void UpdateDisplay()
    {
        // clear missing
        foreach(ModTagDisplay display in this.tagDisplays)
        {
            // display.toggled -= OnTagToggled;
            GameObject.Destroy(display.gameObject);
        }

        // setup
        if(tags != null)
        {
            tagDisplays = new List<ModTagDisplay>(tags.Count);
            for(int i = 0; i < tags.Count; ++i)
            {
                GameObject itemGO = GameObject.Instantiate(tagDisplayPrefab,
                                                           new Vector3(),
                                                           Quaternion.identity,
                                                           tagContainer);

                ModTagDisplay item = itemGO.GetComponent<ModTagDisplay>();
                // item.categoryName = tags[i].categoryName;
                // item.tagName = tags[i].tagName;
                // item.isSelected = tags[i].isSelected;
                // item.toggled += OnTagToggled;
                // item.Initialize();

                // tagDisplays.Add(item);
            }
        }
        else
        {
            tagDisplays = new List<ModTagDisplay>(0);
        }
    }

    public void OnTagToggled(ModTagDisplay displayComponent)
    {
        foreach(SelectableModTag tag in tags)
        {
            // if(tag.tagName == displayComponent.tagName)
            // {
            //     tag.isSelected = displayComponent.isSelected;

            //     if(tagToggled != null)
            //     {
            //         tagToggled(tag);
            //     }

            //     return;
            // }
        }
    }
}
