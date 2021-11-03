using ModIO.PlatformIOCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIO
    {
        // --- Directories ---
        /// <summary>Directory to use for mod installations</summary>
        string InstallationDirectory { get; }

        /// <summary>Directory to use for cached server data</summary>
        string CacheDirectory { get; }

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

        /// <summary>Gets the size and md5 hash of a file.</summary>
        void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback callback);

        /// <summary>Gets the files at a location.</summary>
        void GetFiles(string path, string nameFilter, bool recurseSubdirectories,
                      GetFilesCallback callback);

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        void CreateDirectory(string path, CreateDirectoryCallback callback);

        /// <summary>Deletes a directory.</summary>
        void DeleteDirectory(string path, DeleteDirectoryCallback callback);

        /// <summary>Moves a directory.</summary>
        void MoveDirectory(string source, string destination, MoveDirectoryCallback callback);

        /// <summary>Checks for the existence of a directory.</summary>
        void GetDirectoryExists(string path, GetDirectoryExistsCallback callback);

        /// <summary>Gets the sub-directories at a location.</summary>
        void GetDirectories(string path, GetDirectoriesCallback callback);
    }
}
