namespace ModIO.API
{
    [System.Serializable]
    public struct RatingSummaryObject
    {
        // Number of times this item has been rated.
        public readonly int total_ratings;
        // Number of positive ratings.
        public readonly int positive_ratings;
        // Number of negative ratings.
        public readonly int negative_ratings;
        // Number of positive ratings, divided by the total ratings to determine it’s percentage score.
        public readonly int percentage_positive;
        // Overall rating of this item calculated using the Wilson score confidence interval. This column is good to sort on, as it will order items based on number of ratings and will place items with many positive ratings above those with a higher score but fewer ratings.
        public readonly float weighted_aggregate;
        // Textual representation of the rating in format
        public readonly string display_text;
    }
}