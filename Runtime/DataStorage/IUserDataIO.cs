using ModIO.UserDataIOCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for the platform user data IO.</summary>
    public interface IUserDataIO
    {
        // --- Accessors ---
        /// <summary>Active User Data directory.</summary>
        string ActiveUserDirectory { get; }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        void ReadFile(string pathRelative, ReadFileCallback callback);

        /// <summary>Writes a file.</summary>
        void WriteFile(string pathRelative, byte[] data, WriteFileCallback callback);

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        void DeleteFile(string pathRelative, DeleteFileCallback callback);

        /// <summary>Clears all of the active user's data.</summary>
        void ClearActiveUserData(ClearActiveUserDataCallback callback);
    }

    /// <summary>Adds SetActiveUser to the IUserDataIO interface.</summary>
    public interface IUserDataIO<TPlatformUserIdentifier> : IUserDataIO
    {
        // --- Initialization ---
        /// <summary>Initializes the storage system for the given user.</summary>
        void SetActiveUser(TPlatformUserIdentifier platformUserId, SetActiveUserCallback<TPlatformUserIdentifier> callback);
    }
}
