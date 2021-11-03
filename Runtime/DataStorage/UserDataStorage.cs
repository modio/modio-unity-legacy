// #define DISABLE_EDITOR_CODEPATH
// #define MODIO_FACEPUNCH_SUPPORT
// #define MODIO_STEAMWORKSNET_SUPPORT

using Debug = UnityEngine.Debug;

using ModIO.UserDataIOCallbacks;

namespace ModIO
{
    /// <summary>Functions for user-specific data I/O.</summary>
    public static class UserDataStorage
    {
        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly IUserDataIO PLATFORM_IO;

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

            UserDataStorage.PLATFORM_IO.InitializeForDefaultUser((success) => {
                if(success)
                {
                    LocalUser.Load();
                }
            });
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void SetActiveUser<T>(T platformUserHandle, SetActiveUserCallback<T> callback)
        {
            if(UserDataStorage.PLATFORM_IO is IUserDataIO<T>)
            {
                ((IUserDataIO<T>)UserDataStorage.PLATFORM_IO)
                    .SetActiveUser(platformUserHandle, (id, success) => {
                        if(success)
                        {
                            LocalUser.Load(() => {
                                if(callback != null)
                                {
                                    callback.Invoke(id, success);
                                }
                            });
                        }
                        else
                        {
                            LocalUser.instance = new LocalUser();

                            Debug.Log("[mod.io] Failed to set active user. LocalUser cleared.");

                            if(callback != null)
                            {
                                callback.Invoke(id, success);
                            }
                        }
                    });
            }
            else
            {
                Debug.LogWarning(
                    "[mod.io] Attempt to call SetActiveUser with a type of: " + typeof(T).ToString()
                    + "\nThis type of user handle is unsupported by the assigned IUserDataIO implementation: "
                    + (UserDataStorage.PLATFORM_IO == null
                           ? "NULL"
                           : UserDataStorage.PLATFORM_IO.GetType().ToString()));

                if(callback != null)
                {
                    callback.Invoke(platformUserHandle, false);
                }
            }
        }

