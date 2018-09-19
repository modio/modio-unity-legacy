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
        public int limit;

        /// <summary>
        /// Number of results skipped over.
        /// </summary>
        public int offset;
    }
}
