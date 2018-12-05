using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class ModStatisticsDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public delegate void OnClickDelegate(ModStatisticsDisplay display,
                                             int modId);
        public event OnClickDelegate onClick;

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
        [SerializeField] private int m_modId = -1;

        // ---[ RUNTIME DATA ]---
        private delegate string GetDisplayString(ModStatistics statistics);

        private Dictionary<Text, GetDisplayString> m_displayMapping = null;
        private List<TextLoadingOverlay> m_loadingOverlays = null;

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            m_displayMapping = new Dictionary<Text, GetDisplayString>();

            if(popularityRankDisplay != null)
            {
                m_displayMapping.Add(popularityRankDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.popularityRankPosition));
            }
            if(popularityModCountDisplay != null)
            {
                m_displayMapping.Add(popularityModCountDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.popularityRankModCount));
            }
            if(downloadCountDisplay != null)
            {
                m_displayMapping.Add(downloadCountDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.downloadCount));
            }
            if(subscriberCountDisplay != null)
            {
                m_displayMapping.Add(subscriberCountDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.subscriberCount));
            }
            if(ratingCountDisplay != null)
            {
                m_displayMapping.Add(ratingCountDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.ratingCount));
            }
            if(ratingPositiveCountDisplay != null)
            {
                m_displayMapping.Add(ratingPositiveCountDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.ratingPositiveCount));
            }
            if(ratingPositivePercentageDisplay != null)
            {
                m_displayMapping.Add(ratingPositivePercentageDisplay,
                                     (s) => (s.ratingCount > 0
                                             ? (100f * (float)s.ratingPositiveCount / (float)s.ratingCount).ToString("0") + "%"
                                             : "~%"));
            }
            if(ratingNegativeCountDisplay != null)
            {
                m_displayMapping.Add(ratingNegativeCountDisplay,
                                     (s) => UIUtilities.ValueToDisplayString(s.ratingNegativeCount));
            }
            if(ratingNegativePercentageDisplay != null)
            {
                m_displayMapping.Add(ratingNegativePercentageDisplay,
                                     (s) => (s.ratingCount > 0
                                             ? (100f * (float)s.ratingNegativeCount / (float)s.ratingCount).ToString("0") + "%"
                                             : "~%"));
            }
            if(ratingWeightedAggregateDisplay != null)
            {
                m_displayMapping.Add(ratingWeightedAggregateDisplay,
                                     (s) => (100f * s.ratingWeightedAggregate).ToString("0") + "%");
            }
            if(ratingAsTextDisplay != null)
            {
                m_displayMapping.Add(ratingAsTextDisplay,
                                     (s) => s.ratingDisplayText);
            }

            TextLoadingOverlay[] childLoadingOverlays = this.gameObject.GetComponentsInChildren<TextLoadingOverlay>(true);
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
        public void DisplayStatistics(ModStatistics statistics)
        {
            Debug.Assert(statistics != null);

            m_modId = statistics.modId;

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }

            foreach(var kvp in m_displayMapping)
            {
                kvp.Key.text = kvp.Value(statistics);
                kvp.Key.enabled = true;
            }
        }

        public void DisplayLoading(int modId = -1)
        {
            m_modId = modId;

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(true);
            }

            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.enabled = false;
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this, m_modId);
            }
        }
    }
}
