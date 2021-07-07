namespace ModIO
{
    /// <summary>[Obsolete] Defines the possible external authentication providers for mod.io.</summary>
    [System.Obsolete("Use ModIO.UserPortal instead.")]
    public enum ExternalAuthenticationProvider
    {
        None = 0,
        Steam = UserPortal.Steam,
        GOG = UserPortal.GOG,
        ItchIO = UserPortal.itchio,
        OculusRift = UserPortal.Oculus,
        XboxLive = UserPortal.XboxLive,
        Switch = UserPortal.Nintendo,
        Discord = UserPortal.Discord,
        UNDEFINED = -1,
    }
}
