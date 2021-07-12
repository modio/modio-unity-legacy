namespace ModIO
{
    /// <summary>Defines the possible external authentication providers for mod.io.</summary>
    public enum ExternalAuthenticationProvider
    {
        None = 0,
        Steam,
        GOG,
        ItchIO,
        OculusRift,
        XboxLive,
        Switch,
        Discord,
        UNDEFINED,
    }

    public static class ExternalAuthenticationProviderEnum
    {
        public static UserPortal ToUserPortalEnum(ExternalAuthenticationProvider provider)
        {
            UserPortal portal = UserPortal.None;

            switch(provider)
            {
                case ExternalAuthenticationProvider.Steam:
                {
                    portal = UserPortal.Steam;
                }
                break;
                case ExternalAuthenticationProvider.GOG:
                {
                    portal = UserPortal.GOG;
                }
                break;
                case ExternalAuthenticationProvider.ItchIO:
                {
                    portal = UserPortal.itchio;
                }
                break;
                case ExternalAuthenticationProvider.OculusRift:
                {
                    portal = UserPortal.Oculus;
                }
                break;
                case ExternalAuthenticationProvider.XboxLive:
                {
                    portal = UserPortal.XboxLive;
                }
                break;
                case ExternalAuthenticationProvider.Switch:
                {
                    portal = UserPortal.Nintendo;
                }
                break;
                case ExternalAuthenticationProvider.Discord:
                {
                    portal = UserPortal.Discord;
                }
                break;
            }

            return portal;
        }
    }
}