        // ---------[ Directories ]---------
        /// <summary>The directory for the active user's data.</summary>
        public static string USER_DIRECTORY
        {
            get {
                return UserDataStorage.PLATFORM_IO.UserDirectory;
            }
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Function for reading a user-specific file.</summary>
        public static void ReadFile(string relativePath, ReadFileCallback callback)
        {
            UserDataStorage.PLATFORM_IO.ReadFile(relativePath, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void ReadJSONFile<T>(string relativePath, ReadJSONFileCallback<T> callback)
        {
            UserDataStorage.ReadFile(relativePath, (p, success, fileData) => {
                T jsonObject;

                if(success)
                {
                    success = IOUtilities.TryParseUTF8JSONData(fileData, out jsonObject);
                }
                else
                {
                    jsonObject = default(T);
                }

                callback.Invoke(relativePath, success, jsonObject);
            });
        }

        /// <summary>Function for writing a user-specific file.</summary>
        public static void WriteFile(string relativePath, byte[] data, WriteFileCallback callback)
        {
            Debug.Assert(data != null);

#if DEBUG
            if(data.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + relativePath);
            }
#endif // DEBUG

            UserDataStorage.PLATFORM_IO.WriteFile(relativePath, data, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void WriteJSONFile<T>(string relativePath, T jsonObject,
                                            WriteFileCallback callback)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                UserDataStorage.WriteFile(relativePath, data, callback);
            }
            else
            {
                Debug.LogWarning(
                    "[mod.io] Failed create JSON representation of object before writing file."
                    + "\nFile: " + relativePath + "\n\n");

                if(callback != null)
                {
                    callback.Invoke(relativePath, false);
                }
            }
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static void DeleteFile(string relativePath, DeleteFileCallback callback)
        {
            UserDataStorage.PLATFORM_IO.DeleteFile(relativePath, callback);
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
    public class FacepunchUserDataIO : IUserDataIO
    {
        // ---------[ Constants ]---------
        /// <summary>The root directory for active user directories.</summary>
        protected const string ROOT_DIR = "mod.io";

        /// <summary>Directory to use for user data.</summary>
        public string UserDirectory { get; private set; }

        // --- Initialization ---
        public FacepunchUserDataIO()
        {
            this.UserDirectory = FacepunchUserDataIO.ROOT_DIR;
        }

        /// <summary>Initializes the storage system for the defaul user.</summary>
        public void InitializeForDefaultUser(Action<bool> callback)
        {
            this.SetActiveUser(null, (userId, success) => {
                if(callback != null)
                {
                    callback.Invoke(success);
                }
            });
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback)
        {
            this.UserDirectory = this.GenerateActiveUserDirectory(platformUserId);

            if(callback != null)
            {
                callback.Invoke(platformUserId, true);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback)
        {
            this.UserDirectory = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

            if(callback != null)
            {
                callback.Invoke(platformUserId, true);
            }
        }

        /// <summary>Determines the user directory for a given user id.</summary>
        protected string GenerateActiveUserDirectory(string platformUserId)
        {
            string userDir = FacepunchUserDataIO.ROOT_DIR;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                userDir = IOUtilities.CombinePath(FacepunchUserDataIO.ROOT_DIR, folderName);
            }

            return userDir;
        }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public void ReadFile(string relativePath, ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.UserDirectory, relativePath);
            byte[] data = null;
            if(Steamworks.SteamRemoteStorage.FileExists(path))
            {
                data = Steamworks.SteamRemoteStorage.FileRead(path);
            }

            callback.Invoke(relativePath, (data != null), data);
        }

        /// <summary>Writes a file.</summary>
        public void WriteFile(string relativePath, byte[] data, WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(data != null);

            string path = IOUtilities.CombinePath(this.UserDirectory, relativePath);
            bool success = Steamworks.SteamRemoteStorage.FileWrite(path, data);

            if(callback != null)
            {
                callback.Invoke(relativePath, success);
            }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public void DeleteFile(string relativePath, DeleteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(this.UserDirectory, relativePath);
            bool success = true;

            if(Steamworks.SteamRemoteStorage.FileExists(path))
            {
                success = Steamworks.SteamRemoteStorage.FileDelete(path);
            }

            if(callback != null)
            {
                callback.Invoke(relativePath, success);
            }
        }

        /// <summary>Clears all of the active user's data.</summary>
        public void ClearActiveUserData(ClearActiveUserDataCallback callback)
        {
            var steamFiles = Steamworks.SteamRemoteStorage.Files;
            bool success = true;

            foreach(string path in steamFiles)
            {
                if(path.StartsWith(FacepunchUserDataIO.ROOT_DIR))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(path) && success;
                }
            }

            if(callback != null)
            {
                callback.Invoke(success);
            }
        }
    }

#elif MODIO_STEAMWORKSNET_SUPPORT

    /// <summary>Steamworks.NET User Data I/O interface.</summary>
    public class SteamworksNETUserDataIO : IUserDataIO
    {
        // ---------[ Constants ]---------
        /// <summary>The root directory for active user directories.</summary>
        protected const string ROOT_DIR = "mod.io";

        /// <summary>Directory to use for user data.</summary>
        public string UserDirectory { get; private set; }

        // --- Initialization ---
        public SteamworksNETUserDataIO()
        {
            this.UserDirectory = SteamworksNETUserDataIO.ROOT_DIR;
        }

        /// <summary>Initializes the storage system for the defaul user.</summary>
        public void InitializeForDefaultUser(Action<bool> callback)
        {
            this.SetActiveUser(null, (userId, success) => {
                if(callback != null)
                {
                    callback.Invoke(success);
                }
            });
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback)
        {
            this.UserDirectory = this.GenerateActiveUserDirectory(platformUserId);

            if(callback != null)
            {
                callback.Invoke(platformUserId, true);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback)
        {
            this.UserDirectory = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

            if(callback != null)
            {
                callback.Invoke(platformUserId, true);
            }
        }

        /// <summary>Determines the user directory for a given user id.</summary>
        protected string GenerateActiveUserDirectory(string platformUserId)
        {
            string userDir = SteamworksNETUserDataIO.ROOT_DIR;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                userDir = IOUtilities.CombinePath(SteamworksNETUserDataIO.ROOT_DIR, folderName);
            }

            return userDir;
        }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public void ReadFile(string relativePath, ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.UserDirectory, relativePath);
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

            callback.Invoke(relativePath, (data != null), data);
        }

        /// <summary>Writes a file.</summary>
        public void WriteFile(string relativePath, byte[] data, WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(data != null);

            string path = IOUtilities.CombinePath(this.UserDirectory, relativePath);
            bool success = Steamworks.SteamRemoteStorage.FileWrite(path, data, data.Length);

            if(callback != null)
            {
                callback.Invoke(relativePath, success);
            }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public void DeleteFile(string relativePath, DeleteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(this.UserDirectory, relativePath);
            bool success = true;

            if(Steamworks.SteamRemoteStorage.FileExists(path))
            {
                success = Steamworks.SteamRemoteStorage.FileDelete(path);
            }

            if(callback != null)
            {
                callback.Invoke(relativePath, success);
            }
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

                if(path.StartsWith(SteamworksNETUserDataIO.ROOT_DIR))
                {
                    success = Steamworks.SteamRemoteStorage.FileDelete(path) && success;
                }
            }

            if(callback != null)
            {
                callback.Invoke(success);
            }
        }
    }

#endif // ---[ Futher User Data Interfaces ]---
}
