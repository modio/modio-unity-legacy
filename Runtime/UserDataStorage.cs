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
                UserDataStorage.PLATFORM_IO = new FacepunchUserDataIO();
            #elif MODIO_STEAMWORKSNET_SUPPORT
                UserDataStorage.PLATFORM = UserDataStorage.GetPlatformFunctions_SteamworksNET();
            #else
                UserDataStorage.PLATFORM_IO = new SystemIOWrapper();
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
    }

    // ---------[ Further User Data Interfaces ]---------
    #if MODIO_FACEPUNCH_SUPPORT

        /// <summary>Facepunch User Data I/O interface</summary>
        public class FacepunchUserDataIO : IPlatformUserDataIO
        {
            // ---------[ Constants ]---------
            /// <summary>Defines the base directory for the user-specific data.</summary>
            public static readonly string USER_DIR_ROOT = IOUtilities.CombinePath("mod.io");

            // ---------[ Fields ]--------
            /// <summary>The directory for the active user's data.</summary>
            public string userDir = FacepunchUserDataIO.USER_DIR_ROOT;

            /// <summary>Gets the directory for the active user's data.</summary>
            public string activeUserDirectory { get; set; }

            // --- Initialization ---
            /// <summary>Initializes the storage system for the given user.</summary>
            public void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback)
            {
                this.userDir = this.GenerateActiveUserDirectory(platformUserId);

                if(callback != null)
                {
                    callback.Invoke(platformUserId, true);
                }
            }

            /// <summary>Initializes the storage system for the given user.</summary>
            public void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback)
            {
                this.userDir = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

                if(callback != null)
                {
                    callback.Invoke(platformUserId, true);
                }
            }

            /// <summary>Determines the user directory for a given user id..</summary>
            protected string GenerateActiveUserDirectory(string platformUserId)
            {
                string userDir = FacepunchUserDataIO.USER_DIR_ROOT;

                if(!string.IsNullOrEmpty(platformUserId))
                {
                    string folderName = IOUtilities.MakeValidFileName(platformUserId);
                    userDir = IOUtilities.CombinePath(FacepunchUserDataIO.USER_DIR_ROOT, folderName);
                }

                return userDir;
            }

            // --- File I/O ---
            /// <summary>Reads a file.</summary>
            public void ReadFile(string path, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(callback != null);

                byte[] data = null;
                if(Steamworks.SteamRemoteStorage.FileExists(path))
                {
                    data = Steamworks.SteamRemoteStorage.FileRead(path);
                }

                callback.Invoke(path, (data != null), data);
            }

            /// <summary>Writes a file.</summary>
            public void WriteFile(string path, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(data != null);

                bool success = Steamworks.SteamRemoteStorage.FileWrite(path, data);

                if(callback != null)
                {
                    callback.Invoke(path, success);
                }
            }

            // --- File Management ---
            /// <summary>Deletes a file.</summary>
            public void DeleteFile(string path, DeleteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));

                bool success = true;

                if(Steamworks.SteamRemoteStorage.FileExists(path))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(path);
                }

                if(callback != null)
                {
                    callback.Invoke(path, success);
                }
            }

            /// <summary>Moves a file.</summary>
            public void MoveFile(string source, string destination, MoveFileCallback callback)
            {
                throw new System.NotImplementedException();
            }

            /// <summary>Checks for the existence of a file.</summary>
            public void GetFileExists(string path, GetFileExistsCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(callback != null);

                bool fileExists = Steamworks.SteamRemoteStorage.FileExists(path);
                callback.Invoke(path, fileExists);
            }

            /// <summary>Gets the size of a file.</summary>
            public void GetFileSize(string path, GetFileSizeCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(callback != null);

                int fileSize = Steamworks.SteamRemoteStorage.FileSize(path);
                callback.Invoke(path, (Int64)fileSize);
            }

            /// <summary>Gets the size and md5 hash of a file.</summary>
            public void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(callback != null);

                byte[] data = null;
                Int64 byteCount = -1;
                string md5Hash = null;

                if(Steamworks.SteamRemoteStorage.FileExists(path))
                {
                    data = Steamworks.SteamRemoteStorage.FileRead(path);

                    if(data != null)
                    {
                        byteCount = data.Length;

                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        {
                            var hash = md5.ComputeHash(data);
                            md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }

                callback.Invoke(path, (data != null), byteCount, md5Hash);
            }

            /// <summary>Clears all of the active user's data.</summary>
            public void ClearActiveUserData(ClearActiveUserDataCallback callback)
            {
                var steamFiles = Steamworks.SteamRemoteStorage.Files;
                bool success = true;

                foreach(string path in steamFiles)
                {
                    if(path.StartsWith(FacepunchUserDataIO.USER_DIR_ROOT))
                    {
                        success = Steamworks.SteamRemoteStorage.FileDelete(path) && success;
                    }
                }

                if(callback != null) { callback.Invoke(success); }
            }
        }

    #endif // MODIO_FACEPUNCH_SUPPORT

    #if MODIO_STEAMWORKSNET_SUPPORT

        /// <summary>Steamworks.NET </summary>
        public class SteamworksNETUserDataIO
        {
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
        }

    #endif // MODIO_STEAMWORKSNET_SUPPORT


}
