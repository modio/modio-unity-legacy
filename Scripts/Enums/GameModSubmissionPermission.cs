namespace ModIO
{
    public enum GameModSubmissionPermission
    {
        // Mod uploads must occur via a tool created by the game developers
        ToolOnly = 0,
        // Mod uploads can occur from anywhere, including the website and API
        Unrestricted = 1,
    }
}
