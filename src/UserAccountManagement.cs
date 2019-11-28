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

        /// <summary>The collection of platform specific functions.</summary>
        private struct PlatformFunctions
        {
            public Func<string, string> GenerateUserDataFilePath;
            public Func<string, byte[]> ReadUserDataFile;
            public Func<string, byte[], bool> WriteUserDataFile;
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
        private static string m_activeUserDataFilePath;

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
            PlatformFunctions functions = UserAccountManagement.GetPlatformFunctions();
            Debug.Assert(functions.GenerateUserDataFilePath != null);
            Debug.Assert(functions.ReadUserDataFile != null);
            Debug.Assert(functions.WriteUserDataFile != null);

            UserAccountManagement._GenerateUserDataFilePath = functions.GenerateUserDataFilePath;
            UserAccountManagement.ReadUserDataFile = functions.ReadUserDataFile;
            UserAccountManagement.WriteUserDataFile = functions.WriteUserDataFile;

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
            int activeUserIndex = -1;
            int activeUserId = ModProfile.NULL_ID;

            if(UserAccountManagement.activeUserProfile != null)
            {
                activeUserId = UserAccountManagement.activeUserProfile.id;
            }

            // get active user index
            for(int i = 0;
                i < storedData.userData.Length
                && activeUserIndex == -1;
                ++i)
            {
                if(storedData.userData[i].modioUserId == activeUserId)
                {
                    activeUserIndex = i;
                }
            }

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
            UserAccountManagement.WriteUserDataFile(UserAccountManagement.m_activeUserDataFilePath, fileData);

            // set
            UserAccountManagement.m_storedUserData = storedData;
        }

        /// <summary>Loads the user data for the local user with the given identifier.</summary>
        public static void LoadLocalUser(string localUserId = null)
        {
            // generate file path
            string filePath = UserAccountManagement._GenerateUserDataFilePath(localUserId);
            if(filePath == UserAccountManagement.m_activeUserDataFilePath)
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

            int activeUserIndex = -1;
            activeUserData = UserAccountManagement.FindUserData(storedData, activeUserId,
                                                                out activeUserIndex);

            // set
            UserAccountManagement.activeUserProfile = storedData.activeUserProfile;
            UserAccountManagement.activeOAuthToken = storedData.activeOAuthToken;
            UserAccountManagement.m_activeUserData = activeUserData;
            UserAccountManagement.m_activeUserDataFilePath = filePath;
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

        /// <summary>Finds the user data from the stored data and its index within the array.</summary>
        private static LocalUserData FindUserData(StoredUserData storedData,
                                                  int modioUserId,
                                                  out int arrayIndex)
        {
            Debug.Assert(storedData != null);
            Debug.Assert(storedData.userData != null);

            arrayIndex = -1;

            // find
            for(int i = 0;
                i < storedData.userData.Length
                && arrayIndex == -1;
                ++i)
            {
                if(storedData.userData[i].modioUserId == modioUserId)
                {
                    arrayIndex = i;
                    return storedData.userData[i];
                }
            }

            // if not found
            LocalUserData newData = new LocalUserData()
            {
                modioUserId = modioUserId,
                enabledModIds = new int[0],
            };

            return newData;
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
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
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
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Stores the oAuthToken and steamTicket and fetches the UserProfile.</summary>
        private static void FetchUserProfile(Action<UserProfile> onSuccess,
                                             Action<WebRequestError> onError)
        {
            APIClient.GetAuthenticatedUser((p) =>
            {
                UserAuthenticationData data = UserAuthenticationData.instance;
                data.userId = p.id;
                UserAuthenticationData.instance = data;

                if(onSuccess != null)
                {
                    onSuccess(p);
                }
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
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
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
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        // ---------[ PLATFORM SPECIFIC I/O ]---------
        #if UNITY_EDITOR && !DISABLE_EDITOR_USERDATA

            /// <summary>Returns the platform specific functions. (Unity Editor)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                return new PlatformFunctions()
                {
                    GenerateUserDataFilePath = GenerateUserDataFilePath_Editor,
                    ReadUserDataFile = ReadUserDataFile_Editor,
                    WriteUserDataFile = WriteUserDataFile_Editor,
                };
            }

            /// <summary>Generates the file path for the given file identifier. (Unity Editor)</summary>
            private static string GenerateUserDataFilePath_Editor(string fileIdentifier)
            {
                if(string.IsNullOrEmpty(fileIdentifier))
                {
                    fileIdentifier = "default";
                }
                else
                {
                    fileIdentifier = IOUtilities.ReplaceInvalidPathCharacters(fileIdentifier, "_");
                }

                string filePath = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                          "Editor Default Resources",
                                                          "modio",
                                                          "users",
                                                          fileIdentifier + ".user");
                return filePath;
            }

            /// <summary>Loads the user data file. (Unity Editor)</summary>
            public static byte[] ReadUserDataFile_Editor(string filePath)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                return data;
            }

            /// <summary>Writes the user data file. (Unity Editor)</summary>
            public static bool WriteUserDataFile_Editor(string filePath, byte[] data)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = false;
                success = IOUtilities.WriteBinaryFile(filePath, data);
                return success;
            }

        #elif ENABLE_STEAMCLOUD_USERDATA_FACEPUNCH

            /// <summary>Filename for the user data file.</summary>
            public static readonly string USERDATA_FILEPATH = "modio_user.data";

            /// <summary>Loads the Read/Write functions. (Facepunch.Steamworks)</summary>
            private static void LoadFileIOFunctions()
            {
                UserAccountManagement.ReadUserDataFile = ReadUserDataFile_Facepunch;
                UserAccountManagement.WriteUserDataFile = WriteUserDataFile_Facepunch;
            }

            /// <summary>Loads the user data file. (Facepunch.Steamworks)</summary>
            public static byte[] ReadUserDataFile_Facepunch()
            {
                byte[] data = null;

                if(Steamworks.SteamRemoteStorage.FileExists(UserAccountManagement.USERDATA_FILEPATH))
                {
                    data = Steamworks.SteamRemoteStorage.FileRead(UserAccountManagement.USERDATA_FILEPATH);
                }

                return data;
            }

            /// <summary>Writes the user data file. (Facepunch.Steamworks)</summary>
            public static bool WriteUserDataFile_Facepunch(byte[] data)
            {
                bool success = false;
                success = Steamworks.SteamRemoteStorage.FileWrite(UserAccountManagement.USERDATA_FILEPATH, data);
                return success;
            }

        #elif ENABLE_STEAMCLOUD_USERDATA_STEAMWORKSNET

            /// <summary>Filename for the user data file.</summary>
            public static readonly string USERDATA_FILEPATH = "modio_user.data";

            /// <summary>Filesize limitation for the user data file.</summary>
            public readonly int USERDATA_MAXSIZE = 1024;

            /// <summary>Loads the Read/Write functions. (Steamworks.NET)</summary>
            private static void LoadFileIOFunctions()
            {
                UserAccountManagement.ReadUserDataFile = ReadUserDataFile_SteamworksNET;
                UserAccountManagement.WriteUserDataFile = WriteUserDataFile_SteamworksNET;
            }

            /// <summary>Loads the user data file. (Steamworks.NET)</summary>
            public static byte[] ReadUserDataFile_SteamworksNET()
            {
                byte[] data = null;

                if(Steamworks.SteamRemoteStorage.FileExists(UserAccountManagement.USERDATA_FILEPATH))
                {
                    int fileSize = Steamworks.SteamRemoteStorage.GetFileSize(UserAccountManagement.USERDATA_FILEPATH);

                    if(fileSize > 0)
                    {
                        if(fileSize > UserAccountManagement.USERDATA_MAXSIZE)
                        {
                            fileSize = UserAccountManagement.USERDATA_MAXSIZE;
                        }

                        data = new byte[fileSize];
                        Steamworks.SteamRemoteStorage.FileRead(UserAccountManagement.USERDATA_FILEPATH,
                                                               data, fileSize);
                    }
                }

                return data;
            }

            /// <summary>Writes the user data file. (Steamworks.NET)</summary>
            public static bool WriteUserDataFile_SteamworksNET(byte[] data)
            {
                bool success = false;

                int fileSize = data.Length;

                if(fileSize > UserAccountManagement.USERDATA_MAXSIZE)
                {
                    fileSize = UserAccountManagement.USERDATA_MAXSIZE;
                }

                success = Steamworks.SteamRemoteStorage.FileRead(UserAccountManagement.USERDATA_FILEPATH, data, fileSize);

                return success;
            }

        #elif UNITY_STANDALONE_WIN

            /// <summary>Filename for the user data file.</summary>
            public static readonly string USERDATA_FILEPATH
            = IOUtilities.Combine("%APPDATA%",
                                  "modio",
                                  "game-" + PluginSettings.data.gameId,
                                  "user.data");

            /// <summary>Loads the Read/Write functions. (Windows Executable)</summary>
            private static void LoadFileIOFunctions()
            {
                UserAccountManagement.ReadUserDataFile = ReadUserDataFile_Windows;
                UserAccountManagement.WriteUserDataFile = WriteUserDataFile_Windows;
            }

            /// <summary>Loads the user data file. (Windows Executable)</summary>
            public static byte[] ReadUserDataFile_Windows()
            {
                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(UserAccountManagement.USERDATA_FILEPATH);
                return data;
            }

            /// <summary>Writes the user data file. (Windows Executable)</summary>
            public static bool WriteUserDataFile_Windows(byte[] data)
            {
                bool success = false;
                success = IOUtilities.WriteBinaryFile(UserAccountManagement.USERDATA_FILEPATH, data);
                return success;
            }

        #elif UNITY_STANDALONE_OSX

            /// <summary>Returns the platform specific functions. (Mac Application)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                return new PlatformFunctions()
                {
                    GenerateUserDataFilePath = GenerateUserDataFilePath_MacOS,
                    ReadUserDataFile = ReadUserDataFile_MacOS,
                    WriteUserDataFile = WriteUserDataFile_MacOS,
                };
            }

            /// <summary>Generates the file path for the given file identifier. (Mac Application)</summary>
            private static string GenerateUserDataFilePath_MacOS(string fileIdentifier)
            {
                if(string.IsNullOrEmpty(fileIdentifier))
                {
                    fileIdentifier = "default";
                }
                else
                {
                    fileIdentifier = IOUtilities.ReplaceInvalidPathCharacters(fileIdentifier, "_");
                }

                string filePath = ("~/Library/Application Support/mod.io/game-"
                                   + PluginSettings.data.gameId
                                   + "/users/"
                                   + fileIdentifier + ".user");
                return filePath;
            }

            /// <summary>Loads the user data file. (Mac Application)</summary>
            public static byte[] ReadUserDataFile_MacOS(string filePath)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                return data;
            }

            /// <summary>Writes the user data file. (Mac Application)</summary>
            public static bool WriteUserDataFile_MacOS(string filePath, byte[] data)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = false;
                success = IOUtilities.WriteBinaryFile(filePath, data);
                return success;
            }

        #elif UNITY_STANDALONE_LINUX

            /// <summary>Filename for the user data file.</summary>
            public static readonly string USERDATA_FILEPATH
            = ("~/.config/mod.io/"
               + "game-" + PluginSettings.data.gameId
               + "/user.data");

            /// <summary>Loads the Read/Write functions. (Linux Standalone)</summary>
            private static void LoadFileIOFunctions()
            {
                UserAccountManagement.ReadUserDataFile = ReadUserDataFile_Linux;
                UserAccountManagement.WriteUserDataFile = WriteUserDataFile_Linux;
            }

            /// <summary>Loads the user data file. (Linux Standalone)</summary>
            public static byte[] ReadUserDataFile_Linux()
            {
                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(UserAccountManagement.USERDATA_FILEPATH);
                return data;
            }

            /// <summary>Writes the user data file. (Linux Standalone)</summary>
            public static bool WriteUserDataFile_Linux(byte[] data)
            {
                bool success = false;
                success = IOUtilities.WriteBinaryFile(UserAccountManagement.USERDATA_FILEPATH, data);
                return success;
            }

        #endif
    }
}
