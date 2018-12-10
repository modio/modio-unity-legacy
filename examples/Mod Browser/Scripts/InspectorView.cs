﻿using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

// TODO(@jackson): Handle guest accounts
// TODO(@jackson): Handle errors
// TODO(@jackson): Assert all required object on Initialize()
namespace ModIO.UI
{
    public class InspectorView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<ModProfile> subscribeRequested;
        public event Action<ModProfile> unsubscribeRequested;
        public event Action<ModProfile> enableRequested;
        public event Action<ModProfile> disableRequested;

        [Header("Settings")]
        public GameObject versionHistoryItemPrefab;
        public string missingVersionChangelogText;

        [Header("UI Components")]
        public ModProfileDisplay profileDisplay;
        public ModMediaDisplayComponent selectedMediaPreview;
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
                selectedMediaPreview.youTubeThumbnailClicked += (d) => UIUtilities.OpenYouTubeVideoURL(d.data.youTubeId);
                // selectedMediaPreview.logoClicked += (c, mId) => Debug.Log("Clicked Logo");
                // selectedMediaPreview.galleryImageClicked += (c, mId, iFN) => Debug.Log("Clicked Image: " + iFN);

                if(profileDisplay != null
                   && profileDisplay.mediaContainer != null)
                {
                    profileDisplay.mediaContainer.logoClicked += MediaPreview_Logo;
                    profileDisplay.mediaContainer.galleryImageClicked += MediaPreview_GalleryImage;
                    profileDisplay.mediaContainer.youTubeThumbClicked += MediaPreview_YouTubeThumb;
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

            Debug.Assert(!(versionHistoryItemPrefab != null && versionHistoryItemPrefab.GetComponent<ModfileDisplayComponent>() == null),
                         "[mod.io] The versionHistoryItemPrefab requires a ModfileDisplayComponent on the root Game Object.");
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
                Debug.LogWarning("categories needed");
                profileDisplay.DisplayProfile(profile, null);
            }

            if(selectedMediaPreview != null)
            {
                selectedMediaPreview.DisplayLogo(profile.id, profile.logoLocator);
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

                var entry = go.GetComponent<ModfileDisplayComponent>();
                entry.Initialize();
                entry.DisplayModfile(modfile);
            }
        }

        // ---------[ EVENTS ]---------
        public void NotifySubscribeRequested()
        {
            if(subscribeRequested != null)
            {
                subscribeRequested(this.profile);
            }
        }
        public void NotifyUnsubscribeRequested()
        {
            if(unsubscribeRequested != null)
            {
                unsubscribeRequested(this.profile);
            }
        }
        public void NotifyEnableRequested()
        {
            if(enableRequested != null)
            {
                enableRequested(this.profile);
            }
        }
        public void NotifyDisableRequested()
        {
            if(disableRequested != null)
            {
                disableRequested(this.profile);
            }
        }

        private void MediaPreview_Logo(ModLogoDisplayComponent display)
        {
            ImageDisplayData displayData = display.data;
            selectedMediaPreview.data = displayData;
            selectedMediaPreview.mediaType = ModMediaDisplayComponent.DataType.Logo;

            if(display.logoSize != selectedMediaPreview.logoSize)
            {
                ModManager.GetModLogo(profile, selectedMediaPreview.logoSize,
                                      (t) =>
                                      {
                                        if(Application.isPlaying
                                           && display.data.Equals(displayData))
                                        {
                                            displayData.texture = t;
                                            selectedMediaPreview.data = displayData;
                                        }
                                      },
                                      WebRequestError.LogAsWarning);
            }
        }
        private void MediaPreview_GalleryImage(ModGalleryImageDisplayComponent display)
        {
            ImageDisplayData displayData = display.data;
            selectedMediaPreview.data = displayData;
            selectedMediaPreview.mediaType = ModMediaDisplayComponent.DataType.GalleryImage;


            if(display.imageSize != selectedMediaPreview.galleryImageSize)
            {
                ModManager.GetModGalleryImage(profile, display.data.fileName,
                                              selectedMediaPreview.galleryImageSize,
                                              (t) =>
                                              {
                                                if(Application.isPlaying
                                                   && display.data.Equals(displayData))
                                                {
                                                    displayData.texture = t;
                                                    selectedMediaPreview.data = displayData;
                                                }
                                              },
                                              WebRequestError.LogAsWarning);
            }
        }
        private void MediaPreview_YouTubeThumb(YouTubeThumbnailDisplayComponent display)
        {
            ImageDisplayData displayData = display.data;
            selectedMediaPreview.data = displayData;
            selectedMediaPreview.mediaType = ModMediaDisplayComponent.DataType.YouTubeThumbnail;
        }
    }
}
