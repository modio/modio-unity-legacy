using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

// TODO(@jackson): Handle guest accounts
// TODO(@jackson): Handle errors
// TODO(@jackson): Assert all required object on Initialize()
public class InspectorView : MonoBehaviour
{
    public const float YOUTUBE_THUMB_RATIO = 4f/3f;
    public const float IMAGE_THUMB_RATIO = 16f/9f;

    // ---------[ FIELDS ]---------
    [Serializable]
    public struct InspectorHelper_ProfileElements
    {
        public Text name;
        public Text dateAdded;
        public Text dateUpdated;
        public Text dateLive;
        public Text summary;
        public Text description_HTML;
        public Text description_text;
        public Text tags;
        public Text latestBuildName;
        public Text latestVersion;
        public Text downloadSize;
        // TODO(@jackson): Create layouting classes
        public RectTransform versionHistoryContainer;
    }

    [Serializable]
    public struct InspectorHelper_CreatorElements
    {
        public Text username;
        public RectTransform avatarContainer;
        public Text lastOnline;
    }

    [Serializable]
    public struct InspectorHelper_StatisticsElements
    {
        public Text popularityRankPosition;
        public Text popularityRankModCount;
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

    // ---[ UI ]---
    [Header("Settings")]
    public GameObject defaultAvatarPrefab; // TODO(@jackson): Obsolete
    public GameObject mediaLoadingPrefab;
    public GameObject youTubeOverlayPrefab;
    public GameObject versionHistoryItemPrefab;
    public UserAvatarSize avatarSize;
    public LogoSize logoSize;
    public ModGalleryImageSize galleryImageSize;
    public bool ifDescriptionMissingLoadSummary;

    [Header("UI Components")]
    public InspectorHelper_ProfileElements profileElements;
    public InspectorHelper_CreatorElements creatorElements;
    public InspectorHelper_StatisticsElements statisticsElements;
    public ModMediaCollectionDisplay mediaCollectionDisplay;
    public ScrollRect scrollView;
    public Button subscribeButton;
    public Button unsubscribeButton;
    public Button previousModButton;
    public Button nextModButton;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public ModProfile profile;
    public ModStatistics statistics;
    public bool isModSubscribed;
    public GameObject creatorAvatarPlaceholder;
    public Image creatorAvatar;

    // ---[ TEMP DATA ]---
    [Header("Temp Data")]
    public float mediaElementHeight;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        if(creatorElements.avatarContainer != null)
        {
            if(creatorAvatarPlaceholder == null || creatorAvatar == null)
            {
                Debug.Assert(defaultAvatarPrefab != null);

                foreach(Transform t in creatorElements.avatarContainer)
                {
                    GameObject.Destroy(t.gameObject);
                }

                creatorAvatarPlaceholder = UnityEngine.Object.Instantiate(defaultAvatarPrefab,
                                                                          creatorElements.avatarContainer) as GameObject;

                RectTransform avatarTransform = creatorAvatarPlaceholder.GetComponent<RectTransform>();
                avatarTransform.anchorMin = new Vector2(0f, 0f);
                avatarTransform.anchorMax = new Vector2(1f, 1f);
                avatarTransform.offsetMin = Vector2.zero;
                avatarTransform.offsetMax = Vector2.zero;

                RectTransform ca_transform = (new GameObject("Creator Avatar")).AddComponent<RectTransform>();
                ca_transform.SetParent(creatorElements.avatarContainer);
                ca_transform.anchorMin = new Vector2(0f, 0f);
                ca_transform.anchorMax = new Vector2(1f, 1f);
                ca_transform.offsetMin = Vector2.zero;
                ca_transform.offsetMax = Vector2.zero;

                creatorAvatar = ca_transform.gameObject.AddComponent<Image>();

                creatorAvatarPlaceholder.gameObject.SetActive(true);
                creatorAvatar.gameObject.SetActive(false);
            }
        }
    }

    // ---------[ UPDATE VIEW ]---------
    public void UpdateProfileUIComponents()
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        Debug.Assert(creatorElements.avatarContainer == null || creatorAvatarPlaceholder != null,
                     "[mod.io] InspectorView.UpdateProfileUIComponents() cannot be called until after InspectorView.InitializeLayout() has been called.");
        Debug.Assert(this.profile != null,
                     "[mod.io] Assign the mod profile before updating the profile UI components.");

