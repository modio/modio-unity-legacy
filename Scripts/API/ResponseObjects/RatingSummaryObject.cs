using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct RatingSummaryObject
    {
        // ---------[ FIELDS ]---------
        /// <summary>Number of times this item has been rated.</summary>
        [JsonProperty("total_ratings")]
        public int totalRatingCount;

        /// <summary>Number of positive ratings.</summary>
        [JsonProperty("positive_ratings")]
        public int positiveRatingCount;

        /// <summary>Number of negative ratings.</summary>
        [JsonProperty("negative_ratings")]
        public int negativeRatingCount;

        /// <summary>Overall rating of this item calculated using the
        /// <a href="http://www.evanmiller.org/how-not-to-sort-by-average-rating.html">
        /// Wilson score confidence interval</a>.
        /// This column is good to sort on, as it will order items based
        /// on number of ratings and will place items with many positive
        /// ratings above those with a higher score but fewer
        /// ratings.</summary>
        [JsonProperty("weighted_aggregate")]
        public float weightedAggregate;

        /// <summary>Textual representation of the rating</summary>
        [JsonProperty("display_text")]
        public string displayText;
    }
}