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
        public struct SubmittorDisplay
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
        public SubmittorDisplay                     submittorDisplay;
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

        private delegate void ProfileParserDelegate(ModProfile profile, ref ModDisplayData data);
        private List<ProfileParserDelegate> m_missingDisplayParsers = new List<ProfileParserDelegate>();

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
            CollectDelegates();
        }

        private void CollectDelegates()
        {
            m_getDelegates.Clear();
            m_setDelegates.Clear();
            m_displayDelegates.Clear();
            m_missingDisplayParsers.Clear();
            m_loadingDelegates.Clear();

            // - profile -
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
            else
            {
                m_missingDisplayParsers.Add((ModProfile p, ref ModDisplayData d) =>
                {
                    d.profile = ModProfileDisplayData.CreateFromProfile(p);
                });
            }

            // - media -
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
            else
            {
                m_missingDisplayParsers.Add(ParseProfileMedia);
            }

            // - logo -
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
            else
            {
                m_missingDisplayParsers.Add((ModProfile p, ref ModDisplayData d) =>
                {
                    ImageDisplayData logoData = new ImageDisplayData()
                    {
                        modId = p.id,
                        mediaType = ImageDisplayData.MediaType.ModLogo,
                        fileName = string.Empty,
                        texture = null,
                    };
                    if(p.logoLocator != null)
                    {
                        logoData.fileName = p.logoLocator.fileName;
                    }

                    d.SetLogo(logoData);
                });
            }

            // - submittor -
            if(submittorDisplay.profile != null)
            {
                submittorDisplay.profile.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.submittorProfile = submittorDisplay.profile.data;
                });
                m_setDelegates.Add((d) =>
                {
                    submittorDisplay.profile.data = d.submittorProfile;
                });

                m_displayDelegates.Add((p) => submittorDisplay.profile.DisplayProfile(p.submittedBy));
                m_loadingDelegates.Add(( ) => submittorDisplay.profile.DisplayLoading());
            }
            else
            {
                m_missingDisplayParsers.Add((ModProfile p, ref ModDisplayData d) =>
                {
                    d.submittorProfile = UserProfileDisplayData.CreateFromProfile(p.submittedBy);
                });
            }

            if(submittorDisplay.avatar != null)
            {
                submittorDisplay.avatar.Initialize();

                m_getDelegates.Add((ref ModDisplayData d) =>
                {
                    d.submittorAvatar = submittorDisplay.avatar.data;
                });
                m_setDelegates.Add((d) =>
                {
                    submittorDisplay.avatar.data = d.submittorAvatar;
                });

                m_displayDelegates.Add((p) => submittorDisplay.avatar.DisplayAvatar(p.submittedBy.id,
                                                                                    p.submittedBy.avatarLocator));
                m_loadingDelegates.Add(( ) => submittorDisplay.avatar.DisplayLoading());
            }
            else
            {
                m_missingDisplayParsers.Add((ModProfile p, ref ModDisplayData d) =>
                {
                    ImageDisplayData avatarData = new ImageDisplayData()
                    {
                        userId = -1,
                        mediaType = ImageDisplayData.MediaType.UserAvatar,
                        fileName = string.Empty,
                        texture = null,
                    };
                    if(p.submittedBy != null)
                    {
                        avatarData.userId = p.submittedBy.id;

                        if(p.submittedBy.avatarLocator != null)
                        {
                            avatarData.fileName = p.submittedBy.avatarLocator.fileName;
                        }
                    }

                    d.submittorAvatar = avatarData;
                });
            }

            // - build -
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
            else
            {
                m_missingDisplayParsers.Add((ModProfile p, ref ModDisplayData d) =>
                {
                    d.currentBuild = ModfileDisplayData.CreateFromModfile(p.currentBuild);
                });
            }

            // - tags -
            // NOTE(@jackson): tags has no display/missing parse delegate as it requires categories
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

                m_loadingDelegates.Add(( ) => tagsDisplay.DisplayLoading());
            }

            // - stats -
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

            // - download -
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

        // NOTE(@jackson): This ignores the Logo as it'll be set anyway
        private void ParseProfileMedia(ModProfile profile, ref ModDisplayData data)
        {
            // - early out -
            if(profile.media == null)
            {
                data.media = new ImageDisplayData[0];
                return;
            }

            // - parse -
            List<ImageDisplayData> mediaList = new List<ImageDisplayData>();

            if(profile.media.galleryImageLocators != null
               && profile.media.galleryImageLocators.Length > 0)
            {
                foreach(GalleryImageLocator locator in profile.media.galleryImageLocators)
                {
                    ImageDisplayData imageData = new ImageDisplayData()
                    {
                        modId = profile.id,
                        mediaType = ImageDisplayData.MediaType.ModGalleryImage,
                        fileName = locator.fileName,
                        texture = null,
                    };

                    mediaList.Add(imageData);
                }
            }

            if(profile.media.youTubeURLs != null
               && profile.media.youTubeURLs.Length > 0)
            {
                foreach(string url in profile.media.youTubeURLs)
                {
                    ImageDisplayData imageData = new ImageDisplayData()
                    {
                        modId = profile.id,
                        mediaType = ImageDisplayData.MediaType.YouTubeThumbnail,
                        youTubeId = Utility.ExtractYouTubeIdFromURL(url),
                        texture = null,
                    };

                    mediaList.Add(imageData);
                }
            }

            data.media = mediaList.ToArray();
        }

        public void DisplayMod(ModProfile profile,
                               ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isSubscribed,
                               bool isModEnabled)
        {
            Debug.Assert(profile != null);

            m_data = new ModDisplayData();

            foreach(DisplayProfileDelegate displayDelegate in m_displayDelegates)
            {
                displayDelegate(profile);
            }
            foreach(ProfileParserDelegate parserDelegate in m_missingDisplayParsers)
            {
                parserDelegate(profile, ref m_data);
            }

            // - tags -
            if(tagsDisplay != null)
            {
                tagsDisplay.DisplayTags(profile, tagCategories);
            }
            else
            {
                m_data.tags = ModTagDisplayData.GenerateArray(profile.tagNames, tagCategories);
            }

            // - stats -
            ModStatisticsDisplayData statsData;
            if(statistics == null)
            {
                statsData = new ModStatisticsDisplayData()
                {
                    modId = profile.id,
                };
            }
            else
            {
                statsData = ModStatisticsDisplayData.CreateFromStatistics(statistics);
            }

            if(statisticsDisplay != null)
            {
                statisticsDisplay.data = statsData;
            }
            else
            {
                m_data.statistics = statsData;
            }

            // TODO(@jackson): DownloadDisplay

            // - subscribed -
            if(subscriptionDisplay.isSubscribed != null)
            {
                subscriptionDisplay.isSubscribed.SetActive(isSubscribed);
            }
            if(subscriptionDisplay.notSubscribed != null)
            {
                subscriptionDisplay.notSubscribed.SetActive(!isSubscribed);
            }
            m_data.isSubscribed = isSubscribed;

            // - enabled -
            if(modEnabledDisplay.isEnabled != null)
            {
                modEnabledDisplay.isEnabled.SetActive(isModEnabled);
            }
            if(modEnabledDisplay.isDisabled != null)
            {
                modEnabledDisplay.isDisabled.SetActive(!isModEnabled);
            }
            m_data.isModEnabled = isModEnabled;

            #if UNITY_EDITOR
            if(Application.isPlaying)
            {
                // updates for inspection convenience
                GetData();
            }
            #endif
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
            if(!Application.isPlaying)
            {
                CollectDelegates();
            }

            SetData(m_data);
        }
        #endif
    }
}
