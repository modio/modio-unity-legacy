using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class InspectorView : MonoBehaviour, IGameProfileUpdateReceiver, IModDownloadStartedReceiver, IModEnabledReceiver, IModDisabledReceiver, IModSubscriptionsUpdateReceiver, IModRatingAddedReceiver
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public GameObject versionHistoryItemPrefab = null;
        public string missingVersionChangelogText = "<i>None recorded.</i>";

        [Header("UI Components")]
        public ModView modView;
        public ImageDisplay selectedMediaPreview;
        public RectTransform versionHistoryContainer;
        public ScrollRect scrollView;
        public GameObject loadingDisplay;

        // ---[ RUNTIME DATA ]---
        private bool m_isInitialized = false;

        private IEnumerable<ModTagCategory> m_tagCategories = new ModTagCategory[0];

        private int m_modId = ModProfile.NULL_ID;
        private IEnumerable<Modfile> m_versionHistory = new List<Modfile>(0);

        // --- ACCESSORS ---
        public int modId
        {
            get
            {
                return this.m_modId;
            }
            set
            {
                if(this.m_modId != value)
                {
                    this.m_modId = value;

                    if(this.m_isInitialized)
                    {
                        Refresh();
                    }
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void OnEnable()
        {
            if(this.scrollView != null) { this.scrollView.verticalNormalizedPosition = 1f; }
        }

        protected virtual void Start()
        {
            Debug.Assert(modView != null);

            var tagCategories = ModBrowser.instance.gameProfile.tagCategories;
            if(tagCategories != null)
            {
                this.m_tagCategories = tagCategories;
            }

            if(modView.statisticsDisplay != null)
            {
                modView.statisticsDisplay.Initialize();
            }

            // add listeners
            modView.subscribeRequested +=      (v) => ModBrowser.instance.SubscribeToMod(v.data.profile.modId);
            modView.unsubscribeRequested +=    (v) => ModBrowser.instance.UnsubscribeFromMod(v.data.profile.modId);
            modView.enableModRequested +=      (v) => ModBrowser.instance.EnableMod(v.data.profile.modId);
            modView.disableModRequested +=     (v) => ModBrowser.instance.DisableMod(v.data.profile.modId);
            modView.ratePositiveRequested +=   (v) => ModBrowser.instance.AttemptRateMod(v.data.profile.modId, ModRatingValue.Positive);
            modView.rateNegativeRequested +=   (v) => ModBrowser.instance.AttemptRateMod(v.data.profile.modId, ModRatingValue.Negative);

            ModMediaContainer mediaContainer = modView.mediaContainer as ModMediaContainer;

            if(selectedMediaPreview != null)
            {
                selectedMediaPreview.Initialize();
                selectedMediaPreview.onClick += (d) =>
                {
                    if(d.data.descriptor == ImageDescriptor.YouTubeThumbnail)
                    {
                        UIUtilities.OpenYouTubeVideoURL(d.data.youTubeId);
                    }
                };

                if(mediaContainer != null)
                {
                    mediaContainer.logoClicked += MediaPreview_Logo;
                    mediaContainer.galleryImageClicked += MediaPreview_GalleryImage;
                    mediaContainer.youTubeThumbnailClicked += MediaPreview_YouTubeThumbnail;
                }
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

            this.m_isInitialized = true;
            Refresh();
        }

        // ---------[ UPDATE VIEW ]---------
        public void Refresh()
        {
            Debug.Assert(this.m_isInitialized);
            Debug.Assert(this.modView != null);

            ModProfile profile = null;
            ModStatistics stats = null;

            // set initial values
            this.SetLoadingDisplay(true);
            this.modView.DisplayLoading();

            if(this.selectedMediaPreview != null)
            {
                this.selectedMediaPreview.DisplayLoading();
            }

            // early out if NULL_ID
            if(this.m_modId == ModProfile.NULL_ID) { return; }

            // delegate for pushing changes to mod view
            Action pushToView = () =>
            {
                bool isModSubscribed = ModManager.GetSubscribedModIds().Contains(this.m_modId);
                bool isModEnabled = ModManager.GetEnabledModIds().Contains(this.m_modId);
                ModRatingValue rating = ModBrowser.instance.GetModRating(this.m_modId);

                if(profile != null)
                {
                    modView.DisplayMod(profile, stats,
                                       this.m_tagCategories,
                                       isModSubscribed, isModEnabled,
                                       rating);

                    // media container
                    if(modView.mediaContainer != null)
                    {
                        ModMediaCollection media = profile.media;
                        bool hasMedia = media != null;
                        hasMedia &= ((media.youTubeURLs != null && media.youTubeURLs.Length > 0)
                                     || (media.galleryImageLocators != null && media.galleryImageLocators.Length > 0));

                        modView.mediaContainer.gameObject.SetActive(hasMedia);
                    }

                    // media preview
                    if(selectedMediaPreview != null)
                    {
                        selectedMediaPreview.DisplayLogo(profile.id, profile.logoLocator);
                    }
                }
            };

            // profile
            ModProfileRequestManager.instance.RequestModProfile(this.m_modId,
            (p) =>
            {
                if(this == null) { return; }

                this.SetLoadingDisplay(false);
                profile = p;
                pushToView();
            },
            WebRequestError.LogAsWarning);

            // statistics
            ModStatisticsRequestManager.instance.RequestModStatistics(this.m_modId,
            (s) =>
            {
                if(this == null) { return; }

                stats = s;
                pushToView();
            },
            WebRequestError.LogAsWarning);

            // version history
            if(versionHistoryContainer != null
               && versionHistoryItemPrefab != null)
            {
                RequestFilter modfileFilter = new RequestFilter();
                modfileFilter.sortFieldName = ModIO.API.GetAllModfilesFilterFields.dateAdded;
                modfileFilter.isSortAscending = false;

                APIClient.GetAllModfiles(this.m_modId,
                                         modfileFilter,
                                         new APIPaginationParameters(){ limit = 10 },
                                         (r) =>
                                         {
                                            this.m_versionHistory = r.items;
                                            PopulateVersionHistory();
                                         },
                                         WebRequestError.LogAsWarning);
            }
        }

        public void SetLoadingDisplay(bool visible)
        {
            if(this.loadingDisplay != null)
            {
                this.loadingDisplay.SetActive(visible);
            }
        }

        // ---------[ UI ELEMENT CREATION ]---------
        private void PopulateVersionHistory()
        {
            #if UNITY_EDITOR
            if(!Application.isPlaying) { return; }
            #endif

            foreach(Transform t in versionHistoryContainer)
            {
                GameObject.Destroy(t.gameObject);
            }

            if(this.versionHistoryContainer == null) { return; }

            foreach(Modfile modfile in this.m_versionHistory)
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
        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            if(this.m_tagCategories != gameProfile.tagCategories)
            {
                this.m_tagCategories = gameProfile.tagCategories;

                if(this.m_isInitialized)
                {
                    Refresh();
                }
            }
        }

        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            Debug.Assert(this.modView != null);

            if(this.m_isInitialized)
            {
                ModDisplayData data = modView.data;
                bool wasSubscribed = data.isSubscribed;
                bool subChanged = ((!wasSubscribed && addedSubscriptions.Contains(this.m_modId))
                                   || (wasSubscribed && removedSubscriptions.Contains(this.m_modId)));

                if(subChanged)
                {
                    data.isSubscribed = !wasSubscribed;
                    modView.data = data;
                }
            }
        }

        public void OnModEnabled(int modId)
        {
            Debug.Assert(this.modView != null);

            if(this.m_isInitialized
               && this.m_modId == modId)
            {
                ModDisplayData data = this.modView.data;
                data.isModEnabled = true;
                this.modView.data = data;
            }
        }

        public void OnModDisabled(int modId)
        {
            Debug.Assert(this.modView != null);

            if(this.m_isInitialized
               && this.m_modId == modId)
            {
                ModDisplayData data = this.modView.data;
                data.isModEnabled = false;
                this.modView.data = data;
            }
        }

        public void OnModDownloadStarted(int modId, FileDownloadInfo downloadInfo)
        {
            Debug.Assert(this.modView != null);

            if(this.m_isInitialized
               && this.m_modId == modId)
            {
                this.modView.DisplayDownload(downloadInfo);
            }
        }

        public void OnModRatingAdded(int modId, ModRatingValue rating)
        {
            if(this.m_isInitialized
               && this.m_modId == modId)
            {
                ModDisplayData data = this.modView.data;
                data.userRating = rating;
                this.modView.data = data;
            }
        }

        private void MediaPreview_Logo(ImageDisplay display)
        {
            ImageDisplayData imageData = display.data;
            selectedMediaPreview.data = imageData;
        }
        private void MediaPreview_GalleryImage(ImageDisplay display)
        {
            ImageDisplayData imageData = display.data;
            selectedMediaPreview.data = imageData;
        }
        private void MediaPreview_YouTubeThumbnail(ImageDisplay display)
        {
            ImageDisplayData displayData = display.data;
            selectedMediaPreview.data = displayData;
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        public ModProfile profile;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private ModProfile m_profile;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private ModStatistics m_statistics;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private bool m_isModSubscribed;
        [Obsolete("No longer used. Refer to InspectorView.m_modId instead.")]
        private bool m_isModEnabled;

        [Obsolete("No longer necessary. Initialization occurs in Start().")]
        public void Initialize() {}

        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> subscribeRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifySubscribeRequested()
        {
            if(subscribeRequested != null)
            {
                subscribeRequested(this.m_profile);
            }
        }
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> unsubscribeRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyUnsubscribeRequested()
        {
            if(unsubscribeRequested != null)
            {
                unsubscribeRequested(this.m_profile);
            }
        }
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> enableRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyEnableRequested()
        {
            if(enableRequested != null)
            {
                enableRequested(this.m_profile);
            }
        }
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public event Action<ModProfile> disableRequested;
        [Obsolete("No longer necessary. Event is directly linked to ModBrowser.")]
        public void NotifyDisableRequested()
        {
            if(disableRequested != null)
            {
                disableRequested(this.m_profile);
            }
        }

        [Obsolete("Use OnModSubscriptionsUpdated() instead")]
        public void DisplayModSubscribed(bool isSubscribed)
        {
            if(this.m_isInitialized)
            {
                ModDisplayData data = modView.data;
                if(data.isSubscribed != isSubscribed)
                {
                    data.isSubscribed = isSubscribed;
                    modView.data = data;
                }
            }
        }

        [Obsolete("Use OnModEnabled()/OnModDisabled() instead")]
        public void DisplayModEnabled(bool isEnabled)
        {
            if(isEnabled)
            {
                this.OnModEnabled(this.m_modId);
            }
            else
            {
                this.OnModDisabled(this.m_modId);
            }
        }

        [Obsolete("Set the modId value and/or use Refresh() instead.")]
        public void DisplayMod(ModProfile profile, ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isModSubscribed, bool isModEnabled)
        {
            Debug.Assert(profile != null);
            this.modId = profile.id;
        }
    }
}
