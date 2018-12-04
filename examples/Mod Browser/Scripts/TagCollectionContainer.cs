using System.Collections.Generic;
using UnityEngine;
using ModIO;

public class TagCollectionContainer : TagCollectionDisplayBase
{
    // ---------[ FIELDS ]---------
    public delegate void OnTagClicked(ModTagDisplay component, string tagName, string category);
    public event OnTagClicked tagClicked;

    [Header("Settings")]
    public GameObject tagDisplayPrefab;

    [Header("UI Components")]
    public RectTransform container;
    public GameObject loadingDisplay;

    // --- RUNTIME DATA ---
    public int m_modId = -1;

    // ---------[ INITIALIZATION ]---------
    public override void Initialize()
    {
        Debug.Assert(container != null);

        Debug.Assert(tagDisplayPrefab != null);
        Debug.Assert(tagDisplayPrefab.GetComponent<ModTagDisplay>() != null);
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public override void DisplayTags(IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
    {
        DisplayModTags(-1, tags, tagCategories);
    }
    public override void DisplayModTags(ModProfile profile, IEnumerable<ModTagCategory> tagCategories)
    {
        Debug.Assert(profile != null);
        DisplayModTags(profile.id, profile.tagNames, tagCategories);
    }
    public override void DisplayModTags(int modId, IEnumerable<string> tags, IEnumerable<ModTagCategory> tagCategories)
    {
        Debug.Assert(tags != null);

        m_modId = modId;

        foreach(Transform t in container)
        {
            GameObject.Destroy(t.gameObject);
        }

        IDictionary<string, string> tagCategoryMap = TagCollectionDisplayBase.GenerateTagCategoryMap(tags,
                                                                                                     tagCategories);

        foreach(var tagCategory in tagCategoryMap)
        {
            GameObject displayGO = GameObject.Instantiate(tagDisplayPrefab,
                                                          new Vector3(),
                                                          Quaternion.identity,
                                                          container);

            ModTagDisplay display = displayGO.GetComponent<ModTagDisplay>();
            display.Initialize();
            display.DisplayTag(tagCategory.Key, tagCategory.Value);
            display.onClick += NotifyTagClicked;
        }

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(false);
        }
    }

    public override void DisplayLoading(int modId = -1)
    {
        m_modId = modId;

        foreach(Transform t in container)
        {
            GameObject.Destroy(t.gameObject);
        }

        if(loadingDisplay != null)
        {
            loadingDisplay.SetActive(true);
        }
    }

    // ---------[ EVENTS ]---------
    public void NotifyTagClicked(ModTagDisplay component, string tagName, string category)
    {
        if(tagClicked != null)
        {
            tagClicked(component, tagName, category);
        }
    }
}
