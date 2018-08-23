using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;

public class ModBrowserSearchBar : MonoBehaviour
{
    public event Action<IEnumerable<string>> profileFiltersUpdated;

    public GameObject filterBadgePrefab;
    public InputField inputField;
    public RectTransform filterBadgeContainer;
    // public Dropdown searchSuggestionDropdown;

    public float filterBadgePadding;
    public float filterBadgeSpacing;

    public List<string> profileFilters;

    private void Start()
    {
        inputField.onValueChanged.AddListener(OnInputFieldChanged);
        inputField.onEndEdit.AddListener(OnInputFieldSubmission);
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

            // process input & update data
            string[] filterValueItems = filterInput.Split(',');
            foreach(var filterValueRaw in filterValueItems)
            {
                string filterValue = filterValueRaw.Trim();

                if(!this.profileFilters.Contains(filterValue))
                {
                    this.profileFilters.Add(filterValue);
                    isFilterChanged = true;
                }
            }

            // ui & notification
            inputField.text = string.Empty;

            if(isFilterChanged)
            {
                UpdateFilterBadges();

                // notify
                if(profileFiltersUpdated != null)
                {
                    profileFiltersUpdated(this.profileFilters);
                }
            }
        }
    }

    public void OnBadgeClicked(string filterValue)
    {
        // data
        this.profileFilters.Remove(filterValue);

        // ui
        UpdateFilterBadges();

        // notification
        if(profileFiltersUpdated != null)
        {
            profileFiltersUpdated(this.profileFilters);
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

        float badgeX = 0f;
        float badgeY = 0f;
        float badgeHeight = filterBadgePrefab.GetComponent<RectTransform>().rect.height;

        ModBrowserFilterBadge badgePrefabScript = filterBadgePrefab.GetComponent<ModBrowserFilterBadge>();

        foreach(string filterValue in this.profileFilters)
        {
            float filterBadgeWidth = badgePrefabScript.CalculateWidth(filterValue);

            // calculate badge size and position
            if(badgeX + filterBadgeWidth > badgeContainerWidth)
            {
                badgeY -= badgeHeight + this.filterBadgeSpacing;
                badgeX = 0f;

                // TODO(@jackson): Implement better handling
                if(-badgeY + badgeHeight > badgeContainerHeight)
                {
                    // stop creating badges
                    return;
                }
            }

            // generate badge
            GameObject filterBadge = GameObject.Instantiate(filterBadgePrefab, filterBadgeContainer) as GameObject;
            filterBadge.name = "[FILTER] " + filterValue;

            RectTransform filterBadgeTransform = filterBadge.GetComponent<RectTransform>();
            filterBadgeTransform.anchoredPosition = new Vector2(badgeX, badgeY);
            filterBadgeTransform.sizeDelta = new Vector2(filterBadgeWidth, badgeHeight);

            var filterBadgeScript = filterBadge.GetComponent<ModBrowserFilterBadge>();
            filterBadgeScript.textComponent.text = filterValue;
            filterBadgeScript.buttonComponent.onClick.AddListener(() => OnBadgeClicked(filterValue));

            badgeX += filterBadgeWidth + this.filterBadgeSpacing;
        }
    }

}