        // - text elements -
        if(profileElements.name != null)
        {
            profileElements.name.text = profile.name;
        }
        if(profileElements.dateAdded != null)
        {
            profileElements.dateAdded.text = ServerTimeStamp.ToLocalDateTime(profile.dateAdded).ToString();
        }
        if(profileElements.dateUpdated != null)
        {
            profileElements.dateUpdated.text = ServerTimeStamp.ToLocalDateTime(profile.dateUpdated).ToString();
        }
        if(profileElements.dateLive != null)
        {
            profileElements.dateLive.text = ServerTimeStamp.ToLocalDateTime(profile.dateLive).ToString();
        }
        if(profileElements.summary != null)
        {
            profileElements.summary.text = profile.summary;
        }
        if(profileElements.description_HTML != null)
        {
            profileElements.description_HTML.text = (String.IsNullOrEmpty(profile.description_HTML) && ifDescriptionMissingLoadSummary
                                                     ? profile.summary
                                                     : profile.description_HTML);
        }
        if(profileElements.description_text != null)
        {
            profileElements.description_text.text = (String.IsNullOrEmpty(profile.description_text) && ifDescriptionMissingLoadSummary
                                                     ? profile.summary
                                                     : profile.description_text);
        }

        // - tags -
        if(profileElements.tags != null)
        {
            StringBuilder tagsString = new StringBuilder();

            foreach(string tagName in profile.tagNames)
            {
                tagsString.Append(tagName + ", ");
            }

            if(tagsString.Length > 0)
            {
                tagsString.Length -= 2; // remove final ", "
            }

            profileElements.tags.text = tagsString.ToString();
        }

        // - media -
        if(mediaCollectionDisplay != null)
        {
            mediaCollectionDisplay.modId = profile.id;
            mediaCollectionDisplay.mediaCollection = profile.media;
            mediaCollectionDisplay.UpdateDisplay();
        }

        // - modfile -
        if(profileElements.latestBuildName != null)
        {
            profileElements.latestBuildName.text = profile.activeBuild.fileName;
        }
        if(profileElements.latestVersion != null)
        {
            profileElements.latestVersion.text = profile.activeBuild.version;
        }
        if(profileElements.downloadSize != null)
        {
            profileElements.downloadSize.text = ModBrowser.ByteCountToDisplayString(profile.activeBuild.fileSize);
        }

        // - creator -
        if(creatorElements.username != null)
        {
            creatorElements.username.text = profile.submittedBy.username;
        }
        if(creatorElements.avatarContainer != null)
        {
            creatorAvatarPlaceholder.SetActive(true);
            creatorAvatar.gameObject.SetActive(false);
            // TODO(@jackson): Error handling?
            ModManager.GetUserAvatar(profile.submittedBy, avatarSize,
                                     ApplyCreatorAvatar, null);
        }
        if(creatorElements.lastOnline != null)
        {
            creatorElements.lastOnline.text = ServerTimeStamp.ToLocalDateTime(profile.submittedBy.lastOnline).ToString();
        }

