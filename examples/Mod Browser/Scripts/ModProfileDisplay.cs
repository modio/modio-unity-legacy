using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModProfileDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModProfileDisplay display,
                                         int modId);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    [Tooltip("If the profile has no description, the description display element(s) can be filled with the summary instead.")]
    public bool             replaceMissingDescriptionWithSummary;

    [Header("UI Components")]
    public Text         nameDisplay;
    public Text         dateAddedDisplay;
    public Text         dateUpdatedDisplay;
    public Text         dateLiveDisplay;
    public Text         summaryDisplay;
    public Text         descriptionHTMLDisplay;
    public Text         descriptionTextDisplay;

    public UserProfileDisplay           creatorDisplay;
    public ModLogoDisplay               logoDisplay;
    public ModMediaCollectionContainer  mediaContainer;
    public ModfileDisplay               buildDisplay;
    public ModBinaryRequestDisplay      downloadDisplay;

    // TODO(@jackson)
    public LayoutGroup      tagContainer;
    public GameObject       tagBadgePrefab;

    [Header("Display Data")]
    [SerializeField] private int m_modId = -1;

    // --- RUNTIME DATA ---
    private delegate string GetDisplayString(ModProfile profile);

    private Dictionary<Text, GetDisplayString> m_displayMapping = null;
    private List<TextLoadingDisplay> m_loadingDisplays = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // - text displays -
        m_displayMapping = new Dictionary<Text, GetDisplayString>();

        if(nameDisplay != null)
        {
            m_displayMapping.Add(nameDisplay, (p) => p.name);
        }
        if(dateAddedDisplay != null)
        {
            m_displayMapping.Add(dateAddedDisplay, (p) => ServerTimeStamp.ToLocalDateTime(p.dateAdded).ToString());
        }
        if(dateUpdatedDisplay != null)
        {
            m_displayMapping.Add(dateUpdatedDisplay, (p) => ServerTimeStamp.ToLocalDateTime(p.dateUpdated).ToString());
        }
        if(dateLiveDisplay != null)
        {
            m_displayMapping.Add(dateLiveDisplay, (p) => ServerTimeStamp.ToLocalDateTime(p.dateLive).ToString());
        }
        if(summaryDisplay != null)
        {
            m_displayMapping.Add(summaryDisplay, (p) => p.summary);
        }
        if(descriptionHTMLDisplay != null)
        {
            m_displayMapping.Add(descriptionHTMLDisplay, (p) =>
            {
                string description = p.description_HTML;

                if(replaceMissingDescriptionWithSummary
                   && String.IsNullOrEmpty(description))
                {
                    description = p.summary;
                }

                return description;
            });
        }
        if(descriptionTextDisplay != null)
        {
            m_displayMapping.Add(descriptionTextDisplay, (p) =>
            {
                string description = p.description_text;

                if(replaceMissingDescriptionWithSummary
                   && String.IsNullOrEmpty(description))
                {
                    description = p.summary;
                }

                return description;
            });
        }

        TextLoadingDisplay[] childLoadingDisplays = this.gameObject.GetComponentsInChildren<TextLoadingDisplay>(true);
        List<Text> textDisplays = new List<Text>(m_displayMapping.Keys);

        m_loadingDisplays = new List<TextLoadingDisplay>();
        foreach(TextLoadingDisplay loadingDisplay in childLoadingDisplays)
        {
            if(textDisplays.Contains(loadingDisplay.valueDisplayComponent))
            {
                m_loadingDisplays.Add(loadingDisplay);
            }
        }

        // - nested displays -
        if(creatorDisplay != null)
        {
            creatorDisplay.Initialize();
        }
        if(logoDisplay != null)
        {
            logoDisplay.Initialize();
        }
        if(mediaContainer != null)
        {
            mediaContainer.Initialize();
        }
        if(buildDisplay != null)
        {
            buildDisplay.Initialize();
        }
        if(downloadDisplay != null)
        {
            downloadDisplay.Initialize();
        }
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayProfile(ModProfile profile)
    {
        Debug.Assert(profile != null);

        m_modId = profile.id;

        // - text displays -
        foreach(TextLoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(false);
        }
        foreach(var kvp in m_displayMapping)
        {
            kvp.Key.text = kvp.Value(profile);
            kvp.Key.enabled = true;
        }

        // - nested displays -
        if(creatorDisplay != null)
        {
            creatorDisplay.DisplayProfile(profile.submittedBy);
        }
        if(logoDisplay != null)
        {
            logoDisplay.DisplayLogo(profile.id, profile.logoLocator);
        }
        if(mediaContainer != null)
        {
            mediaContainer.DisplayProfileMedia(profile);
        }
        if(buildDisplay != null)
        {
            buildDisplay.DisplayModfile(profile.activeBuild);
        }
        if(downloadDisplay != null)
        {
            ModBinaryRequest download = null;
            foreach(ModBinaryRequest request in ModManager.downloadsInProgress)
            {
                if(request.modId == profile.id)
                {
                    download = request;
                    break;
                }
            }

            downloadDisplay.DisplayRequest(download);
        }
    }

    public void DisplayLoading(int modId = -1)
    {
        m_modId = modId;

        // - text displays -
        foreach(TextLoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(true);
        }
        foreach(Text textComponent in m_displayMapping.Keys)
        {
            textComponent.enabled = false;
        }

        // - nested displays -
        if(creatorDisplay != null)
        {
            creatorDisplay.DisplayLoading();
        }
        if(logoDisplay != null)
        {
            logoDisplay.DisplayLoading(modId);
        }
        if(mediaContainer != null)
        {
            mediaContainer.DisplayLoading(modId);
        }
        if(buildDisplay != null)
        {
            buildDisplay.DisplayLoading(modId);
        }
        if(downloadDisplay != null)
        {
            downloadDisplay.DisplayRequest(null);
        }
    }

    // ---------[ EVENT HANDLING ]---------
    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_modId);
        }
    }
}
