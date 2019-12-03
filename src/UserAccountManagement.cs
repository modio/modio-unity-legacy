// #define DISABLE_EDITOR_USERDATA
// #define ENABLE_STEAMCLOUD_USERDATA_FACEPUNCH
// #define ENABLE_STEAMCLOUD_USERDATA_STEAMWORKSNET

/*** NOTE:
 * If building to a platform other than Mac, Windows (exe), or Linux,
 * the Unity #define directives as specified at [https://docs.unity3d.com/Manual/PlatformDependentCompilation.html]
 * act to enable those specific authentication methods and thus do not require manual activation.
 ***/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>A collection of user management functions provided for convenience.</summary>
    public static class UserAccountManagement
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Data structure for the user data file.</summary>
        [System.Serializable]
        private class StoredUserData
        {
            public UserProfile activeUserProfile = null;
            public string activeOAuthToken = null;
            public LocalUserData[] userData = null;
        }

        // ---------[ FIELDS ]---------
        /// <summary>User Profile for the currently active user.</summary>
        public static UserProfile activeUserProfile = null;

        /// <summary>OAuthToken for the currently active user.</summary>
        public static string activeOAuthToken = null;

        /// <summary>URL Postfix for the authentication method.</summary>
        public static string authMethodURLPostfix = string.Empty;

        /// <summary>Currently loaded user data.</summary>
        private static StoredUserData m_storedUserData;

        /// <summary>User data for the currently active user.</summary>
        private static LocalUserData m_activeUserData;

        /// <summary>File path to the active user data file.</summary>
        private static string m_localUserFilePath;

        // ---------[ DATA LOADING ]---------
        /// <summary>Function used to the file path for the user data.</summary>
        private readonly static Func<string, string> _GenerateUserDataFilePath = null;

        /// <summary>Function used to read the user data.</summary>
        public readonly static Func<string, byte[]> ReadUserDataFile = null;

        /// <summary>Function used to read the user data.</summary>
        public readonly static Func<string, byte[], bool> WriteUserDataFile = null;

        /// <summary>Loads the platform-specific functionality and stored user data.</summary>
        static UserAccountManagement()
        {
            UserAccountManagement.LoadLocalUser(null);
        }

        /// <summary>Parses user data file.</summary>
        private static StoredUserData ParseStoredUserData(byte[] data)
        {
            // early out
            if(data == null || data.Length == 0)
            {
                return null;
            }

            // attempt to parse data
            StoredUserData storedUserData = null;
            try
            {
                string dataString = Encoding.UTF8.GetString(data);
                storedUserData = JsonConvert.DeserializeObject<StoredUserData>(dataString);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to parse user data from file.");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                storedUserData = null;
            }

            return storedUserData;
        }

        /// <summary>Generates user data file.</summary>
        private static byte[] GenerateUserFileData(StoredUserData storedUserData)
        {
            if(storedUserData == null)
            {
                storedUserData = new StoredUserData();
            }

            // create json data bytes
            byte[] data = null;

            try
            {
                string dataString = JsonConvert.SerializeObject(storedUserData);
                data = Encoding.UTF8.GetBytes(dataString);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to generate user file data.");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                data = new byte[0];
            }

            return data;
        }

        /// <summary>Writes the active user data to disk.</summary>
        public static void WriteActiveUserData()
        {
            StoredUserData storedData = UserAccountManagement.m_storedUserData;
            int activeUserId = ModProfile.NULL_ID;
            if(UserAccountManagement.activeUserProfile != null)
            {
                activeUserId = UserAccountManagement.activeUserProfile.id;
            }

            // get active user index
            int activeUserIndex = UserAccountManagement.FindUserData(storedData, activeUserId);

            // create new data entry if necessary
            if(activeUserIndex < 0)
            {
                LocalUserData[] newUserData = new LocalUserData[storedData.userData.Length];
                Array.Copy(storedData.userData, newUserData, storedData.userData.Length);
                storedData.userData = newUserData;

                activeUserIndex = storedData.userData.Length;
            }

            // Update stored data
            storedData.activeUserProfile = UserAccountManagement.activeUserProfile;
            storedData.activeOAuthToken = UserAccountManagement.activeOAuthToken;
            storedData.userData[activeUserIndex] = UserAccountManagement.m_activeUserData;

            // write file
            byte[] fileData = UserAccountManagement.GenerateUserFileData(storedData);
            UserAccountManagement.WriteUserDataFile(UserAccountManagement.m_localUserFilePath, fileData);

            // set
            UserAccountManagement.m_storedUserData = storedData;
        }

        /// <summary>Loads the user data for the local user with the given identifier.</summary>
        public static void LoadLocalUser(string localUserId = null)
        {
            // generate file path
            string filePath = UserAccountManagement._GenerateUserDataFilePath(localUserId);
            if(filePath == UserAccountManagement.m_localUserFilePath)
            {
                return;
            }

            // load data
            byte[] userFileData = UserAccountManagement.ReadUserDataFile(filePath);
            StoredUserData storedData = UserAccountManagement.ParseStoredUserData(userFileData);
            LocalUserData activeUserData;

            // create stored data if unavailable
            if(storedData == null)
            {
                storedData = new StoredUserData();
                storedData.activeUserProfile = null;
                storedData.activeOAuthToken = null;
                storedData.userData = null;
            }

            if(storedData.userData == null)
            {
                storedData.userData = new LocalUserData[0];
            }

            // load active user
            int activeUserId = ModProfile.NULL_ID;
            if(storedData.activeUserProfile != null)
            {
                activeUserId = storedData.activeUserProfile.id;
            }

            int activeUserIndex = UserAccountManagement.FindUserData(storedData, activeUserId);
            if(activeUserIndex < 0)
            {
                activeUserData = new LocalUserData()
                {
                    modioUserId = activeUserId,
                    enabledModIds = new int[0],
                };
            }
            else
            {
                activeUserData = storedData.userData[activeUserIndex];
            }

            // set
            UserAccountManagement.activeUserProfile = storedData.activeUserProfile;
            UserAccountManagement.activeOAuthToken = storedData.activeOAuthToken;
            UserAccountManagement.m_activeUserData = activeUserData;
            UserAccountManagement.m_localUserFilePath = filePath;
            UserAccountManagement.m_storedUserData = storedData;
        }

        // ---------[ USER ACTIONS ]---------
        /// <summary>Returns the enabled mods for the active user.</summary>
        public static List<int> GetEnabledModIds()
        {
            return new List<int>(UserAccountManagement.m_activeUserData.enabledModIds);
        }

        /// <summary>Sets the enabled mods for the active user.</summary>
        public static void SetEnabledModIds(IEnumerable<int> modIds)
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

            UserAccountManagement.m_activeUserData.enabledModIds = modIdArray;
        }

        // ---------[ UTILITY ]---------
        /// <summary>A wrapper function for setting the UserAuthenticationData.wasTokenRejected to false.</summary>
        public static void MarkAuthTokenRejected()
        {
            UserAuthenticationData data = UserAuthenticationData.instance;
            data.wasTokenRejected = true;
            UserAuthenticationData.instance = data;
        }

        /// <summary>Finds the index of the modioUserId within the stored data array.</summary>
        private static int FindUserData(StoredUserData storedData, int modioUserId)
        {
            Debug.Assert(storedData != null);
            Debug.Assert(storedData.userData != null);

            // find
            for(int i = 0;
                i < storedData.userData.Length;
                ++i)
            {
                if(storedData.userData[i].modioUserId == modioUserId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Fetches and stores the UserProfile for the active user.</summary>
        private static void FetchActiveUserProfile(Action<UserProfile> onSuccess,
                                                   Action<WebRequestError> onError)
        {
            APIClient.GetAuthenticatedUser((p) =>
            {
                UserAuthenticationData data = UserAuthenticationData.instance;
                data.userId = p.id;
                UserAuthenticationData.instance = data;


                UserAccountManagement.activeUserProfile = p;
                UserAccountManagement.WriteActiveUserData();


                if(onSuccess != null)
                {
                    onSuccess(p);
                }
            },
            onError);
        }

        // ---------[ AUTHENTICATION ]---------
        /// <summary>Requests a login code be sent to an email address.</summary>
        public static void RequestSecurityCode(string emailAddress,
                                               Action onSuccess,
                                               Action<WebRequestError> onError)
        {
            if(onSuccess == null)
            {
                APIClient.SendSecurityCode(emailAddress, null, onError);
            }
            else
            {
                APIClient.SendSecurityCode(emailAddress, (m) => onSuccess(), onError);
            }
        }

        /// <summary>Attempts to authenticate a user using an emailed security code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = null,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;

                UserAccountManagement.activeOAuthToken = t;
                UserAccountManagement.activeUserProfile = null;
                UserAccountManagement.WriteActiveUserData();

                UserAccountManagement.FetchActiveUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to reauthenticate using the enabled service.</summary>
        public static void ReauthenticateWithExternalAuthToken(Action<UserProfile> onSuccess,
                                                               Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(UserAuthenticationData.instance.externalAuthToken));

            Action<string, Action<string>, Action<WebRequestError>> authAction = null;

            #if ENABLE_STEAMWORKS_FACEPUNCH || ENABLE_STEAMWORKS_NET || ENABLE_STEAM_OTHER
                authAction = APIClient.RequestSteamAuthentication;
            #elif ENABLE_GOG_AUTHENTICATION
                authAction = APIClient.RequestGOGAuthentication;
            #endif

            #if DEBUG
                if(authAction == null)
                {
                    Debug.LogError("[mod.io] Cannot reauthenticate without enabling an external"
                                   + " authentication service. Please refer to this file"
                                   + " (UserAccountManagement.cs) and uncomment the #define for the"
                                   + " desired service at the beginning of the file.");
                }
            #endif

            authAction.Invoke(UserAuthenticationData.instance.externalAuthToken, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchActiveUserProfile(onSuccess, onError);
            },
            onError);
        }

        // ---------[ STEAM AUTHENTICATION ]---------
        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the Steamworks.NET implementation by
        /// @rlabrecque at https://github.com/rlabrecque/Steamworks.NET</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] pTicket, uint pcbTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(pTicket, pcbTicket);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket,
                                                                          onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the FacePunch.SteamWorks implementation by
        /// @garrynewman at https://github.com/Facepunch/Facepunch.Steamworks</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] authTicketData,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(authTicketData, (uint)authTicketData.Length);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket,
                                                                          onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(string encodedTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = encodedTicket,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;

                UserAccountManagement.activeOAuthToken = t;
                UserAccountManagement.activeUserProfile = null;
                UserAccountManagement.WriteActiveUserData();

                UserAccountManagement.FetchActiveUserProfile(onSuccess, onError);
            },
            onError);
        }

        // ---------[ GOG AUTHENTICATION ]---------
        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(byte[] data, uint dataSize,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(data, dataSize);
            UserAccountManagement.AuthenticateWithGOGEncryptedAppTicket(encodedTicket,
                                                                        onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(string encodedTicket,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = encodedTicket,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;

                UserAccountManagement.activeOAuthToken = t;
                UserAccountManagement.activeUserProfile = null;
                UserAccountManagement.WriteActiveUserData();

                UserAccountManagement.FetchActiveUserProfile(onSuccess, onError);
            },
            onError);
        }
    }
}
