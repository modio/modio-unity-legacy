using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.UI
{
    public class ModView : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<ModView> onClick;
        public event Action<ModView> subscribeRequested;
        public event Action<ModView> unsubscribeRequested;
        public event Action<ModView> enableModRequested;
        public event Action<ModView> disableModRequested;

        [Serializable]
        public struct CreatorDisplay
        {
            public UserProfileDisplayComponent  profile;
            public UserAvatarDisplayComponent   avatar;
        }

        [Serializable]
        public struct SubscriptionStatusDisplay
        {
            public GameObject isSubscribed;
            public GameObject notSubscribed;
        }

        [Serializable]
        public struct EnabledStatusDisplay
        {
            public GameObject isEnabled;
            public GameObject isDisabled;
        }

        [Header("UI Components")]
        public ModProfileDisplayComponent           profileDisplay;
        public CreatorDisplay                       creatorDisplay;
        public ModLogoDisplayComponent              logoDisplay;
        public ModMediaCollectionDisplayComponent   mediaContainer;
        public ModfileDisplayComponent              buildDisplay;
        public ModTagCollectionDisplayComponent     tagsDisplay;
        public ModStatisticsDisplayComponent        statisticsDisplay;
        public ModBinaryDownloadDisplay             downloadDisplay;
        public SubscriptionStatusDisplay            subscriptionDisplay;
        public EnabledStatusDisplay                 modEnabledDisplay;

        [Header("Display Data")]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();

        // --- FUNCTION DELEGATES ---
        private delegate void GetDataDelegate(ref ModDisplayData data);
        private List<GetDataDelegate> m_getDelegates = new List<GetDataDelegate>();

        private delegate void SetDataDelegate(ModDisplayData data);
        private List<SetDataDelegate> m_setDelegates = new List<SetDataDelegate>();

        private delegate void DisplayProfileDelegate(ModProfile profile);
        private List<DisplayProfileDelegate> m_displayDelegates = new List<DisplayProfileDelegate>();

        private delegate void DisplayLoadingDelegate();
        private List<DisplayLoadingDelegate> m_loadingDelegates = new List<DisplayLoadingDelegate>();

        // --- ACCESSORS ---
        public ModDisplayData data
        {
            get
            {
                return GetData();
            }
            set
            {
                SetData(value);
            }
        }

        private ModDisplayData GetData()
        {
            foreach(GetDataDelegate getDelegate in m_getDelegates)
            {
                getDelegate(ref m_data);
            }

            return m_data;
        }

        private void SetData(ModDisplayData value)
        {
            m_data = value;
            foreach(SetDataDelegate setDelegate in m_setDelegates)
            {
                setDelegate(value);
            }

            if(subscriptionDisplay.isSubscribed != null)
            {
                subscriptionDisplay.isSubscribed.SetActive(m_data.isSubscribed);
            }
            if(subscriptionDisplay.notSubscribed != null)
            {
                subscriptionDisplay.notSubscribed.SetActive(!m_data.isSubscribed);
            }

            if(modEnabledDisplay.isEnabled != null)
            {
                modEnabledDisplay.isEnabled.SetActive(m_data.isModEnabled);
            }
            if(modEnabledDisplay.isDisabled != null)
            {
                modEnabledDisplay.isDisabled.SetActive(!m_data.isModEnabled);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            m_getDelegates.Clear();
            m_setDelegates.Clear();
            m_displayDelegates.Clear();

            if(profileDisplay != null)
            {
                profileDisplay.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.profile = profileDisplay.data;
                });
                m_setDelegates.Add((d) =>
                {
                    profileDisplay.data = d.profile;
                });

                m_displayDelegates.Add((p) => profileDisplay.DisplayProfile(p));
                m_loadingDelegates.Add(() => profileDisplay.DisplayLoading());
            }
            if(mediaContainer != null)
            {
                mediaContainer.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.media = mediaContainer.data.ToArray();
                });
                m_setDelegates.Add((d) =>
                {
                    mediaContainer.data = d.media;
                });

                m_displayDelegates.Add((p) => mediaContainer.DisplayMedia(p));
                m_loadingDelegates.Add(( ) => mediaContainer.DisplayLoading());
            }
            // NOTE(@jackson): Logo Data overrides Media Container Logo Data
            if(logoDisplay != null)
            {
                logoDisplay.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.SetLogo(logoDisplay.data);
                });
                m_setDelegates.Add((d) =>
                {
                    logoDisplay.data = d.GetLogo();
                });

                m_displayDelegates.Add((p) => logoDisplay.DisplayLogo(p.id, p.logoLocator));
                m_loadingDelegates.Add(( ) => logoDisplay.DisplayLoading());
            }
            if(creatorDisplay.profile != null)
            {
                creatorDisplay.profile.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.submittedBy.profile = creatorDisplay.profile.data;
                });
                m_setDelegates.Add((d) =>
                {
                    creatorDisplay.profile.data = d.submittedBy.profile;
                });

                m_displayDelegates.Add((p) => creatorDisplay.profile.DisplayProfile(p.submittedBy));
                m_loadingDelegates.Add(( ) => creatorDisplay.profile.DisplayLoading());
            }
            if(creatorDisplay.avatar != null)
            {
                creatorDisplay.avatar.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.submittedBy.avatar = creatorDisplay.avatar.data;
                });
                m_setDelegates.Add((d) =>
                {
                    creatorDisplay.avatar.data = d.submittedBy.avatar;
                });

                m_displayDelegates.Add((p) => creatorDisplay.avatar.DisplayAvatar(p.submittedBy.id,
                                                                                  p.submittedBy.avatarLocator));
                m_loadingDelegates.Add(( ) => creatorDisplay.avatar.DisplayLoading());
            }
            if(buildDisplay != null)
            {
                buildDisplay.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.currentBuild = buildDisplay.data;
                });
                m_setDelegates.Add((d) =>
                {
                    buildDisplay.data = d.currentBuild;
                });

                m_displayDelegates.Add((p) => buildDisplay.DisplayModfile(p.currentBuild));
                m_loadingDelegates.Add(( ) => buildDisplay.DisplayLoading());
            }

            if(tagsDisplay != null)
            {
                tagsDisplay.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.tags = tagsDisplay.data.ToArray();
                });
                m_setDelegates.Add((d) =>
                {
                    tagsDisplay.data = d.tags;
                });

                // NOTE(@jackson): tags has no display delegate as it requires categories
                m_loadingDelegates.Add(( ) => tagsDisplay.DisplayLoading());
            }

            if(statisticsDisplay != null)
            {
                statisticsDisplay.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.statistics = statisticsDisplay.data;
                });
                m_setDelegates.Add((d) =>
                {
                    statisticsDisplay.data = d.statistics;
                });

                m_loadingDelegates.Add(( ) => statisticsDisplay.DisplayLoading());
            }

            if(downloadDisplay != null)
            {
                downloadDisplay.Initialize();

                // m_getDelegates.Add((ref ModDisplayData d) =>
                // {
                //     d.submittedBy = creatorView.data;
                // });
                // m_setDelegates.Add((d) =>
                // {
                //     creatorView.data = d.submittedBy;
                // });
            }
        }

        public void DisplayMod(ModProfile profile,
                               ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isSubscribed,
                               bool isModEnabled)
        {
            Debug.Assert(profile != null);

            m_data = new ModDisplayData()
            {
                isSubscribed = isSubscribed,
                isModEnabled = isModEnabled,
            };

            foreach(DisplayProfileDelegate displayDelegate in m_displayDelegates)
            {
                displayDelegate(profile);
            }

            if(tagsDisplay != null)
            {
                tagsDisplay.DisplayTags(profile, tagCategories);
            }

            if(statisticsDisplay != null)
            {
                if(statistics == null)
                {
                    statisticsDisplay.data = new ModStatisticsDisplayData()
                    {
                        modId = profile.id,
                    };
                }
                else
                {
                    statisticsDisplay.DisplayStatistics(statistics);
                }
            }

            // TODO(@jackson): DownloadDisplay

            if(subscriptionDisplay.isSubscribed != null)
            {
                subscriptionDisplay.isSubscribed.SetActive(isSubscribed);
            }
            if(subscriptionDisplay.notSubscribed != null)
            {
                subscriptionDisplay.notSubscribed.SetActive(!isSubscribed);
            }

            if(modEnabledDisplay.isEnabled != null)
            {
                modEnabledDisplay.isEnabled.SetActive(isModEnabled);
            }
            if(modEnabledDisplay.isDisabled != null)
            {
                modEnabledDisplay.isDisabled.SetActive(!isModEnabled);
            }
        }

        public void DisplayLoading()
        {
            foreach(DisplayLoadingDelegate loadingDelegate in m_loadingDelegates)
            {
                loadingDelegate();
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

        #if UNITY_EDITOR
        private void OnValidate()
        {
            SetData(m_data);
        }
        #endif
    }
}
