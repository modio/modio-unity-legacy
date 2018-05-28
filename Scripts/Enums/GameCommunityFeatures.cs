namespace ModIO
{
    [System.Flags]
    public enum GameCommunityFeatures
    {
        // All of the options below are disabled
        Disabled = 0,
        // Discussion board enabled
        DiscussionBoard = 0x01,
        // Guides and news enabled
        GuidesAndNews = 0x02,
    }
}
