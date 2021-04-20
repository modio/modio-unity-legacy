using ModIO.UserDataIOCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for the platform user data IO.</summary>
    public interface IUserDataIO
    {
        // --- Directories ---
        /// <summary>The directory for the active user's data.</summary>
        string UserDirectory { get; }

        // --- Initialization ---
        /// <summary>Initializes the storage system for the default user.</summary>
        void InitializeForDefaultUser(System.Action<bool> callback);

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

    /// <summary>Defines the functions necessary for the platform user data IO.</summary>
    public interface IUserDataIO<T> : IUserDataIO
    {
        /// <summary>Initializes the storage system for the given user.</summary>
        void SetActiveUser(T platformUserId, SetActiveUserCallback<T> callback);
    }
}
