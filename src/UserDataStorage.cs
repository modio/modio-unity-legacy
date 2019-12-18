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
        }

        // ---------[ IO FUNCTIONS ]---------
        /// <summary>Function used to read a user data file.</summary>
        public static bool TryReadJSONFile<T>(string filePathRelative, out T jsonObject)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);
            byte[] fileData = UserDataStorage._PlatformReadFile(filePath);
            return UserDataStorage.TryParseJSONFile(fileData, out jsonObject);;
        }

        /// <summary>Function used to read a user data file.</summary>
        public static bool TryWriteJSONFile<T>(string filePathRelative, T jsonObject)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            byte[] fileData = null;
            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);

            return(UserDataStorage.TryGenerateJSONFile(jsonObject, out fileData)
                   && UserDataStorage._PlatformWriteFile(filePath, fileData));
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
        public static byte[] ReadBinaryFile(string filePathRelative)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);
            return UserDataStorage._PlatformReadFile(filePath);
        }

        /// <summary>Function for writing a user-specific file.</summary>
        public static bool WriteBinaryFile(string filePathRelative, byte[] fileData)
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
            return UserDataStorage._PlatformWriteFile(filePath, fileData);
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static bool DeleteFile(string filePathRelative)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

            string filePath = IOUtilities.CombinePath(UserDataStorage._activeUserDirectory, filePathRelative);
            return UserDataStorage._PlatformDeleteFile(filePath);
        }

        /// <summary>Function for clearing all user data.</summary>
        public static void ClearAllData()
        {
            UserDataStorage._PlatformClearAllData();
        }

        // ---------[ PLATFORM SPECIFIC I/O ]---------
        /// <summary>Delegate for reading a file.</summary>
        private delegate byte[] ReadFileDelegate(string filePath);

        /// <summary>Delegate for writing a file.</summary>
        private delegate bool WriteFileDelegate(string filePath, byte[] fileData);

        /// <summary>Delegate for deleting a file.</summary>
        private delegate bool DeleteFileDelegate(string filePath);

        /// <summary>Delegate for clearing all data.</summary>
        private delegate void ClearAllDataDelegate();

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
            private static byte[] ReadFile_Editor(string filePath)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));

                byte[] data = null;
                data = IOUtilities.LoadBinaryFile(filePath);
                return data;
            }

            /// <summary>Write a user file. (Unity Editor)</summary>
            private static bool WriteFile_Editor(string filePath, byte[] data)
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

                return success;
            }

            /// <summary>Delete a user file. (Unity Editor)</summary>
            private static bool DeleteFile_Editor(string filePath)
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

                return success;
            }

            /// <summary>Clears all user data. (Unity Editor)</summary>
            private static void ClearAllData_Editor()
            {
                IOUtilities.DeleteDirectory(UserDataStorage._USER_DIRECTORY_ROOT);
                UnityEditor.AssetDatabase.Refresh();
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
                };
            }

            /// <summary>Loads the user data file. (Facepunch.Steamworks)</summary>
            public static byte[] ReadFile_Facepunch(string filePathRelative)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

                filePathRelative = IOUtilities.CombinePath("modio", filePathRelative);

                byte[] data = null;
                if(Steamworks.SteamRemoteStorage.FileExists(filePathRelative))
                {
                    data = Steamworks.SteamRemoteStorage.FileRead(filePathRelative);
                }

                return data;
            }

            /// <summary>Writes a user data file. (Facepunch.Steamworks)</summary>
            public static bool WriteFile_Facepunch(string filePathRelative, byte[] data)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
                Debug.Assert(data != null);

                filePathRelative = IOUtilities.CombinePath("modio", filePathRelative);

                return Steamworks.SteamRemoteStorage.FileWrite(filePathRelative, data);
            }

            /// <summary>Deletes a user data file. (Facepunch.Steamworks)</summary>
            private static bool DeleteFile_Facepunch(string filePathRelative)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

                filePathRelative = IOUtilities.CombinePath("modio", filePathRelative);

                if(Steamworks.SteamRemoteStorage.FileExists(filePathRelative))
                {
                    return Steamworks.SteamRemoteStorage.FileDelete(filePathRelative);
                }
                return true;
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
                };
            }

            /// <summary>Reads a user data file. (Steamworks.NET)</summary>
            private static byte[] ReadFile_SteamworksNET(string filePathRelative)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

                filePathRelative = IOUtilities.CombinePath("modio", filePathRelative);

                byte[] data = null;
                if(Steamworks.SteamRemoteStorage.FileExists(filePathRelative))
                {
                    int fileSize = Steamworks.SteamRemoteStorage.GetFileSize(filePathRelative);

                    if(fileSize > 0)
                    {
                        data = new byte[fileSize];
                        Steamworks.SteamRemoteStorage.FileRead(filePathRelative, data, fileSize);
                    }
                }

                return data;
            }

            /// <summary>Writes a user data file. (Steamworks.NET)</summary>
            public static bool WriteFile_SteamworksNET(string filePathRelative, byte[] data)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
                Debug.Assert(data != null);

                filePathRelative = IOUtilities.CombinePath("modio", filePathRelative);

                return Steamworks.SteamRemoteStorage.FileWrite(filePathRelative, data, data.Length);
            }

            /// <summary>Deletes a user data file. (Steamworks.NET)</summary>
            private static bool DeleteFile_SteamworksNET(string filePathRelative)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

                filePathRelative = IOUtilities.CombinePath("modio", filePathRelative);

                if(Steamworks.SteamRemoteStorage.FileExists(filePathRelative))
                {
                    return Steamworks.SteamRemoteStorage.FileDelete(filePathRelative);
                }
                return true;
            }

        #elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX

            /// <summary>Defines the base directory for the user-specific data.</summary>
            private static readonly string USER_DIRECTORY
            = IOUtilities.CombinePath(UnityEngine.Application.persistentDataPath, "modio-" + PluginSettings.data.gameId);

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            private static PlatformFunctions GetPlatformFunctions()
            {
                Debug.Log("[mod.io] User Data Directory set: " + USER_DIRECTORY);

                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_Standalone,
                    WriteFile = WriteFile_Standalone,
                    DeleteFile = DeleteFile_Standalone,
                };
            }

            /// <summary>Reads a user data file. (Standalone Application)</summary>
            private static byte[] ReadFile_Standalone(string filePathRelative)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

                byte[] data = null;
                string filePathAbs = IOUtilities.CombinePath(UserDataStorage.USER_DIRECTORY,
                                                             filePathRelative);
                data = IOUtilities.LoadBinaryFile(filePathAbs);
                return data;
            }

            /// <summary>Writes a user data file. (Standalone Application)</summary>
            private static bool WriteFile_Standalone(string filePathRelative, byte[] data)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));
                Debug.Assert(data != null);

                bool success = false;
                string filePathAbs = IOUtilities.CombinePath(UserDataStorage.USER_DIRECTORY,
                                                             filePathRelative);
                success = IOUtilities.WriteBinaryFile(filePathAbs, data);
                return success;
            }

            /// <summary>Deletes a user data file. (Standalone Application)</summary>
            private static bool DeleteFile_Standalone(string filePathRelative)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePathRelative));

                bool success = false;
                string filePathAbs = IOUtilities.CombinePath(UserDataStorage.USER_DIRECTORY,
                                                             filePathRelative);
                success = IOUtilities.DeleteFile(filePathAbs);
                return success;
            }

        #endif
    }
}
