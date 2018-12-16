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

        [Header("UI Components")]
        public ModProfileDisplayComponent           profileDisplay;
        public UserView                             creatorView;
        public ModLogoDisplayComponent              logoDisplay;
        public ModMediaCollectionDisplayComponent   mediaContainer;
        public ModfileDisplayComponent              buildDisplay;
        public ModTagCollectionDisplayComponent     tagsDisplay;
        public ModStatisticsDisplayComponent        statisticsDisplay;
        public ModBinaryRequestDisplay              downloadDisplay;

        [Header("Display Data")]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();

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
            foreach(PullDisplayDataDelegate pullDelegate in m_pullDataDelegates)
            {
                pullDelegate(ref m_data);
            }

            return m_data;
        }

        private void SetData(ModDisplayData value)
        {
            m_data = value;
            foreach(PushDisplayDataDelegate pushDelegate in m_pushDataDelegates)
            {
                pushDelegate(value);
            }
        }

        // --- FUNCTION DELEGATES ---
        private delegate void PullDisplayDataDelegate(ref ModDisplayData data);
        private List<PullDisplayDataDelegate> m_pullDataDelegates = new List<PullDisplayDataDelegate>();

        private delegate void PushDisplayDataDelegate(ModDisplayData data);
        private List<PushDisplayDataDelegate> m_pushDataDelegates = new List<PushDisplayDataDelegate>();

        private delegate void DisplayProfileDelegate(ModProfile profile);
        private List<DisplayProfileDelegate> m_displayDelegates = new List<DisplayProfileDelegate>();


        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            m_pullDataDelegates.Clear();
            m_pushDataDelegates.Clear();
            m_displayDelegates.Clear();

            if(profileDisplay != null)
            {
                profileDisplay.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.profile = profileDisplay.data;
                });
                m_pushDataDelegates.Add((d) =>
                {
                    profileDisplay.data = d.profile;
                });

                m_displayDelegates.Add((p) => profileDisplay.DisplayProfile(p));
            }
            if(mediaContainer != null)
            {
                mediaContainer.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.media = mediaContainer.data.ToArray();
                });
                m_pushDataDelegates.Add((d) =>
                {
                    mediaContainer.data = d.media;
                });

                m_displayDelegates.Add((p) => mediaContainer.DisplayMedia(p));
            }
            // NOTE(@jackson): Logo Data overrides Media Container Logo Data
            if(logoDisplay != null)
            {
                logoDisplay.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.SetLogo(logoDisplay.data);
                });
                m_pushDataDelegates.Add((d) =>
                {
                    logoDisplay.data = d.GetLogo();
                });

                m_displayDelegates.Add((p) => logoDisplay.DisplayLogo(p.id, p.logoLocator));
            }
            if(creatorView != null)
            {
                creatorView.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.submittedBy = creatorView.data;
                });
                m_pushDataDelegates.Add((d) =>
                {
                    creatorView.data = d.submittedBy;
                });

                m_displayDelegates.Add((p) => creatorView.DisplayUser(p.submittedBy));
            }
            if(buildDisplay != null)
            {
                buildDisplay.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.currentBuild = buildDisplay.data;
                });
                m_pushDataDelegates.Add((d) =>
                {
                    buildDisplay.data = d.currentBuild;
                });

                m_displayDelegates.Add((p) => buildDisplay.DisplayModfile(p.currentBuild));
            }

            if(tagsDisplay != null)
            {
                tagsDisplay.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.tags = tagsDisplay.data.ToArray();
                });
                m_pushDataDelegates.Add((d) =>
                {
                    tagsDisplay.data = d.tags;
                });

                // NOTE(@jackson): tags has no display delegate as it requires categories
            }

            if(statisticsDisplay != null)
            {
                statisticsDisplay.Initialize();

                m_pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.statistics = statisticsDisplay.data;
                });
                m_pushDataDelegates.Add((d) =>
                {
                    statisticsDisplay.data = d.statistics;
                });
            }

            if(downloadDisplay != null)
            {
                downloadDisplay.Initialize();

                // m_pullDataDelegates.Add((ref ModDisplayData d) =>
                // {
                //     d.submittedBy = creatorView.data;
                // });
                // m_pushDataDelegates.Add((d) =>
                // {
                //     creatorView.data = d.submittedBy;
                // });
            }
        }

        public void DisplayMod(ModProfile profile,
                               ModStatistics statistics,
                               IEnumerable<ModTagCategory> tagCategories)
        {
            Debug.Assert(profile != null);

            m_data = new ModDisplayData()
            {
                modId = profile.id,
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
        }

        // ---------[ EVENTS ]---------
        public void NotifyClicked()
        {
            if(onClick != null)
            {
                onClick(this);
            }
        }
    }
}
