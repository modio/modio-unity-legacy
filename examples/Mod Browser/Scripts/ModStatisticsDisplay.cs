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

    [Header("Settings")]
    public GameObject textLoadingPrefab;

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

    private bool m_isInitialized = false;
    private Dictionary<Text, GetDisplayString> m_displayMapping = null;
    private List<GameObject> m_loadingInstances = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // asserts
        if(m_isInitialized)
        {
            #if DEBUG
            Debug.LogWarning("[mod.io] Once initialized, a ModStatisticsDisplay component cannot be re-initialized.");
            #endif

            return;
        }

        // - text elements -
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

        if(textLoadingPrefab != null)
        {
            m_loadingInstances = new List<GameObject>(m_displayMapping.Count);

            foreach(Text textComponent in m_displayMapping.Keys)
            {
                RectTransform textTransform = textComponent.GetComponent<RectTransform>();
                m_loadingInstances.Add(InstantiateTextLoadingPrefab(textTransform));
            }
        }
        else
        {
            m_loadingInstances = null;
        }

        m_isInitialized = true;
    }

    private GameObject InstantiateTextLoadingPrefab(RectTransform displayObjectTransform)
    {
        RectTransform parentRT = displayObjectTransform.parent as RectTransform;
        GameObject loadingGO = GameObject.Instantiate(textLoadingPrefab,
                                                      new Vector3(),
                                                      Quaternion.identity,
                                                      parentRT);

        RectTransform loadingRT = loadingGO.transform as RectTransform;
        loadingRT.anchorMin = displayObjectTransform.anchorMin;
        loadingRT.anchorMax = displayObjectTransform.anchorMax;
        loadingRT.offsetMin = displayObjectTransform.offsetMin;
        loadingRT.offsetMax = displayObjectTransform.offsetMax;

        return loadingGO;
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayStatistics(ModStatistics statistics)
    {
        Debug.Assert(statistics != null);

        m_modId = statistics.modId;

        foreach(var kvp in m_displayMapping)
        {
            kvp.Key.text = kvp.Value(statistics);
            kvp.Key.gameObject.SetActive(true);
        }

        if(m_loadingInstances != null)
        {
            foreach(GameObject loadingInstance in m_loadingInstances)
            {
                loadingInstance.SetActive(false);
            }
        }
    }

    public void DisplayLoading()
    {
        if(m_loadingInstances != null)
        {
            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.gameObject.SetActive(false);
            }
            foreach(GameObject loadingInstance in m_loadingInstances)
            {
                loadingInstance.SetActive(true);
            }
        }
        else
        {
            foreach(Text textComponent in m_displayMapping.Keys)
            {
                textComponent.text = string.Empty;
            }
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
