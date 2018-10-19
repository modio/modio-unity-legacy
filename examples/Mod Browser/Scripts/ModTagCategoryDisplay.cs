using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModTagCategoryDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public GameObject modTagPrefab;
    public bool displayNameUpperCase;

    [Header("UI Components")]
    public Text nameText;
    public RectTransform tagContainer;

    [Header("Display Data")]
    public ModTagCategory modTagCategory;
    public string[] selectedTags;

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

        // clear existing
        foreach(ModTagToggle toggle in this.tagToggles)
        {
            // TODO(@jackson): Remove listeners
            GameObject.Destroy(toggle.gameObject);
        }

        // setup
        nameText.text = (displayNameUpperCase ? modTagCategory.name.ToUpper() : modTagCategory.name);

        tagToggles = new ModTagToggle[modTagCategory.tags.Length];
        for(int i = 0; i < modTagCategory.tags.Length; ++i)
        {
            GameObject itemGO = GameObject.Instantiate(modTagPrefab,
                                                       new Vector3(),
                                                       Quaternion.identity,
                                                       tagContainer);

            ModTagToggle item = itemGO.GetComponent<ModTagToggle>();
            item.tagName = modTagCategory.tags[i];
            item.isSelected = IsTagSelected(item.tagName);
            item.Initialize();

            // TODO(@jackson): Add Listeners
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

    private bool IsTagSelected(string tagName)
    {
        Debug.Assert(selectedTags != null);

        for(int i = 0; i < selectedTags.Length; ++i)
        {
            if(selectedTags[i] == tagName) { return true; }
        }

        return false;
    }
}
