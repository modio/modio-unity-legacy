using ModIO.UserDataIOCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for the platform user data IO.</summary>
    public interface IPlatformUserDataIO
    {
        // --- Initialization ---
        /// <summary>Initializes the storage system for the given user.</summary>
        void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback);

        /// <summary>Initializes the storage system for the given user.</summary>
        void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback);

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        void ReadFile(string pathRelative, ReadFileCallback callback);

        /// <summary>Writes a file.</summary>
        void WriteFile(string pathRelative, byte[] data, WriteFileCallback callback);

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        void DeleteFile(string pathRelative, DeleteFileCallback callback);

        /// <summary>Checks for the existence of a file.</summary>
        void GetFileExists(string pathRelative, GetFileExistsCallback callback);

        /// <summary>Gets the size of a file.</summary>
        void GetFileSize(string pathRelative, GetFileSizeCallback callback);

        /// <summary>Gets the size and md5 hash of a file.</summary>
        void GetFileSizeAndHash(string pathRelative, GetFileSizeAndHashCallback callback);

        /// <summary>Clears all of the active user's data.</summary>
        void ClearActiveUserData(ClearActiveUserDataCallback callback);
    }
}
