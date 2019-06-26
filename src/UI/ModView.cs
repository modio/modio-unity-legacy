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
        public event Action<ModView> ratePositiveRequested;
        public event Action<ModView> rateNegativeRequested;

        [Serializable]
        public struct SubmittorDisplay
        {
            public UserProfileDisplayComponent  profile;
            public ImageDisplay                 avatar;
        }

        [Serializable]
        public struct UserRatingDisplay
        {
            public StateToggleDisplay positive;
            public StateToggleDisplay negative;
        }

        [Header("UI Components")]
        public ModProfileDisplayComponent           profileDisplay;
        public SubmittorDisplay                     submittorDisplay;
        public ImageDisplay                         logoDisplay;
        public ModMediaCollectionDisplayComponent   mediaContainer;
        public ModfileDisplayComponent              buildDisplay;
        public ModTagCollectionDisplayComponent     tagsDisplay;
        public ModStatisticsDisplayComponent        statisticsDisplay;
        public DownloadDisplayComponent             downloadDisplay;
        public StateToggleDisplay                   subscriptionDisplay;
        public StateToggleDisplay                   modEnabledDisplay;
        public UserRatingDisplay                    userRatingDisplay;

        [Header("Display Data")]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();

        // --- RUNTIME DATA ---
        private Coroutine m_downloadDisplayCoroutine = null;

        // --- FUNCTION DELEGATES ---
        private delegate void GetDataDelegate(ref ModDisplayData data);
        private List<GetDataDelegate> m_getDelegates = null;

        private delegate void SetDataDelegate(ModDisplayData data);
        private List<SetDataDelegate> m_setDelegates = null;

        private delegate void DisplayProfileDelegate(ModProfile profile);
        private List<DisplayProfileDelegate> m_displayDelegates = null;

        private delegate void ProfileParserDelegate(ModProfile profile, ref ModDisplayData data);
        private List<ProfileParserDelegate> m_missingDisplayParsers = null;

        private delegate void DisplayLoadingDelegate();
        private List<DisplayLoadingDelegate> m_loadingDelegates = null;

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
            if(this.m_getDelegates == null)
            {
                CollectDelegates();
            }

            foreach(GetDataDelegate getDelegate in m_getDelegates)
            {
                getDelegate(ref m_data);
            }

            return m_data;
        }

        private void SetData(ModDisplayData value)
        {
            if(this.m_setDelegates == null)
            {
                CollectDelegates();
            }

            m_data = value;
            foreach(SetDataDelegate setDelegate in m_setDelegates)
            {
                setDelegate(value);
            }

            if(subscriptionDisplay != null)
            {
                subscriptionDisplay.isOn = value.isSubscribed;
            }

            if(modEnabledDisplay != null)
            {
                modEnabledDisplay.isOn = value.isModEnabled;
            }

            if(userRatingDisplay.positive != null)
            {
                userRatingDisplay.positive.isOn = (value.userRating == ModRatingValue.Positive);
            }
            if(userRatingDisplay.negative != null)
            {
                userRatingDisplay.negative.isOn = (value.userRating == ModRatingValue.Negative);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public void OnEnable()
        {
            GetData();
            FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(m_data.profile.modId,
                                                                                      m_data.currentBuild.modfileId);
            DisplayDownload(downloadInfo);
        }

        private void CollectDelegates()
        {
            m_getDelegates = new List<GetDataDelegate>();
            m_setDelegates = new List<SetDataDelegate>();
            m_displayDelegates = new List<DisplayProfileDelegate>();
            m_missingDisplayParsers = new List<ProfileParserDelegate>();
            m_loadingDelegates = new List<DisplayLoadingDelegate>();

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
                    d.logo = mediaContainer.logoData;
                    d.youTubeThumbnails = mediaContainer.youTubeData.ToArray();
                    d.galleryImages = mediaContainer.galleryData.ToArray();
                });
                m_setDelegates.Add((d) =>
                {
                    mediaContainer.logoData = d.logo;
                    mediaContainer.youTubeData = d.youTubeThumbnails;
                    mediaContainer.galleryData = d.galleryImages;
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
                    d.logo = logoDisplay.data;
                });
                m_setDelegates.Add((d) =>
                {
                    logoDisplay.data = d.logo;
                });

                m_displayDelegates.Add((p) => logoDisplay.DisplayLogo(p.id, p.logoLocator));
                m_loadingDelegates.Add(( ) => logoDisplay.DisplayLoading());
            }
            else
            {
                m_missingDisplayParsers.Add((ModProfile p, ref ModDisplayData d) =>
                {
                    ImageDisplayData logoData;
                    if(p.logoLocator != null)
                    {
                        logoData = ImageDisplayData.CreateForModLogo(p.id, p.logoLocator);
                    }
                    else
                    {
                        logoData = new ImageDisplayData();
                    }

                    d.logo = logoData;
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
                    ImageDisplayData avatarData;
                    if(p.submittedBy != null
                       && p.submittedBy.avatarLocator != null)
                    {
                        avatarData = ImageDisplayData.CreateForUserAvatar(p.submittedBy.id,
                                                                          p.submittedBy.avatarLocator);
                    }
                    else
                    {
                        avatarData = new ImageDisplayData();
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

        public void DisplayMod(ModProfile profile,
                               ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories,
                               bool isSubscribed,
                               bool isModEnabled,
                               ModRatingValue userRating = ModRatingValue.None)
        {
            Debug.Assert(profile != null);

            if(this.m_displayDelegates == null)
            {
                CollectDelegates();
            }

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

            // - download -
            FileDownloadInfo downloadInfo = DownloadClient.GetActiveModBinaryDownload(m_data.profile.modId,
                                                                                      m_data.currentBuild.modfileId);
            DisplayDownload(downloadInfo);

            // - subscribed -
            if(subscriptionDisplay != null)
            {
                subscriptionDisplay.isOn = isSubscribed;
            }
            m_data.isSubscribed = isSubscribed;

            // - enabled -
            if(modEnabledDisplay != null)
            {
                modEnabledDisplay.isOn = isModEnabled;
            }
            m_data.isModEnabled = isModEnabled;

            // - rating -
            if(userRatingDisplay.positive != null)
            {
                userRatingDisplay.positive.isOn = (userRating == ModRatingValue.Positive);
            }
            if(userRatingDisplay.negative != null)
            {
                userRatingDisplay.negative.isOn = (userRating == ModRatingValue.Negative);
            }
            m_data.userRating = userRating;


            #if UNITY_EDITOR
            if(Application.isPlaying)
            {
                // updates for inspection convenience
                GetData();
            }
            #endif
        }

        public void DisplayDownload(FileDownloadInfo downloadInfo)
        {
            bool activeDownload = (downloadInfo != null && !downloadInfo.isDone);

            if(downloadDisplay != null)
            {
                if(m_downloadDisplayCoroutine != null)
                {
                    this.StopCoroutine(m_downloadDisplayCoroutine);
                }

                downloadDisplay.gameObject.SetActive(activeDownload);

                if(this.isActiveAndEnabled
                   && activeDownload)
                {
                    downloadDisplay.DisplayDownload(downloadInfo);
                    m_downloadDisplayCoroutine = this.StartCoroutine(MonitorDownloadCoroutine(data.profile.modId));
                }

                m_data.binaryDownload = downloadDisplay.data;
            }
            else
            {
                DownloadDisplayData data = new DownloadDisplayData();
                data.bytesReceived = 0;
                data.bytesPerSecond = 0;
                data.bytesTotal = 0;
                data.isActive = activeDownload;

                if(downloadInfo != null)
                {
                    data.bytesReceived = (downloadInfo.request == null
                                          ? 0 : (Int64)downloadInfo.request.downloadedBytes);
                    data.bytesTotal = downloadInfo.fileSize;
                }
            }
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

        public void DisplayLoading()
        {
            if(this.m_loadingDelegates == null)
            {
                CollectDelegates();
            }

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

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    CollectDelegates();
                    SetData(m_data);
                }
            };
        }
        #endif

        // ---------[ OBSOLETE ]---------
        [Obsolete("No longer necessary.")]
        public void Initialize() {}
    }
}
