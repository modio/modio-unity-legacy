namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
    [System.Serializable]
    public struct ModStatisticsDisplayData
    {
        public int modId;
        public int popularityRankPosition;
        public int popularityRankModCount;
        public int downloadCount;
        public int subscriberCount;
        public int ratingCount;
        public int ratingPositiveCount;
        public int ratingNegativeCount;
        public float ratingWeightedAggregate;
        public string ratingDisplayText;
        public int dateExpires;

        public static ModStatisticsDisplayData CreateFromStatistics(ModStatistics statistics)
        {
            ModStatisticsDisplayData statisticsData = new ModStatisticsDisplayData() {
                modId = statistics.modId,
                popularityRankPosition = statistics.popularityRankPosition,
                popularityRankModCount = statistics.popularityRankModCount,
                downloadCount = statistics.downloadCount,
                subscriberCount = statistics.subscriberCount,
                ratingCount = statistics.ratingCount,
                ratingPositiveCount = statistics.ratingPositiveCount,
                ratingNegativeCount = statistics.ratingNegativeCount,
                ratingWeightedAggregate = statistics.ratingWeightedAggregate,
                ratingDisplayText = statistics.ratingDisplayText,
                dateExpires = statistics.dateExpires,
            };

            return statisticsData;
        }
    }

    [System.Obsolete("No longer supported.")]
    public abstract class ModStatisticsDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModStatisticsDisplayComponent> onClick;

        public abstract ModStatisticsDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayStatistics(ModStatistics statistics);
        public abstract void DisplayLoading();
    }
}
