namespace ModIO
{
    public class PaginationParameters
    {
        public const int LIMIT_MAX = 100;

        public static readonly PaginationParameters Default = new PaginationParameters()
        {
            limit = LIMIT_MAX,
            offset = 0,
        };

        public int limit;
        public int offset;
    }
}