using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModRating
    {
        // ---------[ CONSTANTS ]---------
        public const int POSITIVE_VALUE = 1;
        public const int NEGATIVE_VALUE = -1;

        // ---------[ FIELDS ]---------
        /// <summary>Unique game id.</summary>
        [JsonProperty("game_id")]
        public int gameId;

        /// <summary>Unique mod id.</summary>
        [JsonProperty("mod_id")]
        public int modId;

        /// <summary>Is it a positive or negative rating.</summary>
        [JsonProperty("rating")]
        public int ratingValue;

        /// <summary>Unix timestamp of date rating was submitted.</summary>
        [JsonProperty("date_added")]
        public int dateAdded;
    }
}
