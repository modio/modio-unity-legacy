namespace ModIO.API
{
    public class AddModRatingParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        /// <summary>The authenticated users mod rating.</summary>
        public ModRatingValue ratingValue
        {
            set {
#if DEBUG
                if(value == ModRatingValue.None)
                {
                    UnityEngine.Debug.LogError("[mod.io] Submitting a rating value of \'None\' is"
                                               + " currently not supported by the mod.io API.");
                    return;
                }
#endif

                int intValue = AddModRatingParameters.ConvertEnumToInt(value);
                this.SetStringValue("rating", intValue.ToString());
            }
        }

        // ---------[ API SERIALIZATION ]---------
        /// <summary>The value provided by the API to represent a negative rating.</summary>
        public const int APIVALUE_NEGATIVERATING = -1;
        /// <summary>The value provided by the API to represent a positive rating.</summary>
        public const int APIVALUE_POSITIVERATING = 1;

        /// <summary>Converts the API integer value to the rating value enum.</summary>
        public static ModRatingValue ConvertIntToEnum(int valueInteger)
        {
            switch(valueInteger)
            {
                case AddModRatingParameters.APIVALUE_NEGATIVERATING:
                {
                    return ModRatingValue.Negative;
                }
                case AddModRatingParameters.APIVALUE_POSITIVERATING:
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
                    return AddModRatingParameters.APIVALUE_NEGATIVERATING;
                }
                case ModRatingValue.Positive:
                {
                    return AddModRatingParameters.APIVALUE_POSITIVERATING;
                }
                default:
                {
                    return 0;
                }
            }
        }

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("Use ratingValue instead.")]
        public int rating
        {
            set {
                this.SetStringValue("rating", value.ToString());
            }
        }
    }
}
