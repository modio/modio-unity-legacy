using System;

namespace ModIO.API
{
    [Serializable]
    public struct RatingSummaryObject : IEquatable<RatingSummaryObject>
    {
        // - Fields -
        public int total_ratings;   // Number of times this item has been rated.
        public int positive_ratings;    // Number of positive ratings.
        public int negative_ratings;    // Number of negative ratings.
        public int percentage_positive; // Number of positive ratings, divided by the total ratings to determine it’s percentage score.
        public float weighted_aggregate; // Overall rating of this item calculated using the Wilson score confidence interval. This column is good to sort on, as it will order items based on number of ratings and will place items with many positive ratings above those with a higher score but fewer ratings.
        public string display_text; // Textual representation of the rating in format:

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.total_ratings
                    ^ this.percentage_positive);
        }

        public override bool Equals(object obj)
        {
            return (obj is RatingSummaryObject
                    && this.Equals((RatingSummaryObject)obj));
        }

        public bool Equals(RatingSummaryObject other)
        {
            return(this.total_ratings.Equals(other.total_ratings)
                   && this.positive_ratings.Equals(other.positive_ratings)
                   && this.negative_ratings.Equals(other.negative_ratings)
                   && this.percentage_positive.Equals(other.percentage_positive)
                   && this.weighted_aggregate.Equals(other.weighted_aggregate)
                   && this.display_text.Equals(other.display_text));
        }
    }
}