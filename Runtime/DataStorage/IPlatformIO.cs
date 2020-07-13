using System;
using System.Collections.Generic;

using ModIO.PlatformIOCallbacks;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        bool ReadFile(string path, out byte[] data);

        /// <summary>Writes a file.</summary>
        bool WriteFile(string path, byte[] data);

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        bool DeleteFile(string path);

        /// <summary>Moves a file.</summary>
        bool MoveFile(string source, string destination);

        /// <summary>Checks for the existence of a file.</summary>
        bool GetFileExists(string path);

        /// <summary>Gets the size of a file.</summary>
        Int64 GetFileSize(string path);

        /// <summary>Gets the size and md5 hash of a file.</summary>
        bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash);

        /// <summary>Gets the files at a location.</summary>
        IList<string> GetFiles(string path, string nameFilter, bool recurseSubdirectories);

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        bool CreateDirectory(string path);

        /// <summary>Deletes a directory.</summary>
        bool DeleteDirectory(string path);

        /// <summary>Moves a directory.</summary>
        bool MoveDirectory(string source, string destination);

        /// <summary>Checks for the existence of a directory.</summary>
        bool GetDirectoryExists(string path);

        /// <summary>Gets the sub-directories at a location.</summary>
        IList<string> GetDirectories(string path);
    }

    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIO_Async
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

        /// <summary>Gets the size and md5 hash of a file.</summary>
        void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback callback);

        /// <summary>Gets the files at a location.</summary>
        void GetFiles(string path, string nameFilter, bool recurseSubdirectories, GetFilesCallback callback);

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
