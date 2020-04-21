// #define DISABLE_EDITOR_CODEPATH
// #define MODIO_FACEPUNCH_SUPPORT
// #define MODIO_STEAMWORKSNET_SUPPORT

using System;
using System.Text;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

using ModIO.DataStorageCallbacks;

namespace ModIO
{
    /// <summary>Functions for user-specific data I/O.</summary>
    public static class UserDataStorage
    {
        // ---------[ Nested Data-Types ]---------
        // --- Callbacks ---
        /// <summary>Delegate for the initialization callback.</summary>
        public delegate void InitializationCallback();

        /// <summary>Delegate for the ClearAllData callback.</summary>
        public delegate void ClearAllDataCallback(bool success);

        // --- I/O Functions ---
        /// <summary>Delegate for initializing the storage system.</summary>
        public delegate void InitializationStringDelegate(string platformUserIdentifier, InitializationCallback callback);

        /// <summary>Delegate for initializing the storage system.</summary>
        public delegate void InitializationIntDelegate(int platformUserIdentifier, InitializationCallback callback);

        /// <summary>Delegate for clearing all data.</summary>
        public delegate void ClearAllDataDelegate(ClearAllDataCallback callback);

        // ---------[ I/O Functionality ]---------
        /// <summary>Defines the functions needed for a complete platform IO.</summary>
        public interface IPlatformIO : ModIO.IPlatformUserDataIO
        {
            // --- Fields ---
            /// <summary>Delegate for initializing the storage system.</summary>
            void InitializeForUser(string platformUserIdentifier, InitializationCallback callback);

            /// <summary>Delegate for initializing the storage system.</summary>
            void InitializeForUser(int platformUserIdentifier, InitializationCallback callback);

            /// <summary>Delegate for clearing all data.</summary>
            void ClearAllData(ClearAllDataCallback callback);
        }

        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly IPlatformUserDataIO PLATFORM_IO;

        // ---------[ Fields ]---------
        /// <summary>Has UserDataStorage been initialized?</summary>
        public static bool isInitialized = false;

        /// <summary>Defines the active user directory</summary>
        public static string activeUserDirectory = string.Empty;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static UserDataStorage()
        {
            // Select the platform appropriate functions
            #if UNITY_EDITOR && !DISABLE_EDITOR_CODEPATH
                UserDataStorage.PLATFORM_IO = new SystemIOWrapper_Editor();
            #elif MODIO_FACEPUNCH_SUPPORT
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_Facepunch();
            #elif MODIO_STEAMWORKSNET_SUPPORT
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_SteamworksNET();
            #else
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_Standalone();
            #endif
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback)
        {
            UserDataStorage.PLATFORM_IO.SetActiveUser(platformUserId, callback);
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback)
        {
            UserDataStorage.PLATFORM_IO.SetActiveUser(platformUserId, callback);
        }


        // ---------[ I/O Interface ]---------
        /// <summary>Function for reading a user-specific file.</summary>
        public static void ReadFile(string filePathRelative, ReadFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage.activeUserDirectory, filePathRelative);
            UserDataStorage.PLATFORM_IO.ReadFile(filePath, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void ReadJSONFile<T>(string filePathRelative, ReadJSONFileCallback<T> callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(callback != null);

            UserDataStorage.ReadFile(filePathRelative, (path, success, fileData) =>
            {
                T jsonObject;

                if(success)
                {
                    success = IOUtilities.TryParseUTF8JSONData(fileData, out jsonObject);
                }
                else
                {
                    jsonObject = default(T);

                    Debug.LogWarning("[mod.io] Failed convert file data into JSON object."
                                     + "\nFile: " + path + "\n\n");
                }

                callback.Invoke(path, success, jsonObject);
            });
        }

        /// <summary>Function for writing a user-specific file.</summary>
        public static void WriteFile(string filePathRelative, byte[] fileData, WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
            Debug.Assert(fileData != null);

            #if DEBUG
            if(fileData.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + filePathRelative);
            }
            #endif // DEBUG

            string filePath = IOUtilities.CombinePath(UserDataStorage.activeUserDirectory, filePathRelative);
            UserDataStorage.PLATFORM_IO.WriteFile(filePath, fileData, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void WriteJSONFile<T>(string filePathRelative, T jsonObject, WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);

            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                UserDataStorage.WriteFile(filePathRelative, data, callback);
            }
            else if(callback != null)
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + filePathRelative + "\n\n");

