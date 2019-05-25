using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

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
        public ModView modView;
        public ImageDisplay selectedMediaPreview;
        public RectTransform versionHistoryContainer;
        public ScrollRect scrollView;
        public Button backToDiscoverButton;
        public Button backToSubscriptionsButton;
        public GameObject loadingDisplay;

        // ---[ RUNTIME DATA ]---
        [Header("Runtime Data")]
        public ModProfile profile;
        public ModStatistics statistics;
        public bool isModSubscribed;
        public bool isModEnabled;

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
                    FetchDisplayData();
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        private void OnEnable()
        {
            if(this.scrollView != null) { this.scrollView.verticalNormalizedPosition = 1f; }
        }

        private void Start()
        {
            // TODO(@jackson): Asserts
            ModMediaContainer mediaContainer = null;

            if(modView != null)
            {
                modView.Initialize();

                if(modView.statisticsDisplay != null)
                {
                    modView.statisticsDisplay.Initialize();
                }

                mediaContainer = modView.mediaContainer as ModMediaContainer;
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
        }

        // ---------[ UPDATE VIEW ]---------
        public void DisplayMod(ModProfile profile, ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories, // TODO(@jackson): Remove
                               bool isModSubscribed, bool isModEnabled)
        {
            // TODO(@jackson): Remove
            if(profile == null)
            {
                return;
            }

            this.m_modId = profile.id;
            this.profile = profile;
            this.statistics = statistics;
            this.m_tagCategories = tagCategories;
            this.isModSubscribed = isModSubscribed;
            this.isModEnabled = isModEnabled;

            this.UpdateModView();
            this.PopulateVersionHistory();
        }

        public void DisplayModSubscribed(bool isSubscribed)
        {
        }

        public void DisplayModEnabled(bool isEnabled)
        {
            this.isModEnabled = isEnabled;

            if(modView != null)
            {
                ModDisplayData data = modView.data;
                data.isModEnabled = isEnabled;
                modView.data = data;
            }
        }

        // TODO(@jackson): privatise
        private void FetchDisplayData()
        {
            if(this.loadingDisplay != null)
            {
                this.loadingDisplay.gameObject.SetActive(true);
            }

            this.profile = null;
            this.statistics = null;
            this.isModSubscribed = ModManager.GetSubscribedModIds().Contains(this.m_modId);
            this.isModEnabled = ModManager.GetEnabledModIds().Contains(this.m_modId);

            // profile
            ModManager.GetModProfile(this.m_modId,
                                     (p) =>
                                     {
                                        this.profile = p;
                                        this.UpdateModView();
                                     },
                                     WebRequestError.LogAsWarning);


            // statistics
            ModManager.GetModStatistics(this.m_modId,
                                        (s) =>
                                        {
                                            this.statistics = s;
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
                                         new APIPaginationParameters(){ limit = 20 },
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
            if(this.profile == null)
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
                modView.DisplayMod(this.profile, this.statistics,
                                   this.m_tagCategories,
                                   this.isModSubscribed, this.isModEnabled);

                if(modView.mediaContainer != null)
                {
                    ModMediaCollection media = profile.media;
                    bool hasMedia = media != null;
                    hasMedia &= ((media.youTubeURLs != null && media.youTubeURLs.Length > 0)
                                 || (media.galleryImageLocators != null && media.galleryImageLocators.Length > 0));

                    modView.mediaContainer.gameObject.SetActive(hasMedia);
                }
            }

            if(selectedMediaPreview != null)
            {
                selectedMediaPreview.DisplayLogo(profile.id, profile.logoLocator);
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

        public void OnGameProfileUpdated(GameProfile gameProfile)
        {
            if(this.m_tagCategories != gameProfile.tagCategories)
            {
                this.m_tagCategories = gameProfile.tagCategories;
            }
        }

        public void OnSubscriptionsUpdated()
        {
            this.isModSubscribed = ModManager.GetSubscribedModIds().Contains(this.m_modId);

            if(this.modView != null)
            {
                ModDisplayData data = modView.data;
                data.isSubscribed = this.isModSubscribed;
                modView.data = data;
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

                ModManager.GetModLogo(profile, size,
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

                ModManager.GetModGalleryImage(profile, display.data.fileName,
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
        [Obsolete("No longer necessary. Initialization occurs in Start().")]
        public void Initialize() {}
    }
}
