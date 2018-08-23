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
        inputField.onValueChanged.AddListener(OnSearchFieldChanged);
        inputField.onEndEdit.AddListener(OnSearchFieldSubmit);
    }

    public void OnSearchFieldChanged(string newValue)
    {
        string newestChar = (newValue.Length > 0
                             ? newValue[newValue.Length - 1].ToString()
                             : "NULL");

        Debug.Log("Newest character: " + newestChar);
    }

    public void OnSearchFieldSubmit(string searchValue)
    {
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // update data
            this.profileFilters.Add(searchValue);

            // update ui
            inputField.text = string.Empty;
            UpdateFilterBadges();

            // notify
            if(profileFiltersUpdated != null)
            {
                profileFiltersUpdated(this.profileFilters);
            }
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
        float badgeWidth = 0f;
        float badgeHeight = filterBadgePrefab.GetComponent<RectTransform>().rect.height;

        Text badgeText = filterBadgePrefab.GetComponentInChildren<Text>();
        TextGenerator badgeTextGen = new TextGenerator();
        TextGenerationSettings badgeGenSettings = badgeText.GetGenerationSettings(badgeText.rectTransform.rect.size);


        foreach(string filterValue in this.profileFilters)
        {
            // calculate badge size and position
            badgeWidth = badgeTextGen.GetPreferredWidth(filterValue, badgeGenSettings) + 2 * this.filterBadgePadding;

            if(badgeX + badgeWidth > badgeContainerWidth)
            {
                badgeY -= badgeHeight + this.filterBadgeSpacing;
                badgeX = 0f;

                // TODO(@jackson): Implement better handling
                if((-badgeY + badgeHeight) > badgeContainerHeight)
                {
                    // stop creating badges
                    return;
                }
            }

            // generate badge
            GameObject filterBadge = GameObject.Instantiate(filterBadgePrefab, filterBadgeContainer) as GameObject;
            filterBadge.name = "[FILTER] " + filterValue;
            filterBadge.GetComponentInChildren<Text>().text = filterValue;

            RectTransform filterBadgeTransform = filterBadge.GetComponent<RectTransform>();
            filterBadgeTransform.anchoredPosition = new Vector2(badgeX, badgeY);
            filterBadgeTransform.sizeDelta = new Vector2(badgeWidth, badgeHeight);

            badgeX += badgeWidth + this.filterBadgeSpacing;

            // TODO(@jackson): Add button press event
        }

    }

}
