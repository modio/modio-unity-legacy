using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;

using ModIO;

public class ModBrowserSearchBar : MonoBehaviour
{
    [System.Serializable]
    public class SimpleTag
    {
        public string category;
        public string name;
    }

    public event Action<string, IEnumerable<string>> profileFiltersUpdated;

    public GameObject filterBadgePrefab;
    public InputField inputField;
    public RectTransform filterBadgeContainer;
    // public Dropdown searchSuggestionDropdown;

    public float filterBadgePadding;
    public float filterBadgeSpacing;

    public string textFilter;
    public List<SimpleTag> tagFilters;

    public Dictionary<string, string[]> singleTagCategories;
    public Dictionary<string, string[]> multiTagCategories;


    private static bool IsTagInCategoryList(string categoryName, string tagName,
                                            Dictionary<string, string[]> categories)
    {
        string[] tags;

        if(categories.TryGetValue(categoryName, out tags))
        {
            foreach(string tag in tags)
            {
                if(tag == tagName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> SimpleTagsAsTagNames(IEnumerable<SimpleTag> tags)
    {
        List<string> tagNames = new List<string>();
        foreach(SimpleTag tag in tags)
        {
            tagNames.Add(tag.name);
        }
        return tagNames;
    }

    public void Initialize()
    {
        inputField.onValueChanged.AddListener(OnInputFieldChanged);
        inputField.onEndEdit.AddListener(OnInputFieldSubmission);

        singleTagCategories = new Dictionary<string, string[]>();
        multiTagCategories = new Dictionary<string, string[]>();

        ModManager.GetGameProfile(ProcessTagCategories,
                                  null);
    }

    private void ProcessTagCategories(GameProfile profile)
    {
        foreach(ModTagCategory category in profile.tagCategories)
        {
            if(!category.isHidden)
            {
                if(category.isMultiTagCategory)
                {
                    multiTagCategories.Add(category.name,
                                           category.tags);
                }
                else
                {
                    singleTagCategories.Add(category.name,
                                            category.tags);
                }
            }
        }
    }

    public void OnInputFieldChanged(string newValue)
    {
        return;
    }

    public void OnInputFieldSubmission(string filterInput)
    {
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            bool isFilterChanged = false;

            // check for tagging
            bool isTag = false;
            string[] tagParts = filterInput.Split(':');
            if(tagParts.Length == 2)
            {
                SimpleTag inputTag = new SimpleTag()
                {
                    category = tagParts[0].Trim(),
                    name = tagParts[1].Trim(),
                };

                if(IsTagInCategoryList(inputTag.category, inputTag.name,
                                       multiTagCategories))
                {
                    // check if already filtering
                    foreach(SimpleTag tagFilter in tagFilters)
                    {
                        if(tagFilter.category == inputTag.category
                           && tagFilter.name == inputTag.name)
                        {
                            return;
                        }
                    }

                    tagFilters.Add(inputTag);
                    isFilterChanged = true;
                    isTag = true;
                }
                else if(IsTagInCategoryList(inputTag.category, inputTag.name,
                                            singleTagCategories))
                {
                    // check if already filtering category
                    for(int i = 0; i < tagFilters.Count; ++i)
                    {
                        if(tagFilters[i].category == inputTag.category)
                        {
                            if(tagFilters[i].name == inputTag.name)
                            {
                                return;
                            }
                            else
                            {
                                tagFilters.RemoveAt(i);
                                i = tagFilters.Count; // break
                            }
                        }
                    }

                    tagFilters.Add(inputTag);
                    isFilterChanged = true;
                    isTag = true;
                }
            }

            // if not already handled
            if(!isTag
               && this.textFilter != filterInput)
            {
                this.textFilter = filterInput;
                isFilterChanged = true;
            }

            // ui & notification
            inputField.text = string.Empty;

            if(isFilterChanged)
            {
                UpdateFilterBadges();

                // notify
                if(profileFiltersUpdated != null)
                {
                    profileFiltersUpdated(this.textFilter,
                                          SimpleTagsAsTagNames(tagFilters));
                }
            }
        }
    }

    public void OnTextBadgeClicked()
    {
        // data
        this.textFilter = string.Empty;

        // ui
        UpdateFilterBadges();

        // notification
        if(profileFiltersUpdated != null)
        {
            profileFiltersUpdated(this.textFilter,
                                  SimpleTagsAsTagNames(tagFilters));
        }
    }

    public void OnTagBadgeClicked(SimpleTag filterTag)
    {
        // data
        this.tagFilters.Remove(filterTag);

        // ui
        UpdateFilterBadges();

        // notification
        if(profileFiltersUpdated != null)
        {
            profileFiltersUpdated(this.textFilter,
                                  SimpleTagsAsTagNames(tagFilters));
        }
    }

    public void UpdateFilterBadges()
    {
        // remove current badges
        foreach(Transform badgeTransform in filterBadgeContainer)
        {
            GameObject.Destroy(badgeTransform.gameObject);
        }

        // --- --- ---
        float badgeContainerWidth = filterBadgeContainer.rect.width;
        float badgeContainerHeight = filterBadgeContainer.rect.height;

        Vector2 badgePos = new Vector2(0f, 0f);
        float badgeHeight = filterBadgePrefab.GetComponent<RectTransform>().rect.height;

        ModBrowserFilterBadge badgePrefabScript = filterBadgePrefab.GetComponent<ModBrowserFilterBadge>();

        // text filter
        if(!String.IsNullOrEmpty(this.textFilter))
        {
            AddFilterBadge("\"" + this.textFilter + "\"",
                           badgePrefabScript,
                           OnTextBadgeClicked,
                           badgeContainerWidth, badgeContainerHeight,
                           badgeHeight,
                           ref badgePos);
        }

        // tag filters
        foreach(SimpleTag tag in this.tagFilters)
        {
            AddFilterBadge(tag.category + ": " + tag.name,
                           badgePrefabScript,
                           () => OnTagBadgeClicked(tag),
                           badgeContainerWidth, badgeContainerHeight,
                           badgeHeight,
                           ref badgePos);
        }
    }

    public void AddFilterBadge(string badgeText,
                               ModBrowserFilterBadge badgePrefabScript,
                               Action onClickCallback,
                               float badgeContainerWidth, float badgeContainerHeight,
                               float badgeHeight,
                               ref Vector2 badgePos)
    {
        float filterBadgeWidth = badgePrefabScript.CalculateWidth(badgeText);

        // calculate badge size and position
        if(badgePos.x + filterBadgeWidth > badgeContainerWidth)
        {
            badgePos.y -= badgeHeight + this.filterBadgeSpacing;
            badgePos.x = 0f;

            // TODO(@jackson): Implement better handling
            if(-badgePos.y + badgeHeight > badgeContainerHeight)
            {
                // stop creating badges
                return;
            }
        }

        // generate badge
        GameObject filterBadge = GameObject.Instantiate(filterBadgePrefab, filterBadgeContainer) as GameObject;
        filterBadge.name = "[FILTER] " + badgeText;

        RectTransform filterBadgeTransform = filterBadge.GetComponent<RectTransform>();
        filterBadgeTransform.anchoredPosition = new Vector2(badgePos.x, badgePos.y);
        filterBadgeTransform.sizeDelta = new Vector2(filterBadgeWidth, badgeHeight);

        var filterBadgeScript = filterBadge.GetComponent<ModBrowserFilterBadge>();
        filterBadgeScript.textComponent.text = badgeText;
        filterBadgeScript.buttonComponent.onClick.AddListener(() => onClickCallback());

        badgePos.x += filterBadgeWidth + this.filterBadgeSpacing;
    }

}
