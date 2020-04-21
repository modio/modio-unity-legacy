using ModIO.DataStorageCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIOAsync
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        void ReadFile(string filePath, ReadFileCallback callback);

        /// <summary>Writes a file.</summary>
        void WriteFile(string filePath, byte[] data, WriteFileCallback callback);

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        void DeleteFile(string filePath, DeleteFileCallback callback);

        /// <summary>Moves a file.</summary>
        void MoveFile(string sourceFilePath, string destinationFilePath, MoveFileCallback callback);

        /// <summary>Checks for the existence of a file.</summary>
        void GetFileExists(string filePath, GetFileExistsCallback callback);

        /// <summary>Gets the size of a file.</summary>
        void GetFileSize(string filePath, GetFileSizeCallback callback);

        /// <summary>Gets the size and md5 hash of a file.</summary>
        void GetFileSizeAndHash(string filePath, GetFileSizeAndHashCallback callback);

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        void CreateDirectory(string directoryPath, CreateDirectoryCallback callback);

        /// <summary>Deletes a directory.</summary>
        void DeleteDirectory(string directoryPath, DeleteDirectoryCallback callback);

        /// <summary>Moves a directory.</summary>
        void MoveDirectory(string sourcePath, string destinationPath, MoveDirectoryCallback callback);

        /// <summary>Gets the sub-directories at a location.</summary>
        void GetDirectories(string directoryPath, GetDirectoriesCallback callback);
    }
}
