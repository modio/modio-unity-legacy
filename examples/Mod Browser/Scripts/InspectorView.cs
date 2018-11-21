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
    public GameObject versionHistoryItemPrefab;

    [Header("UI Components")]
    public ModProfileDisplay profileDisplay;
    public ModMediaElementDisplay selectedMediaPreview;
    public RectTransform versionHistoryContainer;
    public InspectorHelper_StatisticsElements statisticsElements;
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
        if(profileDisplay != null)
        {
            profileDisplay.Initialize();
        }

        if(selectedMediaPreview != null)
        {
            selectedMediaPreview.Initialize();
            selectedMediaPreview.youTubeThumbClicked += (c, mId, ytId) => ModBrowser.OpenYouTubeVideoURL(ytId);
            // selectedMediaPreview.logoClicked += (c, mId) => Debug.Log("Clicked Logo");
            // selectedMediaPreview.galleryImageClicked += (c, mId, iFN) => Debug.Log("Clicked Image: " + iFN);
        }
    }

    // ---------[ UPDATE VIEW ]---------
    public void UpdateProfileDisplay()
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        Debug.Assert(this.profile != null,
                     "[mod.io] Assign the mod profile before updating the profile UI components.");

        if(profileDisplay != null)
        {
            profileDisplay.DisplayProfile(profile);
        }

        if(selectedMediaPreview != null)
        {
            selectedMediaPreview.DisplayModLogo(profile.id, profile.logoLocator);
        }

        // - version history -
        if(versionHistoryContainer != null)
        {
            foreach(Transform t in versionHistoryContainer)
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
            GameObject go = GameObject.Instantiate(versionHistoryItemPrefab, versionHistoryContainer) as GameObject;
            go.name = "Mod Version: " + modfile.version;

            var entry = go.GetComponent<InspectorView_VersionEntry>();
            entry.modfile = modfile;
            entry.UpdateUIComponents();
        }
    }
}
