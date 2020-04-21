using ModIO.DataStorageCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for the platform user data IO.</summary>
    public interface IPlatformUserDataIO
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        void ReadFile(string path, ReadFileCallback callback);

        /// <summary>Writes a file.</summary>
        void WriteFile(string path, byte[] data, WriteFileCallback callback);

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        void DeleteFile(string path, DeleteFileCallback callback);

        /// <summary>Moves a file.</summary>
        void MoveFile(string source, string destination, MoveFileCallback callback);

        /// <summary>Checks for the existence of a file.</summary>
        void GetFileExists(string path, GetFileExistsCallback callback);

        /// <summary>Gets the size of a file.</summary>
        void GetFileSize(string path, GetFileSizeCallback callback);

        /// <summary>Gets the size and md5 hash of a file.</summary>
        void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback callback);

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        void CreateDirectory(string path, CreateDirectoryCallback callback);

        /// <summary>Deletes a directory.</summary>
        void DeleteDirectory(string path, DeleteDirectoryCallback callback);

        /// <summary>Moves a directory.</summary>
        void MoveDirectory(string source, string destination, MoveDirectoryCallback callback);

        /// <summary>Gets the sub-directories at a location.</summary>
        void GetDirectories(string path, GetDirectoriesCallback callback);
    }
}
