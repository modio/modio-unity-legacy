namespace ModIO.UI
{
    [System.Serializable]
    public struct ModStatisticsDisplayData
    {
        public int      modId;
        public int      popularityRankPosition;
        public int      popularityRankModCount;
        public int      downloadCount;
        public int      subscriberCount;
        public int      ratingCount;
        public int      ratingPositiveCount;
        public int      ratingNegativeCount;
        public float    ratingWeightedAggregate;
        public string   ratingDisplayText;
        public int      dateExpires;
    }

    public abstract class ModStatisticsDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModStatisticsDisplayComponent> onClick;

        public abstract ModStatisticsDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayStatistics(ModStatistics statistics);
        public abstract void DisplayLoading();
    }
}
