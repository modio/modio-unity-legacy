namespace ModIO
{
    public class APIPaginationParameters
    {
        // ---------[ CONSTANTS ]---------
        public const int LIMIT_MAX = 100;

        // ---------[ FIELDS ]---------
        /// <summary>
        /// Maximum number of results returned.
        /// </summary>
        public int limit = APIPaginationParameters.LIMIT_MAX;

        /// <summary>
        /// Number of results skipped over.
        /// </summary>
        public int offset = 0;
    }
}
