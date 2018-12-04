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
    // ---------[ FIELDS ]---------
    [Header("Settings")]
    public GameObject versionHistoryItemPrefab;
    public string missingVersionChangelogText;

    [Header("UI Components")]
    public ModProfileDisplay profileDisplay;
    public ModMediaElementDisplay selectedMediaPreview;
    public ModStatisticsDisplay statisticsDisplay;
    public RectTransform versionHistoryContainer;
    public ScrollRect scrollView;
    public Button subscribeButton;
    public Button unsubscribeButton;
    public Button previousModButton;
    public Button nextModButton;
    public Button backToDiscoverButton;
    public Button backToSubscriptionsButton;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public ModProfile profile;
    public ModStatistics statistics;
    public bool isModSubscribed;

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

            if(profileDisplay != null
               && profileDisplay.mediaDisplay != null)
            {
                profileDisplay.mediaDisplay.logoClicked += MediaPreview_Logo;
                profileDisplay.mediaDisplay.youTubeThumbClicked += MediaPreview_YouTubeThumb;
                profileDisplay.mediaDisplay.galleryImageClicked += MediaPreview_GalleryImage;
            }
        }

        if(statisticsDisplay != null)
        {
            statisticsDisplay.Initialize();
        }

        if((versionHistoryContainer != null && versionHistoryItemPrefab == null)
           || (versionHistoryItemPrefab != null && versionHistoryContainer == null))
        {
            Debug.LogWarning("[mod.io] In order to display a version history both the "
                             + "versionHistoryItemPrefab and versionHistoryContainer variables must "
                             + "be set for the InspectorView.", this);
        }

        Debug.Assert(!(versionHistoryItemPrefab != null && versionHistoryItemPrefab.GetComponent<ModfileDisplay>() == null),
                     "[mod.io] The versionHistoryItemPrefab requires a ModfileDisplay component on the root Game Object.");
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
            selectedMediaPreview.DisplayLogo(profile.id, profile.logoLocator);
            MediaPreview_UpdateAspectRatio();
        }

        // - version history -
        if(versionHistoryContainer != null
           && versionHistoryItemPrefab != null)
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
                                     (r) => PopulateVersionHistory(profile.id, r.items),
                                     WebRequestError.LogAsWarning);
        }
    }
    public void UpdateStatisticsDisplay()
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        Debug.Assert(this.statistics != null,
                     "[mod.io] Assign the mod statistics before updating the statistics UI components.");

        if(statisticsDisplay != null)
        {
            statisticsDisplay.DisplayStatistics(statistics);
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
    private void PopulateVersionHistory(int modId, IEnumerable<Modfile> modfiles)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        // inspector has closed/changed mods since call was made
        if(profile.id != modId) { return; }

        foreach(Modfile modfile in modfiles)
        {
            GameObject go = GameObject.Instantiate(versionHistoryItemPrefab, versionHistoryContainer) as GameObject;
            go.name = "Mod Version: " + modfile.version;

            if(String.IsNullOrEmpty(modfile.changelog))
            {
                modfile.changelog = missingVersionChangelogText;
            }

            var entry = go.GetComponent<ModfileDisplay>();
            entry.Initialize();
            entry.DisplayModfile(modfile);
        }
    }

    // ---------[ CLICKS ]---------
    private void MediaPreview_Logo(ModLogoDisplay display, int modId)
    {
        selectedMediaPreview.DisplayLogoTexture(modId, display.image.mainTexture as Texture2D);
        MediaPreview_UpdateAspectRatio();

        if(display.logoSize != selectedMediaPreview.logoSize)
        {
            ModManager.GetModLogo(profile, selectedMediaPreview.logoSize,
                                  (t) =>
                                  { selectedMediaPreview.DisplayLogoTexture(modId, t); MediaPreview_UpdateAspectRatio(); },
                                  WebRequestError.LogAsWarning);
        }
    }
    private void MediaPreview_YouTubeThumb(YouTubeThumbDisplay display, int modId, string youTubeVideoId)
    {
        selectedMediaPreview.DisplayYouTubeThumbTexture(modId, youTubeVideoId,
                                                        display.image.mainTexture as Texture2D);
        MediaPreview_UpdateAspectRatio();
    }
    private void MediaPreview_GalleryImage(ModGalleryImageDisplay display, int modId, string imageFileName)
    {
        selectedMediaPreview.DisplayGalleryImageTexture(modId, imageFileName, display.image.mainTexture as Texture2D);
        MediaPreview_UpdateAspectRatio();

        if(display.imageSize != selectedMediaPreview.galleryImageSize)
        {
            ModManager.GetModGalleryImage(profile, imageFileName, selectedMediaPreview.galleryImageSize,
                                          (t) =>
                                          {
                                            selectedMediaPreview.DisplayGalleryImageTexture(modId, imageFileName, t);
                                            MediaPreview_UpdateAspectRatio();
                                          },
                                          WebRequestError.LogAsWarning);
        }
    }

    private void MediaPreview_UpdateAspectRatio()
    {
        AspectRatioFitter fitter = selectedMediaPreview.image.GetComponent<AspectRatioFitter>();
        if(fitter != null)
        {
            Texture t = selectedMediaPreview.image.mainTexture;
            fitter.aspectRatio = (float)t.width / (float)t.height;
        }
    }
}
