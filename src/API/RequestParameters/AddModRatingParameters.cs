namespace ModIO.API
{
    public class AddModRatingParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] The authenticated users mod rating:
        //  1 = Positive rating (thumbs up)
        // -1 = Negative rating (thumbs down)
        public int rating
        {
            set { this.SetStringValue("rating", value.ToString()); }
        }
    }
}
