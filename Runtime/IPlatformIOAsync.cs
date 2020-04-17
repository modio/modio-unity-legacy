using ModIO.DataStorageCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIOAsync
    {
        // --- File I/O ---
        /// <summary>Delegate for reading a file.</summary>
        void ReadFile(string filePath, ReadFileCallback callback);

        /// <summary>Delegate for writing a file.</summary>
        void WriteFile(string filePath, byte[] data, WriteFileCallback callback);

        // --- File Management ---
        /// <summary>Delegate for deleting a file.</summary>
        void DeleteFile(string filePath, DeleteFileCallback callback);

        /// <summary>Delegate for moving a file.</summary>
        void MoveFile(string sourceFilePath, string destinationFilePath, MoveFileCallback callback);

        /// <summary>Gets the size of a file.</summary>
        void GetFileExists(string filePath, GetFileExistsCallback callback);

        /// <summary>Delegate for getting a file's size.</summary>
        void GetFileSize(string filePath, GetFileSizeCallback callback);

        /// <summary>Delegate for getting a file's size and md5 hash.</summary>
        void GetFileSizeAndHash(string filePath, GetFileSizeAndHashCallback callback);

        // --- Directory Management ---
        /// <summary>Delegate for creating a directory.</summary>
        void CreateDirectory(string directoryPath, CreateDirectoryCallback callback);

        /// <summary>Delegate for deleting a directory.</summary>
        void DeleteDirectory(string directoryPath, DeleteDirectoryCallback callback);

        /// <summary>Moves a directory.</summary>
        void MoveDirectory(string sourcePath, string destinationPath, MoveDirectoryCallback callback);

        /// <summary>Delegate for getting the directories at a location.</summary>
        void GetDirectories(string directoryPath, GetDirectoriesCallback callback);
    }
}
