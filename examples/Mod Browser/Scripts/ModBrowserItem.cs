using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

// TODO(@jackson): Add "Display Loading Profile" functionality
public class ModBrowserItem : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Serializable]
    public struct InspectorHelper_ProfileElements
    {
        public RectTransform logoContainer;
        public Text name;
        public Text creator;
        public Text dateAdded;
        public Text dateUpdated;
        public Text dateLive;
        public Text summary;
        public LayoutGroup tagContainer;
    }

    [Serializable]
    public struct InspectorHelper_ModfileElements
    {
        public Text dateAdded;
        public Text fileSize;
        public Text version;
    }

    [Serializable]
    public struct InspectorHelper_StatisticsElements
    {
        public Text popularityRankPosition;
        public Text downloadCount;
        public Text subscriberCount;
        public Text ratingsTotalCount;
        public Text ratingsPositiveCount;
        public Text ratingsNegativeCount;
        public Text ratingsPositivePercentage;
        public Text ratingsNegativePercentage;
        public Text ratingsWeightedAggregate;
        public Text ratingsDisplayText;
    }

    // ---[ EVENTS ]---
    public event Action<ModBrowserItem> onClick;

    // ---[ UI ]---
    [Header("Settings")]
    public GameObject logoLoadingPrefab;
    public GameObject tagBadgePrefab;
    public LogoSize logoVersion;
    [Range(0.5f, 1.0f)]
    public float minimumScaleFactor = 1f;
    [Range(1.0f, 2.0f)]
    public float maximumScaleFactor = 1f;

    [Header("UI Components")]
    public InspectorHelper_ProfileElements profileDisplay;
    public InspectorHelper_ModfileElements modfileDisplay;
    public InspectorHelper_StatisticsElements statisticsDisplay;

    // ---[ RUNTIME DATA ]---
    [Header("Runtime Data")]
    public int index;
    public ModProfile profile;
    public ModStatistics statistics;
    public GameObject logoPlaceholderInstance;
    public Image modLogo;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        if(profileDisplay.logoContainer != null)
        {
            if(logoPlaceholderInstance == null || modLogo == null)
            {
                foreach(Transform t in profileDisplay.logoContainer)
                {
                    UnityEngine.Object.Destroy(t.gameObject);
                }

                logoPlaceholderInstance = UnityEngine.Object.Instantiate(logoLoadingPrefab, profileDisplay.logoContainer) as GameObject;

                GameObject modLogo_go = new GameObject("ModLogo");

                RectTransform logoTransfrom = modLogo_go.AddComponent<RectTransform>();
                logoTransfrom.SetParent(profileDisplay.logoContainer);
                logoTransfrom.anchorMin = new Vector2(0f, 0f);
                logoTransfrom.anchorMax = new Vector2(1f, 1f);
                logoTransfrom.offsetMin = new Vector2(0f, 0f);
                logoTransfrom.offsetMax = new Vector2(0f, 0f);

                modLogo = modLogo_go.AddComponent<Image>();
            }

            logoPlaceholderInstance.gameObject.SetActive(false);
            modLogo.gameObject.SetActive(false);
        }
    }

    public void UpdateProfileUIComponents()
    {
        Debug.Assert(this.profile != null,
                     "[mod.io] Assign the mod profile before updating the profile UI components.");

        // - text -
        if(profileDisplay.name != null)
        {
            profileDisplay.name.text = profile.name;
        }
        if(profileDisplay.creator != null)
        {
            profileDisplay.creator.text = profile.submittedBy.username;
        }
        if(profileDisplay.dateAdded != null)
        {
            profileDisplay.dateAdded.text = ServerTimeStamp.ToLocalDateTime(profile.dateAdded).ToString();
        }
        if(profileDisplay.dateUpdated != null)
        {
            profileDisplay.dateUpdated.text = ServerTimeStamp.ToLocalDateTime(profile.dateUpdated).ToString();
        }
        if(profileDisplay.dateLive != null)
        {
            profileDisplay.dateLive.text = ServerTimeStamp.ToLocalDateTime(profile.dateLive).ToString();
        }
        if(profileDisplay.summary != null)
        {
            profileDisplay.summary.text = profile.summary;
        }

        // - logo -
        if(profileDisplay.logoContainer != null)
        {
            logoPlaceholderInstance.SetActive(true);
            modLogo.gameObject.SetActive(false);
            // TODO(@jackson): onError
            ModManager.GetModLogo(profile, logoVersion,
                                  ApplyModLogo, null);
        }

        // - tags -
        if(profileDisplay.tagContainer != null)
        {
            foreach(Transform t in profileDisplay.tagContainer.transform)
            {
                GameObject.Destroy(t.gameObject);
            }

            bool isTextInChild = (tagBadgePrefab.GetComponent<Text>() == null);

            foreach(string tagName in profile.tagNames)
            {
                GameObject tag_go = GameObject.Instantiate(tagBadgePrefab, profileDisplay.tagContainer.transform) as GameObject;
                tag_go.name = "Tag: " + tagName;

                Text tagText;
                if(isTextInChild)
                {
                    tagText = tag_go.GetComponentInChildren<Text>();
                }
                else
                {
                    tagText = tag_go.GetComponent<Text>();
                }

                tagText.text = tagName;
            }
        }

        // - modfile -
        if(modfileDisplay.dateAdded != null)
        {
            modfileDisplay.dateAdded.text = ServerTimeStamp.ToLocalDateTime(profile.activeBuild.dateAdded).ToString();
        }
        if(modfileDisplay.fileSize != null)
        {
            modfileDisplay.fileSize.text = ModBrowser.ByteCountToDisplayString(profile.activeBuild.fileSize);
        }
        if(modfileDisplay.version != null)
        {
            modfileDisplay.version.text = profile.activeBuild.version;
        }
    }

    public void UpdateStatisticsUIComponents()
    {
        bool isLoading = (statistics == null);
        string displayText = (isLoading ? "..." : string.Empty);

        if(statisticsDisplay.popularityRankPosition != null)
        {
            if(!isLoading)
            {
                displayText = "#" + statistics.popularityRankPosition;
            }

            statisticsDisplay.popularityRankPosition.text = displayText;
        }
        if(statisticsDisplay.downloadCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.downloadCount);
            }

            statisticsDisplay.downloadCount.text = displayText;
        }
        if(statisticsDisplay.subscriberCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.subscriberCount);
            }

            statisticsDisplay.subscriberCount.text = displayText;
        }
        if(statisticsDisplay.ratingsTotalCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.ratingsTotalCount);
            }
            statisticsDisplay.ratingsTotalCount.text = displayText;
        }
        if(statisticsDisplay.ratingsPositiveCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.ratingsPositiveCount);
            }
            statisticsDisplay.ratingsPositiveCount.text = displayText;
        }
        if(statisticsDisplay.ratingsNegativeCount != null)
        {
            if(!isLoading)
            {
                displayText = ModBrowser.ValueToDisplayString(statistics.ratingsNegativeCount);
            }
            statisticsDisplay.ratingsNegativeCount.text = displayText;
        }
        if(statisticsDisplay.ratingsPositivePercentage != null)
        {
            if(!isLoading)
            {
                displayText = ((float)statistics.ratingsPositiveCount / (float)statistics.ratingsTotalCount).ToString("0.0") + "%";
            }
            statisticsDisplay.ratingsPositivePercentage.text = displayText;
        }
        if(statisticsDisplay.ratingsNegativePercentage != null)
        {
            if(!isLoading)
            {
                displayText = ((float)statistics.ratingsNegativeCount / (float)statistics.ratingsTotalCount).ToString("0.0") + "%";
            }
            statisticsDisplay.ratingsNegativePercentage.text = displayText;
        }
        if(statisticsDisplay.ratingsWeightedAggregate != null)
        {
            if(!isLoading)
            {
                displayText = (statistics.ratingsWeightedAggregate * 100f).ToString("0.0") + "%";
            }
            statisticsDisplay.ratingsWeightedAggregate.text = displayText;
        }
        if(statisticsDisplay.ratingsDisplayText != null)
        {
            if(!isLoading)
            {
                displayText = statistics.ratingsDisplayText;
            }
            statisticsDisplay.ratingsDisplayText.text = displayText;
        }
    }

    private void ApplyModLogo(Texture2D logoTexture)
    {
        #if UNITY_EDITOR
        if(!Application.isPlaying) { return; }
        #endif

        Debug.Assert(modLogo != null);
        Debug.Assert(logoTexture != null);

        if(modLogo.sprite != null)
        {
            if(modLogo.sprite.texture != null)
            {
                UnityEngine.Object.Destroy(modLogo.sprite.texture);
            }

            UnityEngine.Object.Destroy(modLogo.sprite);
        }

        modLogo.sprite = ModBrowser.CreateSpriteWithTexture(logoTexture);

        modLogo.gameObject.SetActive(true);
        logoPlaceholderInstance.gameObject.SetActive(false);
    }

    public void Clicked()
    {
        if(onClick != null)
        {
            onClick(this);
        }
    }
}
