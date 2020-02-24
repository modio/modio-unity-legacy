// #define DISABLE_EDITOR_USERDATA
// #define ENABLE_STEAMCLOUD_USERDATA_FACEPUNCH
// #define ENABLE_STEAMCLOUD_USERDATA_STEAMWORKSNET

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
        /// <summary>Delegate for the read file callback.</summary>
        public delegate void ReadFileCallback(bool success, byte[] data);

        /// <summary>Delegate for the read json file callback.</summary>
        public delegate void ReadJsonFileCallback<T>(bool success, T jsonObject);

        /// <summary>Delegate for write/delete file callbacks.</summary>
        public delegate void WriteFileCallback(bool success);

        // ---------[ FIELDS ]---------
        /// <summary>Defines the base directory for the user-specific data.</summary>
        private static readonly string _USER_DIRECTORY_ROOT;

        /// <summary>Defines the active user directory</summary>
        private static string _activeUserDirectory;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static UserDataStorage()
        {
            PlatformFunctions platform = UserDataStorage.GetPlatformFunctions();
            Debug.Assert(platform.ReadFile != null);
            Debug.Assert(platform.WriteFile != null);
            Debug.Assert(platform.DeleteFile != null);
            Debug.Assert(platform.ClearAllData != null);
            Debug.Assert(platform.UserDirectoryRoot != null);

            UserDataStorage._PlatformReadFile       = platform.ReadFile;
            UserDataStorage._PlatformWriteFile      = platform.WriteFile;
            UserDataStorage._PlatformDeleteFile     = platform.DeleteFile;
            UserDataStorage._PlatformClearAllData   = platform.ClearAllData;
            UserDataStorage._USER_DIRECTORY_ROOT    = platform.UserDirectoryRoot;

            UserDataStorage.SetActiveUserDirectory(null);
        }

        /// <summary>Sets the user directory to store into based on a given user identifier.</summary>
        public static void SetActiveUserDirectory(string localUserIdentifier = null)
        {
            string userDir = UserDataStorage._USER_DIRECTORY_ROOT;

            if(!string.IsNullOrEmpty(localUserIdentifier))
            {
                string folderName = IOUtilities.MakeValidFileName(localUserIdentifier);
                userDir = IOUtilities.CombinePath(UserDataStorage._USER_DIRECTORY_ROOT,
                                                  folderName);
            }

            UserDataStorage._activeUserDirectory = userDir;

            Debug.Log("[mod.io] User Data Directory set: " + UserDataStorage._activeUserDirectory);
        }

        // ---------[ IO FUNCTIONS ]---------
        /// <summary>Function used to read a user data file.</summary>
        public static void TryReadJSONFile<T>(string filePathRelative, ReadJsonFileCallback<T> callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
            Debug.Assert(callback != null);

            UserDataStorage.ReadBinaryFile(filePathRelative, (success, fileData) =>
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
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));


            byte[] fileData = null;
            if(UserDataStorage.TryGenerateJSONFile(jsonObject, out fileData))
            {
                UserDataStorage.WriteBinaryFile(filePathRelative, fileData, callback);
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
        public static void ReadBinaryFile(string filePathRelative, ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);
            UserDataStorage._PlatformReadFile(filePath, callback);
        }

        /// <summary>Function for writing a user-specific file.</summary>
        public static void WriteBinaryFile(string filePathRelative, byte[] fileData, WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
            Debug.Assert(fileData != null);

            #if DEBUG
            if(fileData.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + filePathRelative);
            }
            #endif // DEBUG

            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);
            UserDataStorage._PlatformWriteFile(filePath, fileData, callback);
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static void DeleteFile(string filePathRelative, WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);
            UserDataStorage._PlatformDeleteFile(filePath, callback);
        }

        /// <summary>Function for clearing all user data.</summary>
        public static void ClearAllData()
        {
            UserDataStorage._PlatformClearAllData((s) => {});
        }

        // ---------[ PLATFORM SPECIFIC I/O ]---------
        /// <summary>Delegate for reading a file.</summary>
        private delegate void ReadFileDelegate(string filePath, ReadFileCallback callback);

        /// <summary>Delegate for writing a file.</summary>
        private delegate void WriteFileDelegate(string filePath, byte[] fileData, WriteFileCallback callback);

        /// <summary>Delegate for deleting a file.</summary>
        private delegate void DeleteFileDelegate(string filePath, WriteFileCallback callback);

        /// <summary>Delegate for clearing all data.</summary>
        private delegate void ClearAllDataDelegate(WriteFileCallback callback);

        /// <summary>Function for reading a user-specific file.</summary>
        private readonly static ReadFileDelegate _PlatformReadFile = null;

        /// <summary>Function for writing a user-specific file.</summary>
        private readonly static WriteFileDelegate _PlatformWriteFile = null;

        /// <summary>Function for deleting a user-specific file.</summary>
        private readonly static DeleteFileDelegate _PlatformDeleteFile = null;

        /// <summary>Function for clearing all user data.</summary>
        private readonly static ClearAllDataDelegate _PlatformClearAllData = null;

        // ------ Platform Specific Functionality ------
        /// <summary>The collection of platform specific functions.</summary>
        private struct PlatformFunctions
        {
            public ReadFileDelegate ReadFile;
            public WriteFileDelegate WriteFile;
            public DeleteFileDelegate DeleteFile;
            public ClearAllDataDelegate ClearAllData;
            public string UserDirectoryRoot;
        }

        #if UNITY_EDITOR && !DISABLE_EDITOR_USERDATA

            /// <summary>Returns the platform specific functions. (Unity Editor)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                string userDir = IOUtilities.CombinePath(UnityEngine.Application.dataPath, "Editor Default Resources", "modio");

                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_Editor,
                    WriteFile = WriteFile_Editor,
                    DeleteFile = DeleteFile_Editor,
                    ClearAllData = ClearAllData_Editor,
                    UserDirectoryRoot = userDir,
                };
            }

            /// <summary>Read a user file. (Unity Editor)</summary>
            private static void ReadFile_Editor(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                callback.Invoke(true, data);
            }

            /// <summary>Write a user file. (Unity Editor)</summary>
            private static void WriteFile_Editor(string filePath, byte[] data, WriteFileCallback callback)
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
            private static void DeleteFile_Editor(string filePath, WriteFileCallback callback)
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
            private static void ClearAllData_Editor(WriteFileCallback callback)
            {
                bool success = IOUtilities.DeleteDirectory(UserDataStorage._USER_DIRECTORY_ROOT);
                UnityEditor.AssetDatabase.Refresh();

                if(callback != null) { callback.Invoke(success); }
            }

        #elif ENABLE_STEAMCLOUD_USERDATA_FACEPUNCH

            /// <summary>Returns the platform specific functions. (Facepunch.Steamworks)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                Debug.Log("[mod.io] User Data I/O being handled by Facepunch.Steamworks");

                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_Facepunch,
                    WriteFile = WriteFile_Facepunch,
                    DeleteFile = DeleteFile_Facepunch,
                    ClearAllData = ClearAllData_Facepunch,
                    UserDirectoryRoot = IOUtilities.CombinePath("modio", "users"),
                };
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
            private static void DeleteFile_Facepunch(string filePath, WriteFileCallback callback)
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
            private static void ClearAllData_Facepunch(WriteFileCallback callback)
            {
                var steamFiles = Steamworks.SteamRemoteStorage.Files;
                bool success = true;

                foreach(string filePath in steamFiles)
                {
                    if(filePath.StartsWith(UserDataStorage._USER_DIRECTORY_ROOT))
                    {
                        success = Steamworks.SteamRemoteStorage.FileDelete(filePath) && success;
                    }
                }

                if(callback != null) { callback.Invoke(success); }
            }

        #elif ENABLE_STEAMCLOUD_USERDATA_STEAMWORKSNET

            /// <summary>Returns the platform specific functions. (Steamworks.NET)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                Debug.Log("[mod.io] User Data I/O being handled by Steamworks.NET");

                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_SteamworksNET,
                    WriteFile = WriteFile_SteamworksNET,
                    DeleteFile = DeleteFile_SteamworksNET,
                    ClearAllData = ClearAllData_SteamworksNET,
                    UserDirectoryRoot = IOUtilities.CombinePath("modio", "users"),
                };
            }

            /// <summary>Reads a user data file. (Steamworks.NET)</summary>
            private static void ReadFile_SteamworksNET(string filePath, ReadFileCallback callback)
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
            private static void DeleteFile_SteamworksNET(string filePath, WriteFileCallback callback)
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
            private static void ClearAllData_SteamworksNET(WriteFileCallback callback)
            {
                int fileCount = Steamworks.SteamRemoteStorage.GetFileCount();
                bool success = true;

                for(int i = 0; i < fileCount; ++i)
                {
                    string filePath;
                    int fileSize;

                    filePath = Steamworks.SteamRemoteStorage.GetFileNameAndSize(i, out fileSize);

                    if(filePath.StartsWith(UserDataStorage._USER_DIRECTORY_ROOT))
                    {
                        success = Steamworks.SteamRemoteStorage.FileDelete(filePath) && success;
                    }
                }

                if(callback != null) { callback.Invoke(success); }
            }

        #else

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                string userDir = IOUtilities.CombinePath(UnityEngine.Application.persistentDataPath,
                                                         "modio-" + PluginSettings.data.gameId,
                                                         "users");

                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_Standalone,
                    WriteFile = WriteFile_Standalone,
                    DeleteFile = DeleteFile_Standalone,
                    ClearAllData = ClearAllData_Standalone,
                    UserDirectoryRoot = userDir,
                };
            }

            /// <summary>Reads a user data file. (Standalone Application)</summary>
            private static void ReadFile_Standalone(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                callback.Invoke(true, data);
            }

            /// <summary>Writes a user data file. (Standalone Application)</summary>
            private static void WriteFile_Standalone(string filePath, byte[] data, WriteFileCallback callback)
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
            private static void DeleteFile_Standalone(string filePath, WriteFileCallback callback)
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
            private static void ClearAllData_Standalone(WriteFileCallback callback)
            {
                bool success = IOUtilities.DeleteDirectory(UserDataStorage._USER_DIRECTORY_ROOT);

                if(callback != null) { callback.Invoke(success); }
            }

        #endif
    }
}
