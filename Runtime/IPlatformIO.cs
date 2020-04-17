using ModIO.DataStorageCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Delegate for reading a file.</summary>
        void ReadFile(string filePath, LocalDataStorage.ReadFileCallback callback);

        /// <summary>Delegate for writing a file.</summary>
        void WriteFile(string filePath, byte[] data, LocalDataStorage.WriteFileCallback callback);

        // --- File Management ---
        /// <summary>Delegate for deleting a file.</summary>
        void DeleteFile(string filePath, LocalDataStorage.DeleteCallback callback);

        /// <summary>Delegate for moving a file.</summary>
        void MoveFile(string sourceFilePath, string destinationFilePath, LocalDataStorage.MoveCallback callback);

        /// <summary>Gets the size of a file.</summary>
        void GetFileExists(string filePath, LocalDataStorage.GetExistsCallback callback);

        /// <summary>Delegate for getting a file's size.</summary>
        void GetFileSize(string filePath, LocalDataStorage.GetFileSizeCallback callback);

        /// <summary>Delegate for getting a file's size and md5 hash.</summary>
        void GetFileSizeAndHash(string filePath, LocalDataStorage.GetFileSizeAndHashCallback callback);

        // --- Directory Management ---
        /// <summary>Delegate for creating a directory.</summary>
        void CreateDirectory(string directoryPath, LocalDataStorage.CreateCallback callback);

        /// <summary>Delegate for deleting a directory.</summary>
        void DeleteDirectory(string directoryPath, LocalDataStorage.DeleteCallback callback);

        /// <summary>Moves a directory.</summary>
        void MoveDirectory(string sourcePath, string destinationPath, LocalDataStorage.MoveCallback callback);

        /// <summary>Delegate for getting the directories at a location.</summary>
        void GetDirectories(string directoryPath, LocalDataStorage.GetDirectoriesCallback callback);
    }
}
