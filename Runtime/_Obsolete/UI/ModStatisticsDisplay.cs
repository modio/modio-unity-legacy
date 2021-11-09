using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [Obsolete("Use ModStatisticsFieldDisplay components instead.")]
    public class ModStatisticsDisplay : ModStatisticsDisplayComponent
    {
        // ---------[ FIELDS ]---------
        public override event Action<ModStatisticsDisplayComponent> onClick;

        [Header("UI Components")]
        public Text popularityRankDisplay;
        public Text popularityModCountDisplay;
        public Text downloadCountDisplay;
        public Text subscriberCountDisplay;
        public Text ratingCountDisplay;
        public Text ratingPositiveCountDisplay;
        public Text ratingPositivePercentageDisplay;
        public Text ratingNegativeCountDisplay;
        public Text ratingNegativePercentageDisplay;
        public Text ratingWeightedAggregateDisplay;
        public Text ratingAsTextDisplay;

        [Header("Display Data")]
        [SerializeField]
        private ModStatisticsDisplayData m_data = new ModStatisticsDisplayData();
        private List<TextLoadingOverlay> m_loadingOverlays = null;

        private delegate string GetDisplayString(ModStatisticsDisplayData data);
        private Dictionary<Text, GetDisplayString> m_displayMapping = null;

        // --- ACCESSORS ---
        public override ModStatisticsDisplayData data
        {
            get {
                return m_data;
            }
            set {
                m_data = value;
                PresentData();
            }
        }

        private void PresentData()
        {
#if UNITY_EDITOR
            if(!Application.isPlaying && this.m_displayMapping == null)
            {
                return;
            }
#endif

            if(this.m_displayMapping == null)
            {
                this.Initialize();
            }

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }
            foreach(var kvp in m_displayMapping) { kvp.Key.text = kvp.Value(m_data); }
        }

        // ---------[ INITIALIZATION ]---------
        public override void Initialize()
        {
            if(this.m_displayMapping == null)
            {
                BuildDisplayMap();
                CollectLoadingOverlays();
            }
        }

        private void BuildDisplayMap()
        {
            m_displayMapping = new Dictionary<Text, GetDisplayString>();

            if(popularityRankDisplay != null)
            {
                m_displayMapping.Add(
                    popularityRankDisplay,
                    (s) => ValueFormatting.AbbreviateInteger(s.popularityRankPosition, "0.0"));
            }
            if(popularityModCountDisplay != null)
            {
                m_displayMapping.Add(
                    popularityModCountDisplay,
                    (s) => ValueFormatting.AbbreviateInteger(s.popularityRankModCount, "0.0"));
            }
            if(downloadCountDisplay != null)
            {
                m_displayMapping.Add(downloadCountDisplay, (s) => ValueFormatting.AbbreviateInteger(
                                                               s.downloadCount, "0.0"));
            }
            if(subscriberCountDisplay != null)
            {
                m_displayMapping.Add(
                    subscriberCountDisplay,
                    (s) => ValueFormatting.AbbreviateInteger(s.subscriberCount, "0.0"));
            }
            if(ratingCountDisplay != null)
            {
                m_displayMapping.Add(ratingCountDisplay, (s) => ValueFormatting.AbbreviateInteger(
                                                             s.ratingCount, "0.0"));
            }
            if(ratingPositiveCountDisplay != null)
            {
                m_displayMapping.Add(
                    ratingPositiveCountDisplay,
                    (s) => ValueFormatting.AbbreviateInteger(s.ratingPositiveCount, "0.0"));
            }
            if(ratingPositivePercentageDisplay != null)
            {
                m_displayMapping.Add(
                    ratingPositivePercentageDisplay,
                    (s) => (s.ratingCount > 0
                                ? (100f * (float)s.ratingPositiveCount / (float)s.ratingCount)
                                          .ToString("0")
                                      + "%"
                                : "--"));
            }
            if(ratingNegativeCountDisplay != null)
            {
                m_displayMapping.Add(
                    ratingNegativeCountDisplay,
                    (s) => ValueFormatting.AbbreviateInteger(s.ratingNegativeCount, "0.0"));
            }
            if(ratingNegativePercentageDisplay != null)
            {
                m_displayMapping.Add(
                    ratingNegativePercentageDisplay,
                    (s) => (s.ratingCount > 0
                                ? (100f * (float)s.ratingNegativeCount / (float)s.ratingCount)
                                          .ToString("0")
                                      + "%"
                                : "--"));
            }
            if(ratingWeightedAggregateDisplay != null)
            {
                m_displayMapping.Add(ratingWeightedAggregateDisplay,
                                     (s) => (100f * s.ratingWeightedAggregate).ToString("0") + "%");
            }
            if(ratingAsTextDisplay != null)
            {
                m_displayMapping.Add(ratingAsTextDisplay, (s) => s.ratingDisplayText);
            }
        }

        private void CollectLoadingOverlays()
        {
            TextLoadingOverlay[] childLoadingOverlays =
                this.gameObject.GetComponentsInChildren<TextLoadingOverlay>(true);
            List<Text> textDisplays = new List<Text>(m_displayMapping.Keys);

            m_loadingOverlays = new List<TextLoadingOverlay>();
            foreach(TextLoadingOverlay loadingOverlay in childLoadingOverlays)
            {
                if(textDisplays.Contains(loadingOverlay.textDisplayComponent))
                {
                    m_loadingOverlays.Add(loadingOverlay);
                }
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public override void DisplayStatistics(ModStatistics statistics)
        {
            Debug.Assert(statistics != null);

            ModStatisticsDisplayData statsData =
                ModStatisticsDisplayData.CreateFromStatistics(statistics);
            m_data = statsData;
            PresentData();
        }

        public override void DisplayLoading()
        {
            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(true);
            }

            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.text = string.Empty;
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if(this != null)
                {
                    BuildDisplayMap();
                    CollectLoadingOverlays();
                    PresentData();
                }
            };
        }
#endif
    }
}
