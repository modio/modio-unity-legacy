using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.UI
{
    [DisallowMultipleComponent]
    public class ModView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<ModView> onClick;
        public event Action<ModView> subscribeRequested;
        public event Action<ModView> unsubscribeRequested;
        public event Action<ModView> enableModRequested;
        public event Action<ModView> disableModRequested;
        public event Action<ModView> ratePositiveRequested;
        public event Action<ModView> rateNegativeRequested;

        public event Action<ModProfile> onProfileChanged;
        public event Action<ModStatistics> onStatisticsChanged;

        /// <summary>Currently displayed mod profile.</summary>
        [SerializeField]
        private ModProfile m_profile = null;

        /// <summary>Currently displayed mod statistics.</summary>
        [SerializeField]
        private ModStatistics m_statistics = null;

        // --- Accessors ---
        /// <summary>Currently displayed mod profile.</summary>
        public ModProfile profile
        {
            get { return this.m_profile; }
            set
            {
                if(this.m_profile != value)
                {
                    this.m_profile = value;

                    if(this.onProfileChanged != null)
                    {
                        this.onProfileChanged(this.m_profile);
                    }
                }
            }
        }

        /// <summary>Currently displayed mod statistics.</summary>
        public ModStatistics statistics
        {
            get { return this.m_statistics; }
            set
            {
                if(this.m_statistics != value)
                {
                    this.m_statistics = value;

                    if(this.onStatisticsChanged != null)
                    {
                        this.onStatisticsChanged(this.m_statistics);
                    }
                }
            }
        }

        // --- FUNCTION DELEGATES ---
        private delegate void GetDataDelegate(ref ModDisplayData data);
        private List<GetDataDelegate> m_getDelegates = null;

        private delegate void SetDataDelegate(ModDisplayData data);
        private List<SetDataDelegate> m_setDelegates = null;

        private delegate void DisplayProfileDelegate(ModProfile profile);
        private List<DisplayProfileDelegate> m_displayDelegates = null;

        private delegate void ProfileParserDelegate(ModProfile profile, ref ModDisplayData data);
        private List<ProfileParserDelegate> m_missingDisplayParsers = null;


        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            #if DEBUG
            ModView nested = this.gameObject.GetComponentInChildren<ModView>(true);
            if(nested != null && nested != this)
            {
                Debug.LogError("[mod.io] Nesting ModViews is currently not supported due to the"
                               + " way IModViewElement component parenting works."
                               + "\nThe nested ModViews must be removed to allow ModView functionality."
                               + "\nthis=" + this.gameObject.name
                               + "\nnested=" + nested.gameObject.name,
                               this);
                return;
            }
            #endif

            // assign mod view elements to this
            var modViewElements = this.gameObject.GetComponentsInChildren<IModViewElement>(true);
            foreach(IModViewElement viewElement in modViewElements)
            {
                viewElement.SetModView(this);
            }
        }

        public void OnEnable()
        {
            FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(m_data.profile.modId,
                                                                                      m_data.currentBuild.modfileId);
            DisplayDownload(downloadInfo);
        }

        // NOTE(@jackson): This ignores the Logo as it'll be set anyway
        private void ParseProfileMedia(ModProfile profile, ref ModDisplayData data)
        {
            // - early out -
            if(profile.media == null)
            {
                data.galleryImages = new ImageDisplayData[0];
                data.youTubeThumbnails = new ImageDisplayData[0];
                return;
            }

            // - parse -
            List<ImageDisplayData> media = new List<ImageDisplayData>();

            if(profile.media.galleryImageLocators != null
               && profile.media.galleryImageLocators.Length > 0)
            {
                foreach(GalleryImageLocator locator in profile.media.galleryImageLocators)
                {
                    ImageDisplayData imageData;
                    if(locator != null)
                    {
                        imageData = ImageDisplayData.CreateForModGalleryImage(profile.id,
                                                                              locator);
                    }
                    else
                    {
                        imageData = new ImageDisplayData();
                    }

                    media.Add(imageData);
                }
            }
            data.galleryImages = media.ToArray();

            media.Clear();
            if(profile.media.youTubeURLs != null
               && profile.media.youTubeURLs.Length > 0)
            {
                foreach(string url in profile.media.youTubeURLs)
                {
                    ImageDisplayData imageData;
                    if(!string.IsNullOrEmpty(url))
                    {
                        imageData = ImageDisplayData.CreateForYouTubeThumbnail(profile.id,
                                                                               Utility.ExtractYouTubeIdFromURL(url));
                    }
                    else
                    {
                        imageData = new ImageDisplayData();
                    }

                    media.Add(imageData);
                }
            }
            data.youTubeThumbnails = media.ToArray();
        }

        private System.Collections.IEnumerator MonitorDownloadCoroutine(int modId)
        {
            while(downloadDisplay.data.isActive)
            {
                yield return null;
            }

            if(data.profile.modId == modId)
            {
                yield return new WaitForSecondsRealtime(4f);

                downloadDisplay.gameObject.SetActive(false);
            }
        }

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }

        public void NotifySubscribeRequested()
        {
            if(subscribeRequested != null)
            {
                subscribeRequested(this);
            }
        }
        public void NotifyUnsubscribeRequested()
        {
            if(unsubscribeRequested != null)
            {
                unsubscribeRequested(this);
            }
        }

        public void NotifyEnableModRequested()
        {
            if(enableModRequested != null)
            {
                enableModRequested(this);
            }
        }
        public void NotifyDisableModRequested()
        {
            if(disableModRequested != null)
            {
                disableModRequested(this);
            }
        }
        public void NotifyRatePositiveRequested()
        {
            if(this.ratePositiveRequested != null)
            {
                this.ratePositiveRequested(this);
            }
        }
        public void NotifyRateNegativeRequested()
        {
            if(this.rateNegativeRequested != null)
            {
                this.rateNegativeRequested(this);
            }
        }

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer necessary.")]
        public void Initialize() {}

        [Obsolete("No longer supported.")][HideInInspector]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();

        [Obsolete("No longer supported. Use ModView.profile and ModView.statistics instead.")]
        public ModDisplayData data
        {
            get
            {
                return this.m_data;
            }
            set
            {
                this.m_data = value;
            }
        }

        [Obsolete("Use ModProfileFieldDisplay components instead.")][HideInInspector]
        public ModProfileDisplayComponent profileDisplay;

        [Obsolete("Use ModLogoDisplay component instead.")][HideInInspector]
        public ImageDisplay logoDisplay;

        [Obsolete("Use ModLogoDisplay, GalleryImageContainer, and YouTubeThumbnailContainer components instead.")][HideInInspector]
        public ModMediaCollectionDisplayComponent mediaContainer;

        [Obsolete][Serializable]
        public struct SubmittorDisplay
        {
            public UserProfileDisplayComponent  profile;
            public ImageDisplay                 avatar;
        }
        [Obsolete("Use a ModSubmittorDisplay component instead.")][HideInInspector]
        public SubmittorDisplay submittorDisplay;

        [Obsolete("Use a CurrentBuildDisplay component instead.")][HideInInspector]
        public ModfileDisplayComponent buildDisplay;

        [Obsolete("Use a TagContainer or TagCollectionTextDisplay component instead.")][HideInInspector]
        public ModTagCollectionDisplayComponent tagsDisplay;

        [Obsolete("Use a ModEnabledDisplay component instead.")][HideInInspector]
        public StateToggleDisplay modEnabledDisplay;

        [Obsolete("Use a ModSubscribedDisplay component instead.")][HideInInspector]
        public StateToggleDisplay subscriptionDisplay;

        [Obsolete][Serializable]
        public struct UserRatingDisplay
        {
            public StateToggleDisplay positive;
            public StateToggleDisplay negative;
        }
        [Obsolete("Use a ModUserRatingDisplay component instead.")][HideInInspector]
        public UserRatingDisplay userRatingDisplay;

        [Obsolete("Use ModStatisticsFieldDisplay components instead.")][HideInInspector]
        public ModStatisticsDisplayComponent statisticsDisplay;

        [Obsolete("Use ModBinaryDownloadDisplay instead.")][HideInInspector]
        public DownloadDisplayComponent downloadDisplay;

        [Obsolete("Set via ModView.profile and ModView.statistics instead.")]
        public void DisplayMod(ModProfile profile,
                               ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isSubscribed,
                               bool isModEnabled,
                               ModRatingValue userRating = ModRatingValue.None)
        {
            this.profile = profile;
            this.statistics = statistics;
        }

        [Obsolete("No longer supported.")]
        public void DisplayLoading() { throw new System.NotImplementedException(); }

        [Obsolete("No longer supported. Use a ModBinaryDownloadDisplay component instead.")]
        public void DisplayDownload(FileDownloadInfo downloadInfo) { throw new System.NotImplementedException(); }
    }
}