        // - version history -
        if(profileElements.versionHistoryContainer != null)
        {
            foreach(Transform t in profileElements.versionHistoryContainer)
            {
                GameObject.Destroy(t.gameObject);
            }

            RequestFilter modfileFilter = new RequestFilter();
            modfileFilter.sortFieldName = ModIO.API.GetAllModfilesFilterFields.dateAdded;
            modfileFilter.isSortAscending = false;

            // TODO(@jackson): onError
            APIClient.GetAllModfiles(profile.id,
                                     modfileFilter,
                                     new APIPaginationParameters(){ limit = 20 },
                                     (r) => ApplyVersionHistory(profile.id, r.items),
                                     null);
        }
    }

    public void UpdateStatisticsUIComponents()
    {
        bool isLoading = (statistics == null);
        string displayText = (isLoading ? "..." : string.Empty);

        if(statisticsElements.popularityRankPosition != null)
        {
            if(!isLoading)
            {
                displayText = statistics.popularityRankPosition.ToString();
            }

            statisticsElements.popularityRankPosition.text = displayText;
        }
        if(statisticsElements.popularityRankModCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.popularityRankModCount);
            }

            statisticsElements.popularityRankModCount.text = displayText;
        }
        if(statisticsElements.downloadCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.downloadCount);
            }

            statisticsElements.downloadCount.text = displayText;
        }
        if(statisticsElements.subscriberCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.subscriberCount);
            }

            statisticsElements.subscriberCount.text = displayText;
        }
        if(statisticsElements.ratingsTotalCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.ratingsTotalCount);
            }
            statisticsElements.ratingsTotalCount.text = displayText;
        }
        if(statisticsElements.ratingsPositiveCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.ratingsPositiveCount);
            }
            statisticsElements.ratingsPositiveCount.text = displayText;
        }
        if(statisticsElements.ratingsNegativeCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.ratingsNegativeCount);
            }
            statisticsElements.ratingsNegativeCount.text = displayText;
        }
        if(statisticsElements.ratingsPositivePercentage != null)
        {
            if(!isLoading)
            {
                displayText = ((float)statistics.ratingsPositiveCount / (float)statistics.ratingsTotalCount).ToString("0.0") + "%";
            }
            statisticsElements.ratingsPositivePercentage.text = displayText;
        }
        if(statisticsElements.ratingsNegativePercentage != null)
        {
            if(!isLoading)
            {
                displayText = ((float)statistics.ratingsNegativeCount / (float)statistics.ratingsTotalCount).ToString("0.0") + "%";
            }
            statisticsElements.ratingsNegativePercentage.text = displayText;
        }
        if(statisticsElements.ratingsWeightedAggregate != null)
        {
            if(!isLoading)
            {
                displayText = (statistics.ratingsWeightedAggregate * 100f).ToString("0.0") + "%";
            }
            statisticsElements.ratingsWeightedAggregate.text = displayText;
        }
        if(statisticsElements.ratingsDisplayText != null)
        {
            if(!isLoading)
            {
                displayText = statistics.ratingsDisplayText;
            }
            statisticsElements.ratingsDisplayText.text = displayText;
        }
    }

    public void UpdateIsSubscribedDisplay()
    {
        if(subscribeButton != null)
        {
            subscribeButton.gameObject.SetActive(!isModSubscribed);
        }
        if(unsubscribeButton != null)
        {
            unsubscribeButton.gameObject.SetActive(isModSubscribed);
        }
    }

    // ---------[ UI ELEMENT CREATION ]---------
    public void ApplyCreatorAvatar(Texture2D texture)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        Debug.Assert(creatorAvatar != null);
        Debug.Assert(creatorAvatarPlaceholder != null);

        if(texture != null)
        {
            if(creatorAvatar.sprite != null
               && creatorAvatar.sprite.texture != null)
            {
                UnityEngine.Object.Destroy(creatorAvatar.sprite.texture);
            }

            creatorAvatar.sprite = ModBrowser.CreateSpriteFromTexture(texture);

            creatorAvatar.gameObject.SetActive(true);
            creatorAvatarPlaceholder.SetActive(false);
        }
        else
        {
            creatorAvatarPlaceholder.SetActive(true);
            creatorAvatar.gameObject.SetActive(false);
        }
    }

    private void ApplyYouTubeOverlay(Image image, string youTubeId)
    {
        // GameObject overlay_go = GameObject.Instantiate(youTubeOverlayPrefab, image.transform) as GameObject;
        // overlay_go.GetComponent<YouTubeLinker>().youTubeVideoId = youTubeId;

        // RectTransform overlayTransform = overlay_go.GetComponent<RectTransform>();
        // overlayTransform.anchorMin = new Vector2(0f, 0f);
        // overlayTransform.anchorMax = new Vector2(1f, 1f);
        // overlayTransform.offsetMin = Vector2.zero;
        // overlayTransform.offsetMax = Vector2.zero;
    }

    // private Image CreateMediaGalleryElement(float width, float height)
    // {
    //     GameObject newElement = new GameObject("Media Gallery Item");

    //     RectTransform elementTransform = newElement.AddComponent<RectTransform>();
    //     elementTransform.SetParent(profileElements.mediaGalleryContainer);
    //     elementTransform.anchorMin = new Vector2(0f, 0.5f);
    //     elementTransform.anchorMax = new Vector2(0f, 0.5f);
    //     elementTransform.pivot = new Vector2(0f, 0.5f);
    //     elementTransform.sizeDelta = new Vector2(width, height);

    //     GameObject placeholder_go = UnityEngine.Object.Instantiate(mediaLoadingPrefab, elementTransform) as GameObject;
    //     RectTransform placeholderTransform = placeholder_go.GetComponent<RectTransform>();
    //     placeholderTransform.anchorMin = new Vector2(0f, 0f);
    //     placeholderTransform.anchorMax = new Vector2(1f, 1f);
    //     placeholderTransform.sizeDelta = new Vector2(0f, 0f);

    //     Image retVal = newElement.AddComponent<Image>();
    //     return retVal;
    // }

    private void ApplyMediaGalleryTexture(Image image, Texture2D texture)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(image != null)
        {
            image.sprite = ModBrowser.CreateSpriteFromTexture(texture);
            image.enabled = true;

            GameObject.Destroy(image.transform.GetChild(0).gameObject);
        }
    }

    private void ApplyVersionHistory(int modId, IEnumerable<Modfile> modfiles)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        if(!this.gameObject.activeInHierarchy
           || profile.id != modId)
        {
            // inspector has closed/changed mods since call was made
            return;
        }

        foreach(Modfile modfile in modfiles)
        {
            GameObject go = GameObject.Instantiate(versionHistoryItemPrefab, profileElements.versionHistoryContainer) as GameObject;
            go.name = "Mod Version: " + modfile.version;

            var entry = go.GetComponent<InspectorView_VersionEntry>();
            entry.modfile = modfile;
            entry.UpdateUIComponents();
        }
    }
}
