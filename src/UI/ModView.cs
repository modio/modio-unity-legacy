using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.UI
{
    [DisallowMultipleComponent]
    public class ModView : MonoBehaviour
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Event for notifying listeners of a change to the mod profile.</summary>
        [Serializable]
        public class ProfileChangedEvent : UnityEngine.Events.UnityEvent<ModProfile> {}

        /// <summary>Event for notifying listeners of a change to the mod statistics.</summary>
        [Serializable]
        public class StatisticsChangedEvent : UnityEngine.Events.UnityEvent<ModStatistics> {}

        // ---------[ FIELDS ]---------
        public event Action<ModView> onClick;
        public event Action<ModView> subscribeRequested;
        public event Action<ModView> unsubscribeRequested;
        public event Action<ModView> enableModRequested;
        public event Action<ModView> disableModRequested;
        public event Action<ModView> ratePositiveRequested;
        public event Action<ModView> rateNegativeRequested;

        /// <summary>Currently displayed mod profile.</summary>
        [SerializeField]
        private ModProfile m_profile = null;

        /// <summary>Currently displayed mod statistics.</summary>
        [SerializeField]
        private ModStatistics m_statistics = null;

        /// <summary>Replace an empty description with the summary?</summary>
        [Tooltip("If the profile has no description, the description can be filled with the summary instead.")]
        public bool replaceMissingDescription = true;

        /// <summary>Event for notifying listeners of a change to the mod profile.</summary>
        public ProfileChangedEvent onProfileChanged = null;

        /// <summary>Event for notifying listeners of a change to the mod statistics.</summary>
        public StatisticsChangedEvent onStatisticsChanged = null;

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

                    if(this.replaceMissingDescription
                       && this.m_profile != null)
                    {
                        if(string.IsNullOrEmpty(this.m_profile.descriptionAsText)
                           && string.IsNullOrEmpty(this.m_profile.descriptionAsHTML))
                        {
                            this.m_profile.descriptionAsText = this.m_profile.summary;
                            this.m_profile.descriptionAsHTML = this.m_profile.summary;
                        }
                    }

                    if(this.onProfileChanged != null)
                    {
                        this.onProfileChanged.Invoke(this.m_profile);
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
                        this.onStatisticsChanged.Invoke(this.m_statistics);
                    }
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Collects and sets view on IModViewElements.</summary>
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

        // ---------[ UI HELPER FUNCTIONS ]---------
        /// <summary>Attempts to subscribe to the currently displayed mod.</summary>
        public void AttemptSubscribe()
        {
            if(this.m_profile != null)
            {
                ModBrowser.instance.SubscribeToMod(this.m_profile.id);
            }
        }

        /// <summary>Attempts to unsubscribe from the currently displayed mod.</summary>
        public void AttemptUnsubscribe()
        {
            if(this.m_profile != null)
            {
                ModBrowser.instance.UnsubscribeFromMod(this.m_profile.id);
            }
        }

        /// <summary>Attempts to enable the currently displayed mod.</summary>
        public void AttemptEnableMod()
        {
            if(this.m_profile != null)
            {
                ModBrowser.instance.EnableMod(this.m_profile.id);
            }
        }

        /// <summary>Attempts to disable the currently displayed mod.</summary>
        public void AttemptDisableMod()
        {
            if(this.m_profile != null)
            {
                ModBrowser.instance.DisableMod(this.m_profile.id);
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
