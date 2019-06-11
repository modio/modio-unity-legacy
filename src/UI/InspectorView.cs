using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class InspectorView : MonoBehaviour, IGameProfileUpdateReceiver, IModDownloadStartedReceiver, IModEnabledReceiver, IModDisabledReceiver, IModSubscriptionsUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        public GameObject versionHistoryItemPrefab = null;
        public string missingVersionChangelogText = "<i>None recorded.</i>";
        public ModProfileRequestManager profileRequests = null;
        public ModStatisticsRequestManager statisticsRequests = null;

        [Header("UI Components")]
        public ModView modView;
        public ImageDisplay selectedMediaPreview;
        public RectTransform versionHistoryContainer;
        public ScrollRect scrollView;
        public Button backToDiscoverButton;
        public Button backToSubscriptionsButton;
        public GameObject loadingDisplay;

        // ---[ RUNTIME DATA ]---
        private ModProfile m_profile = null;
        private ModStatistics m_statistics = null;
        private bool m_isModSubscribed = false;
        private bool m_isModEnabled = false;
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
            var tagCategories = ModBrowser.instance.gameProfile.tagCategories;
            if(tagCategories != null)
            {
                this.m_tagCategories = tagCategories;
            }

            // check for request managers
            if(this.profileRequests == null)
            {
                Debug.Log("[mod.io] The profileRequests component on this InspectorView"
                          + " has been automatically created. Assing a ModProfileRequestManager"
                          + " component to this field that is shared by other views will allow"
                          + " for a much more efficient and responsive browsing experience.",
                          this);

                this.profileRequests = this.gameObject.AddComponent<ModProfileRequestManager>();
            }

            if(this.statisticsRequests == null)
            {
                Debug.Log("[mod.io] The statisticsRequests component on this InspectorView"
                          + " has been automatically created. Assing a ModStatisticsRequestManager"
                          + " component to this field that is shared by other views will allow"
                          + " for a much more efficient and responsive browsing experience.",
                          this);

                this.statisticsRequests = this.gameObject.AddComponent<ModStatisticsRequestManager>();
            }

            ModMediaContainer mediaContainer = null;

            if(modView != null)
            {
                if(modView.statisticsDisplay != null)
                {
                    modView.statisticsDisplay.Initialize();
                }

                mediaContainer = modView.mediaContainer as ModMediaContainer;

                // add listeners
                modView.subscribeRequested +=      (v) => ModBrowser.instance.SubscribeToMod(v.data.profile.modId);
                modView.unsubscribeRequested +=    (v) => ModBrowser.instance.UnsubscribeFromMod(v.data.profile.modId);
                modView.enableModRequested +=      (v) => ModBrowser.instance.EnableMod(v.data.profile.modId);
                modView.disableModRequested +=     (v) => ModBrowser.instance.DisableMod(v.data.profile.modId);
            }

            if(selectedMediaPreview != null)
            {
                selectedMediaPreview.Initialize();
                selectedMediaPreview.onClick += (d) =>
                {
                    if(d.data.mediaType == ImageDisplayData.MediaType.YouTubeThumbnail)
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

            if(this.loadingDisplay != null)
            {
                this.loadingDisplay.gameObject.SetActive(true);
            }

            // early out if NULL_ID
            if(this.m_modId == ModProfile.NULL_ID) { return; }

            this.m_profile = null;
            this.m_statistics = null;
            this.m_isModSubscribed = ModManager.GetSubscribedModIds().Contains(this.m_modId);
            this.m_isModEnabled = ModManager.GetEnabledModIds().Contains(this.m_modId);

            // profile
            this.profileRequests.RequestModProfile(this.m_modId,
            (p) =>
            {
                this.m_profile = p;
                this.UpdateModView();
            },
            WebRequestError.LogAsWarning);

            // statistics
            this.statisticsRequests.RequestModStatistics(this.m_modId,
            (s) =>
            {
                this.m_statistics = s;
                this.UpdateModView();
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

        private void UpdateModView()
        {
            if(this.m_profile == null
               || this.m_profile.id == ModProfile.NULL_ID)
            {
                if(this.modView != null)
                {
                    this.modView.DisplayLoading();
                }
                if(this.selectedMediaPreview != null)
                {
                    this.selectedMediaPreview.DisplayLoading();
                }
                return;
            }

            if(this.loadingDisplay != null)
            {
                this.loadingDisplay.SetActive(false);
            }

            if(modView != null)
            {
                modView.DisplayMod(this.m_profile, this.m_statistics,
                                   this.m_tagCategories,
                                   this.m_isModSubscribed, this.m_isModEnabled);

                if(modView.mediaContainer != null)
                {
                    ModMediaCollection media = this.m_profile.media;
                    bool hasMedia = media != null;
                    hasMedia &= ((media.youTubeURLs != null && media.youTubeURLs.Length > 0)
                                 || (media.galleryImageLocators != null && media.galleryImageLocators.Length > 0));

                    modView.mediaContainer.gameObject.SetActive(hasMedia);
                }
            }

            if(selectedMediaPreview != null)
            {
                selectedMediaPreview.DisplayLogo(this.m_profile.id, this.m_profile.logoLocator);
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
                this.UpdateModView();
            }
        }

        public void OnModSubscriptionsUpdated()
        {
            this.m_isModSubscribed = ModManager.GetSubscribedModIds().Contains(this.m_modId);

            if(this.modView != null)
            {
                ModDisplayData data = modView.data;
                data.isSubscribed = this.m_isModSubscribed;
                modView.data = data;
            }
        }

        public void OnModEnabled(int modId)
        {
            if(this.m_modId == modId
               && this.modView != null)
            {
                ModDisplayData data = this.modView.data;
                data.isModEnabled = true;
                this.modView.data = data;
            }
        }

        public void OnModDisabled(int modId)
        {
            if(this.m_modId == modId
               && this.modView != null)
            {
                ModDisplayData data = this.modView.data;
                data.isModEnabled = false;
                this.modView.data = data;
            }
        }

        public void OnModDownloadStarted(int modId, FileDownloadInfo downloadInfo)
        {
            if(this.m_modId == modId
               && this.modView != null)
            {
                this.modView.DisplayDownload(downloadInfo);
            }
        }

        private void MediaPreview_Logo(ImageDisplayComponent display)
        {
            ImageDisplayData imageData = display.data;
            selectedMediaPreview.data = imageData;

            if(imageData.GetImageTexture(selectedMediaPreview.useOriginal) == null)
            {
                bool original = selectedMediaPreview.useOriginal;
                LogoSize size = (original ? LogoSize.Original : ImageDisplayData.logoThumbnailSize);

                ModManager.GetModLogo(this.m_profile, size,
                                      (t) =>
                                      {
                                        if(Application.isPlaying
                                           && selectedMediaPreview.data.Equals(imageData))
                                        {
                                            imageData.SetImageTexture(original, t);
                                            selectedMediaPreview.data = imageData;
                                        }
                                      },
                                      WebRequestError.LogAsWarning);
            }
        }
        private void MediaPreview_GalleryImage(ImageDisplayComponent display)
        {
            ImageDisplayData imageData = display.data;
            selectedMediaPreview.data = imageData;

            if(imageData.GetImageTexture(selectedMediaPreview.useOriginal) == null)
            {
                bool original = selectedMediaPreview.useOriginal;
                ModGalleryImageSize size = (original ? ModGalleryImageSize.Original : ImageDisplayData.galleryThumbnailSize);

                ModManager.GetModGalleryImage(this.m_profile, display.data.fileName,
                                              size,
                                              (t) =>
                                              {
                                                if(Application.isPlaying
                                                   && selectedMediaPreview.data.Equals(imageData))
                                                {
                                                    imageData.SetImageTexture(original, t);
                                                    selectedMediaPreview.data = imageData;
                                                }
                                              },
                                              WebRequestError.LogAsWarning);
            }
        }
        private void MediaPreview_YouTubeThumbnail(ImageDisplayComponent display)
        {
            ImageDisplayData displayData = display.data;
            selectedMediaPreview.data = displayData;
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("Public access revoked.")]
        public ModProfile profile
        {
            get { return this.m_profile; }
        }

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
            this.OnModSubscriptionsUpdated();
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

        [Obsolete("No longer requires a ModTagCollection parameter.")]
        public void DisplayMod(ModProfile profile, ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isModSubscribed, bool isModEnabled)
        {
            DisplayMod(profile, statistics, isModSubscribed, isModEnabled);
        }
    }
}
