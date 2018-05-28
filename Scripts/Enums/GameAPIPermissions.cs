namespace ModIO
{
    [System.Flags]
    public enum GameAPIPermissions
    {
        // All of the options below are disabled
        RestrictAll = 0,
        // Allow 3rd parties to access this games API endpoints
        AllowPublicAccess = 1,
        // Allow mods to be downloaded directly
        // (If disabled all download URLs will contain a frequently
        // changing verification hash to stop unauthorized use)
        AllowDirectDownloads = 2,
    }
}
