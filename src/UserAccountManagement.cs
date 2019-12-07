using System;
using System.Collections.Generic;
using System.Linq;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Main functional wrapper for the LocalUser structure.</summary>
    public static class UserAccountManagement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Data instance.</summary>
        private static LocalUser _activeUser;

        /// <summary>User data file path for the active user.</summary>
        private static string _activeUserDataFilePath;

        // --- Accessors ---
        /// <summary>User Profile for the currently active user.</summary>
        public static UserProfile ActiveUserProfile
        {
            get { return UserAccountManagement._activeUser.profile; }
        }

        /// <summary>OAuthToken for the currently active user.</summary>
        public static string ActiveUserToken
        {
            get { return UserAccountManagement._activeUser.oAuthToken; }
        }

        /// <summary>Is the ActiveUserToken valid?</summary>
        public static bool WasTokenRejected
        {
            get { return UserAccountManagement._activeUser.wasTokenRejected; }
        }

        /// <summary>Indicates whether the OAuthToken exists and has not been marked aas rejected.</summary>
        public static bool IsTokenValid
        {
            get
            {
                return (!string.IsNullOrEmpty(UserAccountManagement.ActiveUserToken)
                        && !UserAccountManagement.WasTokenRejected);
            }
        }

        /// <summary>External Authentication Ticket for the active user.</summary>
        public static string ExternalAuthTicket
        {
            get { return UserAccountManagement._activeUser.externalAuthTicket.value; }
            private set { UserAccountManagement._activeUser.externalAuthTicket.value = value; }
        }

        /// <summary>Provider of the ExternalAuthTicket.</summary>
        public static ExternalAuthenticationProvider ExternalAuthProvider
        {
            get { return UserAccountManagement._activeUser.externalAuthTicket.provider; }
            private set { UserAccountManagement._activeUser.externalAuthTicket.provider = value; }
        }

        /// <summary>File path for the active user data.</summary>
        public static string UserDataFilePath
        {
            get { return UserAccountManagement._activeUserDataFilePath; }
        }

        /// <summary>URL Postfix for the authentication method.</summary>
        public static string authMethodURLPostfix
        {
            get { throw new System.NotImplementedException(); }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Loads the default local user.</summary>
        static UserAccountManagement()
        {
            UserAccountManagement.LoadLocalUser(null);
        }

        // ---------[ MOD COLLECTION MANAGEMENT ]---------
        /// <summary>Returns the enabled mods for the active user.</summary>
        public static List<int> GetEnabledMods()
        {
            return new List<int>(UserAccountManagement._activeUser.enabledModIds);
        }

        /// <summary>Sets the enabled mods for the active user.</summary>
        public static void SetEnabledMods(IEnumerable<int> modIds)
        {
            int[] modIdArray;

            if(modIds == null)
            {
                modIdArray = new int[0];
            }
            else
            {
                modIdArray = modIds.ToArray();
            }

            UserAccountManagement._activeUser.enabledModIds = modIdArray;
            SaveActiveUser();
        }

        /// <summary>Returns the subscribed mods for the active user.</summary>
        public static List<int> GetSubscribedMods()
        {
            return new List<int>(UserAccountManagement._activeUser.subscribedModIds);
        }

        /// <summary>Sets the subscribed mods for the active user.</summary>
        public static void SetSubscribedMods(IEnumerable<int> modIds)
        {
            int[] modIdArray;

            if(modIds == null)
            {
                modIdArray = new int[0];
            }
            else
            {
                modIdArray = modIds.ToArray();
            }

            UserAccountManagement._activeUser.subscribedModIds = modIdArray;
            SaveActiveUser();
        }

        // ---------[ AUTHENTICATION ]---------
        /// <summary>Pulls any changes to the User Profile from the mod.io servers.</summary>
        public static void FetchUserProfile(Action<UserProfile> onSuccess,
                                            Action<WebRequestError> onError)
        {
            APIClient.GetAuthenticatedUser((p) =>
            {
                UserAccountManagement._activeUser.profile = p;
                UserAccountManagement.SaveActiveUser();

                if(onSuccess != null)
                {
                    onSuccess(p);
                }
            },
            onError);
        }

        /// <summary>A wrapper function for setting the UserAuthenticationData.wasTokenRejected to false.</summary>
        public static void MarkAuthTokenRejected()
        {
            UserAccountManagement._activeUser.wasTokenRejected = true;
            SaveActiveUser();
        }


        /// <summary>Begins the authentication process using a mod.io Security Code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode, (t) =>
            {
                UserAccountManagement._activeUser.oAuthToken = t;
                UserAccountManagement.SaveActiveUser();
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the Steamworks.NET implementation by
        /// @rlabrecque at https://github.com/rlabrecque/Steamworks.NET</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] pTicket, uint pcbTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(pTicket, pcbTicket);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket, onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the FacePunch.SteamWorks implementation by
        /// @garrynewman at https://github.com/Facepunch/Facepunch.Steamworks</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] authTicketData,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(authTicketData, (uint)authTicketData.Length);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket, onSuccess, onError);
        }


        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(string encodedTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            UserAccountManagement.ExternalAuthTicket = encodedTicket;
            UserAccountManagement.ExternalAuthProvider = ExternalAuthenticationProvider.Steam;

            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                UserAccountManagement._activeUser.oAuthToken = t;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(byte[] data, uint dataSize,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(data, dataSize);
            UserAccountManagement.AuthenticateWithGOGEncryptedAppTicket(encodedTicket, onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(string encodedTicket,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            UserAccountManagement.ExternalAuthTicket = encodedTicket;
            UserAccountManagement.ExternalAuthProvider = ExternalAuthenticationProvider.Steam;

            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                UserAccountManagement._activeUser.oAuthToken = t;
                UserAccountManagement.SaveActiveUser();

                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to reauthenticate using the stored external auth ticket.</summary>
        public static void ReauthenticateWithExternalAuthToken(Action<UserProfile> onSuccess,
                                                               Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(UserAccountManagement.ExternalAuthTicket));
            Debug.Assert(UserAccountManagement.ExternalAuthProvider != ExternalAuthenticationProvider.None);

            Action<string, Action<string>, Action<WebRequestError>> authAction = null;

            switch(UserAccountManagement.ExternalAuthProvider)
            {
                case ExternalAuthenticationProvider.Steam:
                {
                    authAction = APIClient.RequestSteamAuthentication;
                }
                break;

                case ExternalAuthenticationProvider.GOG:
                {
                    authAction = APIClient.RequestGOGAuthentication;
                }
                break;

                default:
                {
                    throw new System.NotImplementedException();
                }
            }

            authAction.Invoke(UserAccountManagement.ExternalAuthTicket, (t) =>
            {
                UserAccountManagement._activeUser.oAuthToken = t;
                UserAccountManagement.SaveActiveUser();

                if(onSuccess != null)
                {
                    UserAccountManagement.FetchUserProfile(onSuccess, onError);
                }
            },
            onError);
        }

        // ---------[ USER MANAGEMENT ]---------
        /// <summary>Loads the user data for the local user with the given identifier.</summary>
        public static void LoadLocalUser(string localUserIdentifier = null)
        {
            // generate file path
            string fileName;
            if(string.IsNullOrEmpty(localUserIdentifier))
            {
                fileName = "default.user";
            }
            else
            {
                fileName = IOUtilities.MakeValidFileName(localUserIdentifier, ".user");
            }
            UserAccountManagement._activeUserDataFilePath = "users/" + fileName;

            // read file
            LocalUser userData;
            if(!UserDataStorage.TryReadJSONFile(UserAccountManagement._activeUserDataFilePath, out userData))
            {
                userData = new LocalUser()
                {
                    enabledModIds = new int[0],
                    subscribedModIds = new int[0],
                };
            }
            else
            {
                if(userData.enabledModIds == null)
                {
                    userData.enabledModIds = new int[0];
                }
                if(userData.subscribedModIds == null)
                {
                    userData.subscribedModIds = new int[0];
                }
            }

            // set
            UserAccountManagement._activeUser = userData;
        }

        /// <summary>Changes the local user identifier.</summary>
        public static void SetLocalUserIdentifier(string localUserIdentifier)
        {
            string oldFilePath = UserAccountManagement._activeUserDataFilePath;

            // generate file path
            string newFileName;
            if(string.IsNullOrEmpty(localUserIdentifier))
            {
                newFileName = "default.user";
            }
            else
            {
                newFileName = IOUtilities.MakeValidFileName(localUserIdentifier, ".user");
            }
            UserAccountManagement._activeUserDataFilePath = "users/" + newFileName;

            // set
            UserAccountManagement.SaveActiveUser();

            // delete old
            UserDataStorage.DeleteFile(oldFilePath);
        }

        /// <summary>Writes the active user data to disk.</summary>
        public static void SaveActiveUser()
        {
            UserDataStorage.TryWriteJSONFile(UserAccountManagement._activeUserDataFilePath,
                                             UserAccountManagement._activeUser);
        }

        /// <summary>Sets the local user data directly.</summary>
        public static void SetLocalUserData(LocalUser locaUserData)
        {
            UserAccountManagement._activeUser = locaUserData;
        }
    }
}
