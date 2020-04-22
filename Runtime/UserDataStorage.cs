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
                UserDataStorage.PLATFORM_IO = new SteamworksNETUserDataIO();
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
        public static void ReadFile(string relativePath, ReadFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(UserDataStorage.PLATFORM_IO.activeUserDirectory,
                                                  relativePath);
            UserDataStorage.PLATFORM_IO.ReadFile(path, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void ReadJSONFile<T>(string relativePath, ReadJSONFileCallback<T> callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(callback != null);

            UserDataStorage.ReadFile(relativePath, (path, success, fileData) =>
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
        public static void WriteFile(string relativePath, byte[] fileData, WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(fileData != null);

            #if DEBUG
            if(fileData.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + relativePath);
            }
            #endif // DEBUG

            string path = IOUtilities.CombinePath(UserDataStorage.PLATFORM_IO.activeUserDirectory,
                                                  relativePath);
            UserDataStorage.PLATFORM_IO.WriteFile(path, fileData, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void WriteJSONFile<T>(string relativePath, T jsonObject, WriteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);

            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                UserDataStorage.WriteFile(relativePath, data, callback);
            }
            else if(callback != null)
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + relativePath + "\n\n");

                string path = IOUtilities.CombinePath(UserDataStorage.PLATFORM_IO.activeUserDirectory,
                                                      relativePath);
                callback.Invoke(path, false);
            }
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static void DeleteFile(string relativePath, DeleteFileCallback callback)
        {
            Debug.Assert(UserDataStorage.isInitialized);
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(UserDataStorage.PLATFORM_IO.activeUserDirectory,
                                                  relativePath);
            UserDataStorage.PLATFORM_IO.DeleteFile(path, callback);
        }

        /// <summary>Function for clearing of the active user's data.</summary>
        public static void ClearActiveUserData(ClearActiveUserDataCallback callback)
        {
            UserDataStorage.PLATFORM_IO.ClearActiveUserData(callback);
        }
    }

    // ---------[ Further User Data Interfaces ]---------
    #if MODIO_FACEPUNCH_SUPPORT

        /// <summary>Facepunch User Data I/O interface.</summary>
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

            /// <summary>Determines the user directory for a given user id.</summary>
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

        /// <summary>Steamworks.NET User Data I/O interface.</summary>
        public class SteamworksNETUserDataIO : IPlatformUserDataIO
        {
            // ---------[ Constants ]---------
            /// <summary>Defines the base directory for the user-specific data.</summary>
            public static readonly string USER_DIR_ROOT = IOUtilities.CombinePath("mod.io");

            // ---------[ Fields ]--------
            /// <summary>The directory for the active user's data.</summary>
            public string userDir = SteamworksNETUserDataIO.USER_DIR_ROOT;

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

            /// <summary>Determines the user directory for a given user id.</summary>
            protected string GenerateActiveUserDirectory(string platformUserId)
            {
                string userDir = SteamworksNETUserDataIO.USER_DIR_ROOT;

                if(!string.IsNullOrEmpty(platformUserId))
                {
                    string folderName = IOUtilities.MakeValidFileName(platformUserId);
                    userDir = IOUtilities.CombinePath(SteamworksNETUserDataIO.USER_DIR_ROOT, folderName);
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
                    int fileSize = Steamworks.SteamRemoteStorage.GetFileSize(path);

                    if(fileSize > 0)
                    {
                        data = new byte[fileSize];
                        Steamworks.SteamRemoteStorage.FileRead(path, data, fileSize);
                    }
                }

                callback.Invoke(path, (data != null), data);
            }

            /// <summary>Writes a file.</summary>
            public void WriteFile(string path, byte[] data, WriteFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(data != null);

                bool success = Steamworks.SteamRemoteStorage.FileWrite(path, data, data.Length);

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
                int fileCount = Steamworks.SteamRemoteStorage.GetFileCount();
                bool success = true;

                for(int i = 0; i < fileCount; ++i)
                {
                    string path;
                    int fileSize;

                    path = Steamworks.SteamRemoteStorage.GetFileNameAndSize(i, out fileSize);

                    if(path.StartsWith(SteamworksNETUserDataIO.USER_DIR_ROOT))
                    {
                        success = Steamworks.SteamRemoteStorage.FileDelete(path) && success;
                    }
                }

                if(callback != null) { callback.Invoke(success); }
            }
        }

    #endif // MODIO_STEAMWORKSNET_SUPPORT
}
