namespace ModIO.API
{
    public class AddModRatingParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] The authenticated users mod rating
        public int rating
        {
            set
            {
                this.SetStringValue("rating", value.ToString());
            }
        }

        // ---------[ CONSTRUCTOR ]---------
        public AddModRatingParameters(int ratingValue)
        {
            rating = ratingValue;
        }
    }
}