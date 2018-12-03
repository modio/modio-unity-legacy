using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

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
    private List<LoadingDisplay> m_loadingDisplays = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        m_displayMapping = new Dictionary<Text, GetDisplayString>();

        if(popularityRankDisplay != null)
        {
            m_displayMapping.Add(popularityRankDisplay,
                                 (s) => ModBrowser.ValueToDisplayString(s.popularityRankPosition));
        }
        if(popularityModCountDisplay != null)
        {
            m_displayMapping.Add(popularityModCountDisplay,
                                 (s) => ModBrowser.ValueToDisplayString(s.popularityRankModCount));
        }
        if(downloadCountDisplay != null)
        {
            m_displayMapping.Add(downloadCountDisplay,
                                 (s) => ModBrowser.ValueToDisplayString(s.downloadCount));
        }
        if(subscriberCountDisplay != null)
        {
            m_displayMapping.Add(subscriberCountDisplay,
                                 (s) => ModBrowser.ValueToDisplayString(s.subscriberCount));
        }
        if(ratingCountDisplay != null)
        {
            m_displayMapping.Add(ratingCountDisplay,
                                 (s) => ModBrowser.ValueToDisplayString(s.ratingCount));
        }
        if(ratingPositiveCountDisplay != null)
        {
            m_displayMapping.Add(ratingPositiveCountDisplay,
                                 (s) => ModBrowser.ValueToDisplayString(s.ratingPositiveCount));
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
                                 (s) => ModBrowser.ValueToDisplayString(s.ratingNegativeCount));
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

        m_loadingDisplays = new List<LoadingDisplay>();
        foreach(Text textDisplay in m_displayMapping.Keys)
        {
            m_loadingDisplays.AddRange(textDisplay.gameObject.GetComponentsInChildren<LoadingDisplay>(true));
        }
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayStatistics(ModStatistics statistics)
    {
        Debug.Assert(statistics != null);

        m_modId = statistics.modId;

        foreach(LoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(false);
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

        foreach(LoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(true);
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
