#if UNITY_EDITOR

namespace ModIO
{
    /// <summary>Editor Platform User definition</summary>
    public class PlatformUser_Editor : PlatformUser<object, object>
    {
        // ---------[ User Data Storage ]---------
        /// <summary>Creates and initializes a user data storage instance.</summary>
        protected internal override void CreateUserDataStore(System.Action<IUserDataIO> onComplete)
        {
            UserDataIO_Editor ioModule = new UserDataIO_Editor();
            ioModule.SetActiveUser(this.identifier, (id, success) =>
            {
                if(!success)
                {
                    ioModule = null;
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(ioModule);
                }
            });
        }

        // ---------[ External Authentication ]---------
        /// <summary>URL for the external authentication endpoint.</summary>
        protected internal override string ExternalAuthenticationEndpoint
        {
            get { return null; }
        }

        /// <summary>Generates the headers for an external authentication request.</summary>
        protected internal override System.Collections.Generic.Dictionary<string, string> GenerateAuthenticationHeaders()
        {
            return null;
        }
    }
}

#endif // UNITY_EDITOR
