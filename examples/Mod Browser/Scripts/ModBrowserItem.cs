using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

// TODO(@jackson): Add "Display Loading Profile" functionality
public class ModBrowserItem : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Serializable]
    public struct InspectorHelper_ProfileElements
    {
        public Image logo;
        public Text name;
        public Text creatorUsername;
        public Image creatorAvatar;
        public Text dateAdded;
        public Text dateUpdated;
        public Text dateLive;
        public Text summary;
        public LayoutGroup tagContainer;

        public Text modfileDateAdded;
        public Text modfileSize;
        public Text modfileVersion;
    }

    [Serializable]
    public struct InspectorHelper_StatisticsElements
    {
        public Text popularityRankPosition;
        public Text downloadCount;
        public Text subscriberCount;
        public Text ratingsTotalCount;
        public Text ratingsPositiveCount;
        public Text ratingsNegativeCount;
        public Text ratingsPositivePercentage;
        public Text ratingsNegativePercentage;
        public Text ratingsWeightedAggregate;
        public Text ratingsDisplayText;
    }

    // ---[ EVENTS ]---
    public event Action<ModBrowserItem> inspectRequested;
    public event Action<ModBrowserItem> subscribeRequested;
    public event Action<ModBrowserItem> unsubscribeRequested;
    public event Action<ModBrowserItem> toggleModEnabledRequested;

    // ---[ UI ]---
    [Header("Settings")]
    [Range(1.0f, 2.0f)]
    public float maximumScaleFactor = 1f;
    public GameObject textLoadingPrefab;
    public LogoSize logoSize;
    public GameObject logoLoadingPrefab;
    public UserAvatarSize avatarSize;
    public GameObject avatarLoadingPrefab;
    public GameObject tagBadgePrefab;

    [Header("UI Components")]
    public InspectorHelper_ProfileElements profileDisplay;
    public InspectorHelper_StatisticsElements statisticsDisplay;
    public Button subscribeButton;
    public Button unsubscribeButton;
    public Button enableModButton;
    public Button disableModButton;

    // TODO(@jackson): You know what to do...
    public Text downloadProgressPercentageText;

    [Header("Display Data")]
    public ModProfile profile = null;
    public ModStatistics statistics = null;
    public bool isSubscribed = false;
    public bool isModEnabled = false;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public bool isInitialized = false;
    public int index = -1;

    private List<Action> profileUIDelegates = null;
    private List<Action> profileLoadingUIDelegates = null;
    private List<Action> statisticsUIDelegates = null;
    private List<Action> statisticsLoadingUIDelegates = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        if(isInitialized)
        {
            #if DEBUG
            Debug.LogWarning("[mod.io] Once initialized, a ModBrowserItem cannot be re-initialized.");
            #endif

            return;
        }

        InitializeProfileDisplay();
        InitializeStatisticsDisplay();

        // TODO(@jackson): Move to button Prefab
        if(subscribeButton != null)
        {
            subscribeButton.onClick.AddListener(SubscribeClicked);
        }

        if(unsubscribeButton != null)
        {
            unsubscribeButton.onClick.AddListener(UnsubscribeClicked);
        }

        if(enableModButton != null)
        {
            enableModButton.onClick.AddListener(ModEnabledToggled);
        }

        if(disableModButton != null)
        {
            disableModButton.onClick.AddListener(ModEnabledToggled);
        }
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

    private void InitializeProfileDisplay()
    {
        Debug.Assert(profileDisplay.logo == null || logoLoadingPrefab != null,
                     "[mod.io] If the profile logo is to be displayed, this display requires a"
                     + " logoLoadingPrefab to present while loading.");

        profileUIDelegates = new List<Action>();
        profileLoadingUIDelegates = new List<Action>();

        // - logo -
        if(profileDisplay.logo != null)
        {
            RectTransform displayRT = profileDisplay.logo.transform as RectTransform;
            RectTransform parentRT = displayRT.parent as RectTransform;
            GameObject loadingGO = GameObject.Instantiate(logoLoadingPrefab,
                                                          new Vector3(),
                                                          Quaternion.identity,
                                                          parentRT);

            RectTransform loadingRT = loadingGO.transform as RectTransform;
            loadingRT.anchorMin = displayRT.anchorMin;
            loadingRT.anchorMax = displayRT.anchorMax;
            loadingRT.offsetMin = displayRT.offsetMin;
            loadingRT.offsetMax = displayRT.offsetMax;

            profileUIDelegates.Add(() =>
            {
                Action<Texture2D> onGetTexture = (t) =>
                {
                    #if UNITY_EDITOR
                    if(!Application.isPlaying) { return; }
                    #endif

                    Debug.Assert(t != null);

                    profileDisplay.logo.sprite = ModBrowser.CreateSpriteFromTexture(t);
                    profileDisplay.logo.gameObject.SetActive(true);
                    loadingGO.gameObject.SetActive(false);
                };

                loadingGO.SetActive(true);
                profileDisplay.logo.gameObject.SetActive(false);

                // TODO(@jackson): onError
                ModManager.GetModLogo(profile, logoSize, onGetTexture, null);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.logo.gameObject.SetActive(false);
            });
        }

        // - avatar -
        if(profileDisplay.creatorAvatar != null)
        {
            RectTransform displayRT = profileDisplay.creatorAvatar.transform as RectTransform;
            RectTransform parentRT = displayRT.parent as RectTransform;
            GameObject loadingGO = GameObject.Instantiate(avatarLoadingPrefab,
                                                          new Vector3(),
                                                          Quaternion.identity,
                                                          parentRT);

            RectTransform loadingRT = loadingGO.transform as RectTransform;
            loadingRT.anchorMin = displayRT.anchorMin;
            loadingRT.anchorMax = displayRT.anchorMax;
            loadingRT.offsetMin = displayRT.offsetMin;
            loadingRT.offsetMax = displayRT.offsetMax;

            profileUIDelegates.Add(() =>
            {
                Action<Texture2D> onGetTexture = (t) =>
                {
                    #if UNITY_EDITOR
                    if(!Application.isPlaying) { return; }
                    #endif

                    Debug.Assert(t != null);

                    if(profileDisplay.creatorAvatar.sprite != null)
                    {
                        if(profileDisplay.creatorAvatar.sprite.texture != null)
                        {
                            UnityEngine.Object.Destroy(profileDisplay.creatorAvatar.sprite.texture);
                        }
                        UnityEngine.Object.Destroy(profileDisplay.creatorAvatar.sprite);
                    }

                    profileDisplay.creatorAvatar.sprite = ModBrowser.CreateSpriteFromTexture(t);

                    profileDisplay.creatorAvatar.gameObject.SetActive(true);
                    loadingGO.gameObject.SetActive(false);
                };

                loadingGO.SetActive(true);
                profileDisplay.creatorAvatar.gameObject.SetActive(false);

                // TODO(@jackson): onError
                ModManager.GetUserAvatar(profile.submittedBy, avatarSize, onGetTexture, null);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.creatorAvatar.gameObject.SetActive(false);
            });
        }

        // - tags -
        if(profileDisplay.tagContainer != null)
        {
            Func<GameObject, Text> getTextComponent;
            if(tagBadgePrefab.GetComponent<Text>() != null)
            {
                getTextComponent = (go) => go.GetComponent<Text>();
            }
            else
            {
                getTextComponent = (go) => go.GetComponentInChildren<Text>();
            }

            profileUIDelegates.Add(() =>
            {
                foreach(Transform t in profileDisplay.tagContainer.transform)
                {
                    GameObject.Destroy(t.gameObject);
                }

                foreach(string tagName in profile.tagNames)
                {
                    GameObject tag_go = GameObject.Instantiate(tagBadgePrefab, profileDisplay.tagContainer.transform) as GameObject;
                    tag_go.name = "Tag: " + tagName;
                    getTextComponent(tag_go).text = tagName;
                }
            });

            profileLoadingUIDelegates.Add(() =>
            {
                foreach(Transform t in profileDisplay.tagContainer.transform)
                {
                    GameObject.Destroy(t.gameObject);
                }
            });
        }

        // - text elements -
        if(profileDisplay.name != null)
        {
            RectTransform displayRT = profileDisplay.name.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.name.text = profile.name;

                profileDisplay.name.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.name.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.creatorUsername != null)
        {
            RectTransform displayRT = profileDisplay.creatorUsername.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.creatorUsername.text = profile.submittedBy.username;

                profileDisplay.creatorUsername.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.creatorUsername.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.dateAdded != null)
        {
            RectTransform displayRT = profileDisplay.dateAdded.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.dateAdded.text = ServerTimeStamp.ToLocalDateTime(profile.dateAdded).ToString();

                profileDisplay.dateAdded.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.dateAdded.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.dateUpdated != null)
        {
            RectTransform displayRT = profileDisplay.dateUpdated.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.dateUpdated.text = ServerTimeStamp.ToLocalDateTime(profile.dateUpdated).ToString();

                profileDisplay.dateUpdated.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.dateUpdated.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.dateLive != null)
        {
            RectTransform displayRT = profileDisplay.dateLive.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.dateLive.text = ServerTimeStamp.ToLocalDateTime(profile.dateLive).ToString();

                profileDisplay.dateLive.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.dateLive.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.summary != null)
        {
            RectTransform displayRT = profileDisplay.summary.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.summary.text = profile.summary;

                profileDisplay.summary.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.summary.gameObject.SetActive(false);
            });
        }

        // - modfile elements -
        if(profileDisplay.modfileDateAdded != null)
        {
            RectTransform displayRT = profileDisplay.modfileDateAdded.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.modfileDateAdded.text = ServerTimeStamp.ToLocalDateTime(profile.activeBuild.dateAdded).ToString();

                profileDisplay.modfileDateAdded.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.modfileDateAdded.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.modfileSize != null)
        {
            RectTransform displayRT = profileDisplay.modfileSize.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.modfileSize.text = ModBrowser.ByteCountToDisplayString(profile.activeBuild.fileSize);

                profileDisplay.modfileSize.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.modfileSize.gameObject.SetActive(false);
            });
        }
        if(profileDisplay.modfileVersion != null)
        {
            RectTransform displayRT = profileDisplay.modfileVersion.transform as RectTransform;
            GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

            profileUIDelegates.Add(() =>
            {
                profileDisplay.modfileVersion.text = profile.activeBuild.version;

                profileDisplay.modfileVersion.gameObject.SetActive(true);
                loadingGO.SetActive(false);
            });

            profileLoadingUIDelegates.Add(() =>
            {
                loadingGO.SetActive(true);
                profileDisplay.modfileVersion.gameObject.SetActive(false);
            });
        }
    }

    private void InitializeStatisticsDisplay()
    // {
    //     statisticsUIDelegates = new List<Action>();
    //     statisticsLoadingUIDelegates = new List<Action>();

    //     if(statisticsDisplay.popularityRankPosition != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.popularityRankPosition.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.popularityRankPosition.text = statistics.popularityRankPosition.ToString();

    //             statisticsDisplay.popularityRankPosition.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.popularityRankPosition.gameObject.SetActive(false);
    //         });
    //     }
    //     if(statisticsDisplay.downloadCount != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.downloadCount.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.downloadCount.text = ModBrowser.ValueToDisplayString(statistics.downloadCount);

    //             statisticsDisplay.downloadCount.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.downloadCount.gameObject.SetActive(false);
    //         });
    //     }
    //     if(statisticsDisplay.subscriberCount != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.subscriberCount.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.subscriberCount.text = ModBrowser.ValueToDisplayString(statistics.subscriberCount);

    //             statisticsDisplay.subscriberCount.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.subscriberCount.gameObject.SetActive(false);
    //         });

    //     }
    //     if(statisticsDisplay.ratingsTotalCount != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsTotalCount.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.ratingsTotalCount.text = ModBrowser.ValueToDisplayString(statistics.ratingsTotalCount);

    //             statisticsDisplay.ratingsTotalCount.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsTotalCount.gameObject.SetActive(false);
    //         });

    //     }
    //     if(statisticsDisplay.ratingsPositiveCount != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsPositiveCount.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.ratingsPositiveCount.text = ModBrowser.ValueToDisplayString(statistics.ratingsPositiveCount);

    //             statisticsDisplay.ratingsPositiveCount.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsPositiveCount.gameObject.SetActive(false);
    //         });

    //     }
    //     if(statisticsDisplay.ratingsNegativeCount != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsNegativeCount.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.ratingsNegativeCount.text = ModBrowser.ValueToDisplayString(statistics.ratingsNegativeCount);

    //             statisticsDisplay.ratingsNegativeCount.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsNegativeCount.gameObject.SetActive(false);
    //         });
    //     }
    //     if(statisticsDisplay.ratingsPositivePercentage != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsPositivePercentage.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             string displayText = string.Empty;
    //             if(statistics.ratingsTotalCount > 0)
    //             {
    //                 float value = 100f * (float)statistics.ratingsPositiveCount / (float)statistics.ratingsTotalCount;
    //                 displayText = value.ToString("0") + "%";
    //             }
    //             else
    //             {
    //                 displayText = "~%";
    //             }
    //             statisticsDisplay.ratingsPositivePercentage.text = displayText;

    //             statisticsDisplay.ratingsPositivePercentage.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsPositivePercentage.gameObject.SetActive(false);
    //         });
    //     }
    //     if(statisticsDisplay.ratingsNegativePercentage != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsNegativePercentage.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             string displayText = string.Empty;
    //             if(statistics.ratingsTotalCount > 0)
    //             {
    //                 float value = 100f * (float)statistics.ratingsNegativeCount / (float)statistics.ratingsTotalCount;
    //                 displayText = value.ToString("0") + "%";
    //             }
    //             else
    //             {
    //                 displayText = "~%";
    //             }
    //             statisticsDisplay.ratingsNegativePercentage.text = displayText;

    //             statisticsDisplay.ratingsNegativePercentage.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsNegativePercentage.gameObject.SetActive(false);
    //         });
    //     }
    //     if(statisticsDisplay.ratingsWeightedAggregate != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsWeightedAggregate.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.ratingsWeightedAggregate.text = (100f * statistics.ratingsWeightedAggregate).ToString("0") + "%";

    //             statisticsDisplay.ratingsWeightedAggregate.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsWeightedAggregate.gameObject.SetActive(false);
    //         });
    //     }
    //     if(statisticsDisplay.ratingsDisplayText != null)
    //     {
    //         RectTransform displayRT = statisticsDisplay.ratingsDisplayText.transform as RectTransform;
    //         GameObject loadingGO = InstantiateTextLoadingPrefab(displayRT);

    //         statisticsUIDelegates.Add(() =>
    //         {
    //             statisticsDisplay.ratingsDisplayText.text = statistics.ratingsDisplayText;

    //             statisticsDisplay.ratingsDisplayText.gameObject.SetActive(true);
    //             loadingGO.SetActive(false);
    //         });

    //         statisticsLoadingUIDelegates.Add(() =>
    //         {
    //             loadingGO.SetActive(true);
    //             statisticsDisplay.ratingsDisplayText.gameObject.SetActive(false);
    //         });
    //     }
    // }
    {}

    // ---------[ UI UPDATES ]---------
    public void UpdateProfileDisplay()
    {
        if(downloadProgressPercentageText != null)
        {
            downloadProgressPercentageText.gameObject.SetActive(false);
        }

        if(profile != null)
        {
            foreach(Action updateDelegate in profileUIDelegates)
            {
                updateDelegate();
            }

            // TODO(@jackson): yep
            // UpdateDownloadProgressDisplay();
        }
        else
        {
            foreach(Action updateDelegate in profileLoadingUIDelegates)
            {
                updateDelegate();
            }
        }
    }

    public void UpdateStatisticsDisplay()
    {
        // if(statistics != null)
        // {
        //     foreach(Action updateDelegate in statisticsUIDelegates)
        //     {
        //         updateDelegate();
        //     }
        // }
        // else
        // {
        //     foreach(Action updateDelegate in statisticsLoadingUIDelegates)
        //     {
        //         updateDelegate();
        //     }
        // }
    }

    public void UpdateIsSubscribedDisplay()
    {
        if(subscribeButton != null)
        {
            if(profile == null)
            {
                subscribeButton.interactable = false;
                subscribeButton.gameObject.SetActive(true);
            }
            else
            {
                subscribeButton.interactable = true;
                subscribeButton.gameObject.SetActive(!isSubscribed);
            }
        }
        if(unsubscribeButton != null)
        {
            if(profile == null)
            {
                unsubscribeButton.gameObject.SetActive(false);
            }
            else
            {
                unsubscribeButton.gameObject.SetActive(isSubscribed);
            }
        }

        if(profile != null && isSubscribed)
        {
            // TODO(@jackson): yep
            // UpdateDownloadProgressDisplay();
        }
    }

    public void UpdateIsModEnabledDisplay()
    {
        if(enableModButton != null)
        {
            if(profile == null)
            {
                enableModButton.interactable = false;
                enableModButton.gameObject.SetActive(true);
            }
            else
            {
                enableModButton.interactable = true;
                enableModButton.gameObject.SetActive(!isModEnabled);
            }
        }
        if(unsubscribeButton != null)
        {
            if(profile == null)
            {
                disableModButton.gameObject.SetActive(false);
            }
            else
            {
                disableModButton.gameObject.SetActive(isModEnabled);
            }
        }
    }

    public void UpdateDownloadProgressDisplay()
    {
        if(downloadProgressPercentageText != null)
        {
            ModBinaryRequest request = ModManager.GetDownloadInProgress(profile.activeBuild.id);
            if(request == null
               || request.isDone)
            {
                downloadProgressPercentageText.gameObject.SetActive(false);
            }
            else
            {
                downloadProgressPercentageText.gameObject.SetActive(true);

                StartCoroutine(UpdateDownloadProgressText(request));
            }
        }
    }

    public IEnumerator UpdateDownloadProgressText(ModBinaryRequest request)
    {
        downloadProgressPercentageText.text = "Initializing";

        while(!request.isDone)
        {
            if(request.webRequest != null)
            {
                float percentComplete = request.webRequest.downloadProgress;
                string displayString = ("Downloading "
                                        + (percentComplete * 100f).ToString("0.0")
                                        + "%");
                downloadProgressPercentageText.text = displayString;
            }

            downloadProgressPercentageText.text = "Downloading 100%";

            yield return new WaitForSeconds(4f);
        }

        downloadProgressPercentageText.gameObject.SetActive(false);
    }

    // private IEnumerator UpdateDownloadProgressBar(ModBinaryRequest request)
    // {
    //     itemInspector_downloadProgressBar.sizeDelta = new Vector2(0f, 0f);
    //     itemInspector_downloadProgressText.text = "Initializing";

    //     RectTransform progressBarParent = itemInspector_downloadProgressBar.parent.GetComponent<RectTransform>();
    //     while(!request.isDone)
    //     {
    //         if(request.webRequest != null)
    //         {
    //             float percentComplete = request.webRequest.downloadProgress;

    //             float barWidth = percentComplete * progressBarParent.rect.width;
    //             itemInspector_downloadProgressBar.sizeDelta = new Vector2(barWidth, 0f);

    //             itemInspector_downloadProgressText.text = (percentComplete * 100f).ToString("0.0") + "%";
    //         }

    //         yield return null;
    //     }

    //     itemInspector_buttonContainer.gameObject.SetActive(true);
    //     itemInspector_downloadContainer.gameObject.SetActive(false);
    // }

    // ---------[ EVENTS ]---------
    public void InspectClicked()
    {
        if(inspectRequested != null)
        {
            inspectRequested(this);
        }
    }
    public void SubscribeClicked()
    {
        if(subscribeRequested != null)
        {
            subscribeRequested(this);
        }
    }
    public void UnsubscribeClicked()
    {
        if(unsubscribeRequested != null)
        {
            unsubscribeRequested(this);
        }
    }
    public void ModEnabledToggled()
    {
        if(toggleModEnabledRequested != null)
        {
            toggleModEnabledRequested(this);
        }
    }
}
