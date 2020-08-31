namespace ModIO
{
    /// <summary>Describes a platform-specific user in mod.io terms.</summary>
    public abstract class PlatformUser<TUserIdentifier, TPlatformCredentials>
    {
        // ---------[ User Data Storage ]---------
        /// <summary>User Identifier to use for the user data storage initialization.</summary>
        public TUserIdentifier identifier;

        // ---------[ External Authentication ]---------
        /// <summary>Credentials to use for the mod.io external auth request.</summary>
        public TPlatformCredentials credentials;
    }
}
