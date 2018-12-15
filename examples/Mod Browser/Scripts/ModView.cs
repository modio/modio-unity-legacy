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
        public ModLogoDisplayComponent              logoDisplay;
        public ModMediaCollectionDisplayComponent   mediaContainer;
        public UserDisplayComponent                 creatorDisplay;
        public ModStatisticsDisplayComponent        statisticsDisplay;
        public ModTagCollectionDisplayComponent     tagsDisplay;
        public ModfileDisplayComponent              buildDisplay;
        public ModBinaryRequestDisplay              downloadDisplay;

        [Header("Display Data")]
        [SerializeField] private ModDisplayData m_data = new ModDisplayData();

        private delegate void PullDisplayDataDelegate(ref ModDisplayData data);
        private List<PullDisplayDataDelegate> pullDataDelegates = new List<PullDisplayDataDelegate>();

        private delegate void PushDisplayDataDelegate(ModDisplayData data);
        private List<PushDisplayDataDelegate> pushDataDelegates = new List<PushDisplayDataDelegate>();

        // ---[ ACCESSORS ]---
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
            foreach(PullDisplayDataDelegate pullDelegate in pullDataDelegates)
            {
                pullDelegate(ref m_data);
            }

            return m_data;
        }

        private void SetData(ModDisplayData value)
        {
            m_data = value;
            foreach(PushDisplayDataDelegate pushDelegate in pushDataDelegates)
            {
                pushDelegate(value);
            }
        }

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            pullDataDelegates.Clear();
            pushDataDelegates.Clear();

            if(profileDisplay != null)
            {
                profileDisplay.Initialize();

                // pullDataDelegates.Add((ref ModDisplayData d) =>
                // {
                //     d.profile = profileDisplay.data;
                // });
                // pushDataDelegates.Add((d) =>
                // {
                //     profileDisplay.data = d.profile;
                // });
            }
            if(mediaContainer != null)
            {
                mediaContainer.Initialize();

                pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.media = mediaContainer.data.ToArray();
                });
                pushDataDelegates.Add((d) =>
                {
                    mediaContainer.data = d.media;
                });
            }
            // NOTE(@jackson): Logo Data overrides Media Container Logo Data
            if(logoDisplay != null)
            {
                logoDisplay.Initialize();

                pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.SetLogo(logoDisplay.data);
                });
                pushDataDelegates.Add((d) =>
                {
                    logoDisplay.data = d.GetLogo();
                });
            }
            if(creatorDisplay != null)
            {
                creatorDisplay.Initialize();

                pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.submittedBy = creatorDisplay.data;
                });
                pushDataDelegates.Add((d) =>
                {
                    creatorDisplay.data = d.submittedBy;
                });
            }
            if(tagsDisplay != null)
            {
                tagsDisplay.Initialize();

                pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.tags = tagsDisplay.data.ToArray();
                });
                pushDataDelegates.Add((d) =>
                {
                    tagsDisplay.data = d.tags;
                });
            }
            if(buildDisplay != null)
            {
                buildDisplay.Initialize();

                pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.currentBuild = buildDisplay.data;
                });
                pushDataDelegates.Add((d) =>
                {
                    buildDisplay.data = d.currentBuild;
                });
            }
            if(downloadDisplay != null)
            {
                downloadDisplay.Initialize();

                // pullDataDelegates.Add((ref ModDisplayData d) =>
                // {
                //     d.submittedBy = creatorDisplay.data;
                // });
                // pushDataDelegates.Add((d) =>
                // {
                //     creatorDisplay.data = d.submittedBy;
                // });
            }

            if(statisticsDisplay != null)
            {
                statisticsDisplay.Initialize();

                pullDataDelegates.Add((ref ModDisplayData d) =>
                {
                    d.statistics = statisticsDisplay.data;
                });
                pushDataDelegates.Add((d) =>
                {
                    statisticsDisplay.data = d.statistics;
                });
            }
        }

        // public void DisplayProfile(ModProfile profile,
        //                            IEnumerable<ModTagCategory> tagCategories)
        // {

        // }

        // public void DisplayStatistics(ModStatistics statistics)
        // {

        // }

        // public void DisplayLoading()
        // {

        // }

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
