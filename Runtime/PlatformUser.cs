namespace ModIO
{
    /// <summary>Describes a platform-specific user in mod.io terms.</summary>
    public abstract class PlatformUser<TUserIdentifier, TPlatformCredentials>
    {
        // ---------[ User Data Storage ]---------
        /// <summary>User Identifier to use for the user data storage initialization.</summary>
        public TUserIdentifier identifier;

        /// <summary>Creates and initializes a user data storage instance.</summary>
        protected internal abstract void CreateUserDataStore(System.Action<IUserDataIO> onComplete);

        // ---------[ External Authentication ]---------
        /// <summary>Credentials to use for the mod.io external auth request.</summary>
        public TPlatformCredentials credentials;

        /// <summary>URL for the external authentication endpoint.</summary>
        protected internal abstract string ExternalAuthenticationEndpoint { get; }

        /// <summary>Generates the headers for an external authentication request.</summary>
        protected internal abstract System.Collections.Generic.Dictionary<string, string> GenerateAuthenticationHeaders();
    }
}
