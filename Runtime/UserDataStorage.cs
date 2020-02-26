// #define DISABLE_EDITOR_USERDATA
// #define EXCLUDE_STANDALONE_IO
// #define MODIO_FACEPUNCH_SUPPORT
// #define MODIO_STEAMWORKSNET_SUPPORT

using System;
using System.Text;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Functions for user-specific data I/O.</summary>
    public static class UserDataStorage
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Delegate for the initialization callback.</summary>
        public delegate void InitializationCallback();

        /// <summary>Delegate for the read file callback.</summary>
        public delegate void ReadFileCallback(bool success, byte[] data);

        /// <summary>Delegate for the read json file callback.</summary>
        public delegate void ReadJsonFileCallback<T>(bool success, T jsonObject);

        /// <summary>Delegate for write/delete file callbacks.</summary>
        public delegate void WriteFileCallback(bool success);

        /// <summary>The collection of platform specific functions.</summary>
        public struct PlatformFunctions
        {
            // --- Delegates ---
            /// <summary>Delegate for initializing the storage system.</summary>
            public delegate void InitializationStringDelegate(string platformUserIdentifier, InitializationCallback callback);

            /// <summary>Delegate for initializing the storage system.</summary>
            public delegate void InitializationIntDelegate(int platformUserIdentifier, InitializationCallback callback);

            /// <summary>Delegate for reading a file.</summary>
            public delegate void ReadFileDelegate(string filePath, ReadFileCallback callback);

            /// <summary>Delegate for writing a file.</summary>
            public delegate void WriteFileDelegate(string filePath, byte[] fileData, WriteFileCallback callback);

            /// <summary>Delegate for deleting a file.</summary>
            public delegate void DeleteFileDelegate(string filePath, WriteFileCallback callback);

            /// <summary>Delegate for clearing all data.</summary>
            public delegate void ClearAllDataDelegate(WriteFileCallback callback);

            // --- Fields ---
            /// <summary>Delegate for initializing the storage system.</summary>
            public InitializationIntDelegate InitializeWithInt;

            /// <summary>Delegate for initializing the storage system.</summary>
            public InitializationStringDelegate InitializeWithString;

            /// <summary>Delegate for reading a file.</summary>
            public ReadFileDelegate ReadFile;

            /// <summary>Delegate for writing a file.</summary>
            public WriteFileDelegate WriteFile;

            /// <summary>Delegate for deleting a file.</summary>
            public DeleteFileDelegate DeleteFile;

            /// <summary>Delegate for clearing all data.</summary>
            public ClearAllDataDelegate ClearAllData;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly PlatformFunctions PLATFORM;

        /// <summary>Has UserDataStorage been initialized?</summary>
        public static bool isInitialized = false;

        /// <summary>Defines the active user directory</summary>
        public static string activeUserDirectory = string.Empty;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static UserDataStorage()
        {
            // Select the platform appropriate functions
            #if UNITY_EDITOR && !DISABLE_EDITOR_USERDATA
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_Editor();
            #elif MODIO_FACEPUNCH_SUPPORT
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_Facepunch();
            #elif MODIO_STEAMWORKSNET_SUPPORT
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_SteamworksNET();
            #elif !EXCLUDE_STANDALONE_IO
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_Standalone();
            #endif

            Debug.Assert(UserDataStorage.PLATFORM.InitializeWithInt != null);
            Debug.Assert(UserDataStorage.PLATFORM.InitializeWithString != null);
            Debug.Assert(UserDataStorage.PLATFORM.ReadFile != null);
            Debug.Assert(UserDataStorage.PLATFORM.WriteFile != null);
            Debug.Assert(UserDataStorage.PLATFORM.DeleteFile != null);
            Debug.Assert(UserDataStorage.PLATFORM.ClearAllData != null);

            UserDataStorage.InitializeForUser(null);
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void InitializeForUser(string platformUserIdentifier = null, InitializationCallback callback = null)
        {
            UserDataStorage.PLATFORM.InitializeWithString(platformUserIdentifier, callback);
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void InitializeForUser(int platformUserIdentifier, InitializationCallback callback = null)
        {
            UserDataStorage.PLATFORM.InitializeWithInt(platformUserIdentifier, callback);
        }

        // ---------[ IO FUNCTIONS ]---------
        /// <summary>Function used to read a user data file.</summary>
        public static void TryReadJSONFile<T>(string filePathRelative, ReadJsonFileCallback<T> callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
            Debug.Assert(callback != null);

            UserDataStorage.ReadFile(filePathRelative, (success, fileData) =>
            {
                T jsonObject;

                if(success)
                {
                    success = UserDataStorage.TryParseJSONFile(fileData, out jsonObject);
                }
                else
                {
                    jsonObject = default(T);
                }

                callback(success, jsonObject);
            });
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void TryWriteJSONFile<T>(string filePathRelative, T jsonObject, WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            byte[] fileData = null;
            if(UserDataStorage.TryGenerateJSONFile(jsonObject, out fileData))
            {
                UserDataStorage.WriteFile(filePathRelative, fileData, callback);
            }
            else if(callback != null)
            {
                callback.Invoke(false);
            }
        }

        /// <summary>Generates user data file.</summary>
        public static bool TryGenerateJSONFile<T>(T jsonObject, out byte[] fileData)
        {
            // create json data bytes
            try
            {
                string dataString = JsonConvert.SerializeObject(jsonObject);
                fileData = Encoding.UTF8.GetBytes(dataString);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to generate user file data.");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                fileData = new byte[0];
                return false;
            }
        }

        /// <summary>Parses user data file.</summary>
        public static bool TryParseJSONFile<T>(byte[] fileData, out T jsonObject)
        {
            // early out
            if(fileData == null || fileData.Length == 0)
            {
                jsonObject = default(T);
                return false;
            }

            // attempt to parse data
            try
            {
                string dataString = Encoding.UTF8.GetString(fileData);
                jsonObject = JsonConvert.DeserializeObject<T>(dataString);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to parse user data from file.");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                jsonObject = default(T);
                return false;
            }
        }

        /// <summary>Function for reading a user-specific file.</summary>
        public static void ReadFile(string filePathRelative, ReadFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage.activeUserDirectory, filePathRelative);
            UserDataStorage.PLATFORM.ReadFile(filePath, callback);
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
            UserDataStorage.PLATFORM.WriteFile(filePath, fileData, callback);
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static void DeleteFile(string filePathRelative, WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage.activeUserDirectory, filePathRelative);
            UserDataStorage.PLATFORM.DeleteFile(filePath, callback);
        }

        /// <summary>Function for clearing all user data.</summary>
        public static void ClearAllData(WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);

            UserDataStorage.PLATFORM.ClearAllData(callback);
        }

        // ---------[ Platform Specific Functionality ]---------

        #if UNITY_EDITOR && !DISABLE_EDITOR_USERDATA

            /// <summary>Defines the base directory for the user-specific data.</summary>
            public static readonly string EDITOR_RESOURCES_FOLDER = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                                                            "Editor Default Resources",
                                                                                            "modio");

            /// <summary>Returns the platform specific functions. (Unity Editor)</summary>
            public static PlatformFunctions GetPlatformFunctions_Editor()
            {
                return new PlatformFunctions()
                {
                    InitializeWithInt = InitializeForUser_Editor,
                    InitializeWithString = InitializeForUser_Editor,
                    ReadFile = ReadFile_Editor,
                    WriteFile = WriteFile_Editor,
                    DeleteFile = DeleteFile_Editor,
                    ClearAllData = ClearAllData_Editor,
                };
            }

            /// <summary>Initializes the data storage system for a given user. (Unity Editor)</summary>
            public static void InitializeForUser_Editor(string platformUserIdentifier, InitializationCallback callback)
            {
                string userDir = UserDataStorage.EDITOR_RESOURCES_FOLDER;

                if(!string.IsNullOrEmpty(platformUserIdentifier))
                {
                    string folderName = IOUtilities.MakeValidFileName(platformUserIdentifier);
                    userDir = IOUtilities.CombinePath(EDITOR_RESOURCES_FOLDER,
                                                      folderName);
                }

                UserDataStorage.activeUserDirectory = userDir;
                UserDataStorage.isInitialized = true;

                Debug.Log("[mod.io] User Data Directory set: " + UserDataStorage.activeUserDirectory);
            }

            /// <summary>Initializes the data storage system for a given user. (Unity Editor)</summary>
            public static void InitializeForUser_Editor(int platformUserIdentifier, InitializationCallback callback)
            {
                UserDataStorage.InitializeForUser_Editor(platformUserIdentifier.ToString("x8"), callback);
            }

            /// <summary>Read a user file. (Unity Editor)</summary>
            public static void ReadFile_Editor(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                callback.Invoke(true, data);
            }

            /// <summary>Write a user file. (Unity Editor)</summary>
            public static void WriteFile_Editor(string filePath, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(data != null);

                bool fileExisted = System.IO.File.Exists(filePath);
                bool success = false;

                success = IOUtilities.WriteBinaryFile(filePath, data);

                if(success && !fileExisted)
                {
                    UnityEditor.AssetDatabase.Refresh();
                }

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Delete a user file. (Unity Editor)</summary>
            public static void DeleteFile_Editor(string filePath, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool fileExisted = System.IO.File.Exists(filePath);
                bool success = true;

                if(fileExisted)
                {
                    success = IOUtilities.DeleteFile(filePath);

                    if(success)
                    {
                        UnityEditor.AssetDatabase.Refresh();
                    }
                }

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Clears all user data. (Unity Editor)</summary>
            public static void ClearAllData_Editor(WriteFileCallback callback)
            {
                bool success = IOUtilities.DeleteDirectory(UserDataStorage.EDITOR_RESOURCES_FOLDER);
                UnityEditor.AssetDatabase.Refresh();

                if(callback != null) { callback.Invoke(success); }
            }

        #endif // UNITY_EDITOR

        #if MODIO_FACEPUNCH_SUPPORT

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

                callback.Invoke(true, data);
            }

            /// <summary>Writes a user data file. (Facepunch.Steamworks)</summary>
            public static void WriteFile_Facepunch(string filePath, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(data != null);

                bool success = Steamworks.SteamRemoteStorage.FileWrite(filePath, data);

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Deletes a user data file. (Facepunch.Steamworks)</summary>
            public static void DeleteFile_Facepunch(string filePath, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = true;

                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(filePath);
                }

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Clears all user data. (Facepunch.Steamworks)</summary>
            public static void ClearAllData_Facepunch(WriteFileCallback callback)
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

        #endif // MODIO_FACEPUNCH_SUPPORT

        #if MODIO_STEAMWORKSNET_SUPPORT

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

                callback.Invoke(true, data);
            }

            /// <summary>Writes a user data file. (Steamworks.NET)</summary>
            public static void WriteFile_SteamworksNET(string filePath, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(data != null);

                bool success = Steamworks.SteamRemoteStorage.FileWrite(filePath, data, data.Length);

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Deletes a user data file. (Steamworks.NET)</summary>
            public static void DeleteFile_SteamworksNET(string filePath, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = true;

                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(filePath);
                }

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Clears all user data. (Steamworks.NET)</summary>
            public static void ClearAllData_SteamworksNET(WriteFileCallback callback)
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

        #endif // MODIO_STEAMWORKSNET_SUPPORT

        #if !EXCLUDE_STANDALONE_IO

            /// <summary>Root directory for the </summary>
            public static readonly string STANDALONE_USERS_FOLDER = IOUtilities.CombinePath(UnityEngine.Application.persistentDataPath,
                                                                                            "modio-" + PluginSettings.data.gameId,
                                                                                            "users");

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            public static PlatformFunctions GetPlatformFunctions_Standalone()
            {
                return new PlatformFunctions()
                {
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
            }

            /// <summary>Initializes the data storage system for a given user. (Standalone Application)</summary>
            public static void InitializeForUser_Standalone(int platformUserIdentifier, InitializationCallback callback)
            {
                UserDataStorage.InitializeForUser_Standalone(platformUserIdentifier.ToString("x8"), callback);
            }

            /// <summary>Reads a user data file. (Standalone Application)</summary>
            public static void ReadFile_Standalone(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                callback.Invoke(true, data);
            }

            /// <summary>Writes a user data file. (Standalone Application)</summary>
            public static void WriteFile_Standalone(string filePath, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(data != null);

                bool success = false;
                success = IOUtilities.WriteBinaryFile(filePath, data);

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Deletes a user data file. (Standalone Application)</summary>
            public static void DeleteFile_Standalone(string filePath, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                bool success = false;
                success = IOUtilities.DeleteFile(filePath);

                if(callback != null)
                {
                    callback.Invoke(success);
                }
            }

            /// <summary>Clears all user data. (Standalone Application)</summary>
            public static void ClearAllData_Standalone(WriteFileCallback callback)
            {
                bool success = IOUtilities.DeleteDirectory(UserDataStorage.STANDALONE_USERS_FOLDER);

                if(callback != null) { callback.Invoke(success); }
            }

        #endif // !EXCLUDE_STANDALONE_IO
    }
}
