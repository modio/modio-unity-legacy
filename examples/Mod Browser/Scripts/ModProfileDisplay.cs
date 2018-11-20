using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModProfileDisplay : MonoBehaviour, IModProfilePresenter
{
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public GameObject       textLoadingPrefab;
    public LogoSize         logoSize;
    public GameObject       logoLoadingPrefab;
    public UserAvatarSize   avatarSize;
    public GameObject       avatarLoadingPrefab;
    public GameObject       tagBadgePrefab;
    [Tooltip("If the profile has no description, the description display element(s) can be filled with the summary instead.")]
    public bool             replaceMissingDescriptionWithSummary;

    [Header("UI Components")]
    public Text         nameDisplay;
    public Text         creatorUsernameDisplay;
    public Image        creatorAvatarDisplay;
    public Text         dateAddedDisplay;
    public Text         dateUpdatedDisplay;
    public Text         dateLiveDisplay;
    public Text         summaryDisplay;
    public Text         descriptionHTMLDisplay;
    public Text         descriptionTextDisplay;
    public ModLogoDisplay logoDisplay;
    public LayoutGroup  tagContainer;

    // TODO(@jackson): Push to modfile Display
    public Text modfileDateAddedDisplay;
    public Text modfileSizeDisplay;
    public Text modfileVersionDisplay;

    [Header("Display Data")]
    [SerializeField]
    private ModProfile m_profile = null;

    // ---[ RUNTIME DATA ]---
    private bool m_isInitialized = false;
    private delegate string GetDisplayString(ModProfile profile);
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

        // // - avatar -
        // if(creatorAvatarDisplay != null)
        // {
        //     RectTransform displayRT = creatorAvatarDisplay.transform as RectTransform;
        //     RectTransform parentRT = displayRT.parent as RectTransform;
        //     GameObject loadingGO = GameObject.Instantiate(avatarLoadingPrefab,
        //                                                   new Vector3(),
        //                                                   Quaternion.identity,
        //                                                   parentRT);

        //     RectTransform loadingRT = loadingGO.transform as RectTransform;
        //     loadingRT.anchorMin = displayRT.anchorMin;
        //     loadingRT.anchorMax = displayRT.anchorMax;
        //     loadingRT.offsetMin = displayRT.offsetMin;
        //     loadingRT.offsetMax = displayRT.offsetMax;

        //     m_profileUIDelegates.Add(() =>
        //     {
        //         Action<Texture2D> onGetTexture = (t) =>
        //         {
        //             #if UNITY_EDITOR
        //             if(!Application.isPlaying) { return; }
        //             #endif

        //             Debug.Assert(t != null);

        //             if(creatorAvatarDisplay.sprite != null)
        //             {
        //                 if(creatorAvatarDisplay.sprite.texture != null)
        //                 {
        //                     UnityEngine.Object.Destroy(creatorAvatarDisplay.sprite.texture);
        //                 }
        //                 UnityEngine.Object.Destroy(creatorAvatarDisplay.sprite);
        //             }

        //             creatorAvatarDisplay.sprite = ModBrowser.CreateSpriteFromTexture(t);

        //             creatorAvatarDisplay.gameObject.SetActive(true);
        //             loadingGO.gameObject.SetActive(false);
        //         };

        //         loadingGO.SetActive(true);
        //         creatorAvatarDisplay.gameObject.SetActive(false);

        //         // TODO(@jackson): onError
        //         ModManager.GetUserAvatar(profile.submittedBy, avatarSize, onGetTexture, null);
        //     });

        //     m_profileLoadingUIDelegates.Add(() =>
        //     {
        //         loadingGO.SetActive(true);
        //         creatorAvatarDisplay.gameObject.SetActive(false);
        //     });
        // }

        // // - tags -
        // if(tagContainer != null)
        // {
        //     Func<GameObject, Text> getTextComponent;
        //     if(tagBadgePrefab.GetComponent<Text>() != null)
        //     {
        //         getTextComponent = (go) => go.GetComponent<Text>();
        //     }
        //     else
        //     {
        //         getTextComponent = (go) => go.GetComponentInChildren<Text>();
        //     }

        //     m_profileUIDelegates.Add(() =>
        //     {
        //         foreach(Transform t in tagContainer.transform)
        //         {
        //             GameObject.Destroy(t.gameObject);
        //         }

        //         foreach(string tagName in profile.tagNames)
        //         {
        //             GameObject tag_go = GameObject.Instantiate(tagBadgePrefab, tagContainer.transform) as GameObject;
        //             tag_go.name = "Tag: " + tagName;
        //             getTextComponent(tag_go).text = tagName;
        //         }
        //     });

        //     m_profileLoadingUIDelegates.Add(() =>
        //     {
        //         foreach(Transform t in tagContainer.transform)
        //         {
        //             GameObject.Destroy(t.gameObject);
        //         }
        //     });
        // }

        // - text elements -
        m_displayMapping = new Dictionary<Text, GetDisplayString>();

        if(nameDisplay != null)
        {
            m_displayMapping.Add(nameDisplay, (p) => p.name);
        }
        if(creatorUsernameDisplay != null)
        {
            m_displayMapping.Add(creatorUsernameDisplay, (p) => p.submittedBy.username);
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

        // - nested components -
        m_nestedPresenters = new List<IModProfilePresenter>();

        if(logoDisplay != null)
        {
            m_nestedPresenters.Add(logoDisplay);
            logoDisplay.Initialize();
        }

        // // - modfile elements -
        // if(modfileDateAddedDisplay != null)
        // {
        //     RectTransform displayRT = modfileDateAddedDisplay.transform as RectTransform;
        //     GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

        //     m_profileUIDelegates.Add(() =>
        //     {
        //         modfileDateAddedDisplay.text = ServerTimeStamp.ToLocalDateTime(profile.activeBuild.dateAdded).ToString();

        //         modfileDateAddedDisplay.gameObject.SetActive(true);
        //         loadingGO.SetActive(false);
        //     });

        //     m_profileLoadingUIDelegates.Add(() =>
        //     {
        //         loadingGO.SetActive(true);
        //         modfileDateAddedDisplay.gameObject.SetActive(false);
        //     });
        // }
        // if(modfileSizeDisplay != null)
        // {
        //     RectTransform displayRT = modfileSizeDisplay.transform as RectTransform;
        //     GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

        //     m_profileUIDelegates.Add(() =>
        //     {
        //         modfileSizeDisplay.text = ModBrowser.ByteCountToDisplayString(profile.activeBuild.fileSize);

        //         modfileSizeDisplay.gameObject.SetActive(true);
        //         loadingGO.SetActive(false);
        //     });

        //     m_profileLoadingUIDelegates.Add(() =>
        //     {
        //         loadingGO.SetActive(true);
        //         modfileSizeDisplay.gameObject.SetActive(false);
        //     });
        // }
        // if(modfileVersionDisplay != null)
        // {
        //     RectTransform displayRT = modfileVersionDisplay.transform as RectTransform;
        //     GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

        //     m_profileUIDelegates.Add(() =>
        //     {
        //         modfileVersionDisplay.text = profile.activeBuild.version;

        //         modfileVersionDisplay.gameObject.SetActive(true);
        //         loadingGO.SetActive(false);
        //     });

        //     m_profileLoadingUIDelegates.Add(() =>
        //     {
        //         loadingGO.SetActive(true);
        //         modfileVersionDisplay.gameObject.SetActive(false);
        //     });
        // }

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

        m_profile = profile;

        foreach(var kvp in m_displayMapping)
        {
            kvp.Key.text = kvp.Value(m_profile);
            kvp.Key.gameObject.SetActive(true);
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

        foreach(IModProfilePresenter presenter in m_nestedPresenters)
        {
            presenter.DisplayLoading();
        }
    }
}
