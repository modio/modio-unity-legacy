using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModStatistics
    {
        // ---------[ FIELDS ]---------
        /// <summary>Mod this object references.</summary>
        [JsonProperty("mod_id")]
        public int modId;

        /// <summary>Current rank of the mod.</summary>
        [JsonProperty("popularity_rank_position")]
        public int popularityRankPosition;

        /// <summary>Total mods counted in this ranking statistic.</summary>
        [JsonProperty("popularity_rank_total_mods")]
        public int popularityRankModCount;

        /// <summary>Number of total mod downloads.</summary>
        [JsonProperty("downloads_total")]
        public int downloadCount;

        /// <summary>Number of total users who have subscribed to the mod.</summary>
        [JsonProperty("subscribers_total")]
        public int subscriberCount;

        /// <summary>Number of times this item has been rated.</summary>
        [JsonProperty("ratings_total")]
        public int ratingCount;

        /// <summary>Number of positive ratings.</summary>
        [JsonProperty("ratings_positive")]
        public int ratingPositiveCount;

        /// <summary>Number of negative ratings.</summary>
        [JsonProperty("ratings_negative")]
        public int ratingNegativeCount;

        /// <summmary>An aggregrated rating beased on votes and number of votes</summary>
        /// <para>Overall rating of this item calculated using the
        /// <a href="http://www.evanmiller.org/how-not-to-sort-by-average-rating.html">Wilson score
        /// confidence interval</a>. This score is calculated through a combination of number of
        /// ratings submitted and the percentage of positive ratings, placing items with many
        /// positive ratings above those with a higher percentage but fewer ratings.</para>
        [JsonProperty("ratings_weighted_aggregate")]
        public float ratingWeightedAggregate;

        /// <summary>Friendly display text suggested for the rating.</summary>
        [JsonProperty("ratings_display_text")]
        public string ratingDisplayText;

        /// <summary>Unix timestamp until this object is considered out-dated.</summary>
        [JsonProperty("date_expires")]
        public int dateExpires;

        // --- Accessors ---
        /// <summary>Percentage of votes that are positive.</summary>
        [JsonIgnore]
        public float ratingPositivePercentage
        {
            get {
                return (this.ratingCount == 0
                            ? 0f
                            : (float)this.ratingPositiveCount / (float)this.ratingCount);
            }
        }
        /// <summary>Percentage of votes that are negative.</summary>
        [JsonIgnore] public float ratingNegativePercentage
        {
            get {
                return (this.ratingCount == 0
                            ? 0f
                            : (float)this.ratingNegativeCount / (float)this.ratingCount);
            }
        }
    }
}
