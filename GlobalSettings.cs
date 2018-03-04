namespace ModIO
{
    public static class GlobalSettings
    {
        #if DEBUG
        public const bool USE_TEST_SERVER = true;
        #endif

        public const int GAME_ID = 0;
        public const string GAME_APIKEY = null;
    }
}