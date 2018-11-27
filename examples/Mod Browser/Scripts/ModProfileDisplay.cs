using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModProfileDisplay : MonoBehaviour, IModProfilePresenter
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModProfileDisplay display,
                                         int modId);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public GameObject       textLoadingPrefab;
    public GameObject       tagBadgePrefab;
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
    public ModMediaCollectionDisplay    mediaDisplay;
    public LayoutGroup  tagContainer;
    public ModfileDisplay buildDisplay;

    [Header("Display Data")]
    [SerializeField] private int m_modId = -1;

    // ---[ RUNTIME DATA ]---
    private delegate string GetDisplayString(ModProfile profile);

    private bool m_isInitialized = false;
    private Dictionary<Text, GetDisplayString> m_displayMapping = null;
    private List<GameObject> m_loadingInstances = null;
    private List<IModProfilePresenter> m_nestedPresenters = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        if(m_isInitialized)
        {
            #if DEBUG
            Debug.LogWarning("[mod.io] Once initialized, a ModProfileDisplay component cannot be re-initialized.");
            #endif

            return;
        }

        // - text elements -
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

        if(textLoadingPrefab != null)
        {
            m_loadingInstances = new List<GameObject>(m_displayMapping.Count);

            foreach(Text textComponent in m_displayMapping.Keys)
            {
                RectTransform textTransform = textComponent.GetComponent<RectTransform>();
                m_loadingInstances.Add(InstantiateTextLoadingPrefab(textTransform));
            }
        }
        else
        {
            m_loadingInstances = null;
        }

        // - user display -
        if(creatorDisplay != null)
        {
            creatorDisplay.Initialize();
        }

        // - nested components -
        m_nestedPresenters = new List<IModProfilePresenter>();

        if(logoDisplay != null)
        {
            m_nestedPresenters.Add(logoDisplay);
        }
        if(mediaDisplay != null)
        {
            m_nestedPresenters.Add(mediaDisplay);
        }

        foreach(IModProfilePresenter presenter in m_nestedPresenters)
        {
            presenter.Initialize();
        }

        if(buildDisplay != null)
        {
            buildDisplay.Initialize();
        }

        m_isInitialized = true;
    }

    private GameObject InstantiateTextLoadingPrefab(RectTransform displayObjectTransform)
    {
        RectTransform parentRT = displayObjectTransform.parent as RectTransform;
        GameObject loadingGO = GameObject.Instantiate(textLoadingPrefab,
                                                      new Vector3(),
                                                      Quaternion.identity,
                                                      parentRT);

        RectTransform loadingRT = loadingGO.transform as RectTransform;
        loadingRT.anchorMin = displayObjectTransform.anchorMin;
        loadingRT.anchorMax = displayObjectTransform.anchorMax;
        loadingRT.offsetMin = displayObjectTransform.offsetMin;
        loadingRT.offsetMax = displayObjectTransform.offsetMax;

        return loadingGO;
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayProfile(ModProfile profile)
    {
        Debug.Assert(profile != null);

        m_modId = profile.id;

        foreach(var kvp in m_displayMapping)
        {
            kvp.Key.text = kvp.Value(profile);
            kvp.Key.gameObject.SetActive(true);
        }

        if(creatorDisplay != null)
        {
            creatorDisplay.DisplayProfile(profile.submittedBy);
        }

        if(m_loadingInstances != null)
        {
            foreach(GameObject loadingInstance in m_loadingInstances)
            {
                loadingInstance.SetActive(false);
            }
        }

        foreach(IModProfilePresenter presenter in m_nestedPresenters)
        {
            presenter.DisplayProfile(profile);
        }

        if(buildDisplay != null)
        {
            buildDisplay.DisplayModfile(profile.activeBuild);
        }
    }

    public void DisplayLoading()
    {
        if(m_loadingInstances != null)
        {
            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.gameObject.SetActive(false);
            }
            foreach(GameObject loadingInstance in m_loadingInstances)
            {
                loadingInstance.SetActive(true);
            }
        }
        else
        {
            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.text = string.Empty;
            }
        }

        if(creatorDisplay != null)
        {
            creatorDisplay.DisplayLoading();
        }

        foreach(IModProfilePresenter presenter in m_nestedPresenters)
        {
            presenter.DisplayLoading();
        }

        if(buildDisplay != null)
        {
            buildDisplay.DisplayLoading();
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