                string filePath = IOUtilities.CombinePath(UserDataStorage.activeUserDirectory, filePathRelative);
                callback.Invoke(filePath, false);
            }
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static void DeleteFile(string filePathRelative, DeleteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage.activeUserDirectory, filePathRelative);
            UserDataStorage.PLATFORM_IO.DeleteFile(filePath, callback);
        }

        /// <summary>Function for clearing of the active user's data.</summary>
        public static void ClearActiveUserData(ClearActiveUserDataCallback callback)
        {
            UserDataStorage.PLATFORM_IO.ClearActiveUserData(callback);
        }

        // ---------[ Platform Specific Functionality ]---------
        #if UNITY_EDITOR && !DISABLE_EDITOR_CODEPATH

        #elif MODIO_FACEPUNCH_SUPPORT

            /// <summary>Defines the base directory for the user-specific data.</summary>
            public static readonly string FACEPUNCH_USER_DIRECTORY = IOUtilities.CombinePath("modio", "users");

            /// <summary>Returns the platform specific functions. (Facepunch.Steamworks)</summary>
            public static PlatformFunctions GetPlatformFunctions_Facepunch()
            {
                Debug.Log("[mod.io] User Data I/O being handled by Facepunch.Steamworks");

                return new PlatformFunctions()
                {
                    InitializeWithInt = InitializeForUser_Facepunch,
                    InitializeWithString = InitializeForUser_Facepunch,
                    ReadFile = ReadFile_Facepunch,
                    WriteFile = WriteFile_Facepunch,
                    DeleteFile = DeleteFile_Facepunch,
                    ClearAllData = ClearAllData_Facepunch,
                };
            }

            /// <summary>Initializes the data storage system for a given user. (Facepunch.Steamworks)</summary>
            public static void InitializeForUser_Facepunch(string platformUserIdentifier, InitializationCallback callback)
            {
                string userDir = UserDataStorage.FACEPUNCH_USER_DIRECTORY;

                if(!string.IsNullOrEmpty(platformUserIdentifier))
                {
                    string folderName = IOUtilities.MakeValidFileName(platformUserIdentifier);
                    userDir = IOUtilities.CombinePath(FACEPUNCH_USER_DIRECTORY,
                                                      folderName);
                }

                UserDataStorage.activeUserDirectory = userDir;
                UserDataStorage.isInitialized = true;

                Debug.Log("[mod.io] Steam User Data Directory set: " + UserDataStorage.activeUserDirectory);

                if(callback != null)
                {
                    callback.Invoke();
                }
            }

            /// <summary>Initializes the data storage system for a given user. (Facepunch.Steamworks)</summary>
            public static void InitializeForUser_Facepunch(int platformUserIdentifier, InitializationCallback callback)
            {
                UserDataStorage.InitializeForUser_Facepunch(platformUserIdentifier.ToString("x8"), callback);
            }

            /// <summary>Loads the user data file. (Facepunch.Steamworks)</summary>
            public static void ReadFile_Facepunch(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    data = Steamworks.SteamRemoteStorage.FileRead(filePath);
                }

                callback.Invoke(true, data, filePath);
            }

            /// <summary>Writes a user data file. (Facepunch.Steamworks)</summary>
            public static void WriteFile_Facepunch(string filePath, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(data != null);

                bool success = Steamworks.SteamRemoteStorage.FileWrite(filePath, data);

                if(callback != null)
                {
                    callback.Invoke(success, filePath);
                }
            }

            /// <summary>Deletes a user data file. (Facepunch.Steamworks)</summary>
            public static void DeleteFile_Facepunch(string filePath, DeleteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = true;

                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(filePath);
                }

                if(callback != null)
                {
                    callback.Invoke(success, filePath);
                }
            }

            /// <summary>Clears all user data. (Facepunch.Steamworks)</summary>
            public static void ClearAllData_Facepunch(ClearAllDataCallback callback)
            {
                var steamFiles = Steamworks.SteamRemoteStorage.Files;
                bool success = true;

                foreach(string filePath in steamFiles)
                {
                    if(filePath.StartsWith(UserDataStorage.FACEPUNCH_USER_DIRECTORY))
                    {
                        success = Steamworks.SteamRemoteStorage.FileDelete(filePath) && success;
                    }
                }

                if(callback != null) { callback.Invoke(success); }
            }

        #elif MODIO_STEAMWORKSNET_SUPPORT

            /// <summary>Defines the base directory for the user-specific data.</summary>
            public static readonly string STEAMWORKSNET_USER_DIRECTORY = IOUtilities.CombinePath("modio", "users");

            /// <summary>Returns the platform specific functions. (Steamworks.NET)</summary>
            public static PlatformFunctions GetPlatformFunctions_SteamworksNET()
            {
                Debug.Log("[mod.io] User Data I/O being handled by Steamworks.NET");

                return new PlatformFunctions()
                {
                    InitializeWithInt = InitializeForUser_SteamworksNET,
                    InitializeWithString = InitializeForUser_SteamworksNET,
                    ReadFile = ReadFile_SteamworksNET,
                    WriteFile = WriteFile_SteamworksNET,
                    DeleteFile = DeleteFile_SteamworksNET,
                    ClearAllData = ClearAllData_SteamworksNET,
                };
            }

            /// <summary>Initializes the data storage system for a given user. (Steamworks.NET)</summary>
            public static void InitializeForUser_SteamworksNET(string platformUserIdentifier, InitializationCallback callback)
            {
                string userDir = UserDataStorage.STEAMWORKSNET_USER_DIRECTORY;

                if(!string.IsNullOrEmpty(platformUserIdentifier))
                {
                    string folderName = IOUtilities.MakeValidFileName(platformUserIdentifier);
                    userDir = IOUtilities.CombinePath(STEAMWORKSNET_USER_DIRECTORY,
                                                      folderName);
                }

                UserDataStorage.activeUserDirectory = userDir;
                UserDataStorage.isInitialized = true;

                Debug.Log("[mod.io] Steam User Data Directory set: " + UserDataStorage.activeUserDirectory);

                if(callback != null)
                {
                    callback.Invoke();
                }
            }

            /// <summary>Initializes the data storage system for a given user. (Steamworks.NET)</summary>
            public static void InitializeForUser_SteamworksNET(int platformUserIdentifier, InitializationCallback callback)
            {
                UserDataStorage.InitializeForUser_SteamworksNET(platformUserIdentifier.ToString("x8"), callback);
            }

            /// <summary>Reads a user data file. (Steamworks.NET)</summary>
            public static void ReadFile_SteamworksNET(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    int fileSize = Steamworks.SteamRemoteStorage.GetFileSize(filePath);

                    if(fileSize > 0)
                    {
                        data = new byte[fileSize];
                        Steamworks.SteamRemoteStorage.FileRead(filePath, data, fileSize);
                    }
                }

                callback.Invoke(true, data, filePath);
            }

            /// <summary>Writes a user data file. (Steamworks.NET)</summary>
            public static void WriteFile_SteamworksNET(string filePath, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(data != null);

                bool success = Steamworks.SteamRemoteStorage.FileWrite(filePath, data, data.Length);

                if(callback != null)
                {
                    callback.Invoke(success, filePath);
                }
            }

            /// <summary>Deletes a user data file. (Steamworks.NET)</summary>
            public static void DeleteFile_SteamworksNET(string filePath, DeleteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = true;

                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(filePath);
                }

                if(callback != null)
                {
                    callback.Invoke(success, filePath);
                }
            }

            /// <summary>Clears all user data. (Steamworks.NET)</summary>
            public static void ClearAllData_SteamworksNET(ClearAllDataCallback callback)
            {
                int fileCount = Steamworks.SteamRemoteStorage.GetFileCount();
                bool success = true;

                for(int i = 0; i < fileCount; ++i)
                {
                    string filePath;
                    int fileSize;

                    filePath = Steamworks.SteamRemoteStorage.GetFileNameAndSize(i, out fileSize);

                    if(filePath.StartsWith(UserDataStorage.STEAMWORKSNET_USER_DIRECTORY))
                    {
                        success = Steamworks.SteamRemoteStorage.FileDelete(filePath) && success;
                    }
                }

                if(callback != null) { callback.Invoke(success); }
            }

        #else

            /// <summary>Root directory for the user-specific data.</summary>
            public static readonly string STANDALONE_USERS_FOLDER = IOUtilities.CombinePath(UnityEngine.Application.persistentDataPath,
                                                                                            "modio-" + PluginSettings.data.gameId,
                                                                                            "users");

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            public static PlatformFunctions GetPlatformFunctions_Standalone()
            {
                return new PlatformFunctions()
                {
                    InitializeWithInt = InitializeForUser_Standalone,
                    InitializeWithString = InitializeForUser_Standalone,
                    ReadFile = ReadFile_Standalone,
                    WriteFile = WriteFile_Standalone,
                    DeleteFile = DeleteFile_Standalone,
                    ClearAllData = ClearAllData_Standalone,
                };
            }

            /// <summary>Initializes the data storage system for a given user. (Standalone Application)</summary>
            public static void InitializeForUser_Standalone(string platformUserIdentifier, InitializationCallback callback)
            {
                string userDir = UserDataStorage.STANDALONE_USERS_FOLDER;

                if(!string.IsNullOrEmpty(platformUserIdentifier))
                {
                    string folderName = IOUtilities.MakeValidFileName(platformUserIdentifier);
                    userDir = IOUtilities.CombinePath(STANDALONE_USERS_FOLDER,
                                                      folderName);
                }

                UserDataStorage.activeUserDirectory = userDir;
                UserDataStorage.isInitialized = true;

                Debug.Log("[mod.io] User Data Directory set: " + UserDataStorage.activeUserDirectory);

                if(callback != null)
                {
                    callback.Invoke();
                }
            }

            /// <summary>Initializes the data storage system for a given user. (Standalone Application)</summary>
            public static void InitializeForUser_Standalone(int platformUserIdentifier, InitializationCallback callback)
            {
                UserDataStorage.InitializeForUser_Standalone(platformUserIdentifier.ToString("x8"), callback);
            }

            /// <summary>Reads a user data file. (Standalone Application)</summary>
            public static void ReadFile_Standalone(string filePath, ReadFileCallback callback)
            {
                bool success = false;
                byte[] data = null;

                success = LocalDataStorage.ReadFile(filePath, out data);

                callback.Invoke(filePath, success, data);
            }

            /// <summary>Writes a user data file. (Standalone Application)</summary>
            public static void WriteFile_Standalone(string filePath, byte[] data, WriteFileCallback callback)
            {
                bool success = LocalDataStorage.WriteFile(filePath, data);

                if(callback != null) { callback.Invoke(filePath, success); }
            }

            /// <summary>Deletes a user data file. (Standalone Application)</summary>
            public static void DeleteFile_Standalone(string filePath, DeleteFileCallback callback)
            {
                bool success = LocalDataStorage.DeleteFile(filePath);

                if(callback != null)
                {
                    callback.Invoke(filePath, success);
                }
            }

            /// <summary>Clears all user data. (Standalone Application)</summary>
            public static void ClearAllData_Standalone(ClearAllDataCallback callback)
            {
                bool success = LocalDataStorage.DeleteDirectory(UserDataStorage.STANDALONE_USERS_FOLDER);

                if(callback != null) { callback.Invoke(success); }
            }

        #endif

        // ---------[ Obsolete ]---------
        /// <summary>Initializes the data storage functionality for a given user.</summary>
        [System.Obsolete()]
        public static void InitializeForUser(string platformUserIdentifier = null, InitializationCallback callback = null)
        {
            UserDataStorage.PLATFORM_IO.SetActiveUser(platformUserIdentifier, null);
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        [System.Obsolete()]
        public static void InitializeForUser(int platformUserIdentifier, InitializationCallback callback = null)
        {
            UserDataStorage.PLATFORM_IO.SetActiveUser(platformUserIdentifier, null);
        }

        /// <summary>Function for clearing all user data.</summary>
        [System.Obsolete()]
        public static void ClearAllData(ClearAllDataCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);

            UserDataStorage.PLATFORM_IO.ClearActiveUserData(null);
        }
    }
}
