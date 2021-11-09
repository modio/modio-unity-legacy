using System.Runtime.Serialization;

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
        [JsonProperty("rating_enum")]
        public ModRatingValue ratingValue;

        /// <summary>Unix timestamp of date rating was submitted.</summary>
        [JsonProperty("date_added")]
        public int dateAdded;


        // ---------[ API SERIALIZATION ]---------
        /// <summary>The value provided by the API to represent a negative rating.</summary>
        public const int APIOBJECT_VALUEINT_NEGATIVERATING = -1;
        /// <summary>The value provided by the API to represent a positive rating.</summary>
        public const int APIOBJECT_VALUEINT_POSITIVERATING = 1;

        /// <summary>Converts the API integer value to the rating value enum.</summary>
        public static ModRatingValue ConvertIntToEnum(int valueInteger)
        {
            switch(valueInteger)
            {
                case ModRating.APIOBJECT_VALUEINT_NEGATIVERATING:
                {
                    return ModRatingValue.Negative;
                }
                case ModRating.APIOBJECT_VALUEINT_POSITIVERATING:
                {
                    return ModRatingValue.Positive;
                }
                default:
                {
                    return ModRatingValue.None;
                }
            }
        }

        /// <summary>Converts a rating value enum to the API integer value.</summary>
        public static int ConvertEnumToInt(ModRatingValue valueEnum)
        {
            switch(valueEnum)
            {
                case ModRatingValue.Negative:
                {
                    return ModRating.APIOBJECT_VALUEINT_NEGATIVERATING;
                }
                case ModRatingValue.Positive:
                {
                    return ModRating.APIOBJECT_VALUEINT_POSITIVERATING;
                }
                default:
                {
                    return 0;
                }
            }
        }

        [JsonProperty("rating")]
        private int? _apiRatingValue = null;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(this._apiRatingValue == null)
            {
                return;
            }

            this.ratingValue = ModRating.ConvertIntToEnum((int)this._apiRatingValue);
        }
    }
}
