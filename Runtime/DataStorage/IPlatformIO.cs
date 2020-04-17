using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Delegate for reading a file.</summary>
        byte[] ReadFile(string filePath);

        /// <summary>Delegate for writing a file.</summary>
        bool WriteFile(string filePath, byte[] data);

        // --- File Management ---
        /// <summary>Delegate for deleting a file.</summary>
        bool DeleteFile(string filePath);

        /// <summary>Delegate for moving a file.</summary>
        bool MoveFile(string sourceFilePath, string destinationFilePath);

        /// <summary>Gets the size of a file.</summary>
        bool GetFileExists(string filePath);

        /// <summary>Delegate for getting a file's size.</summary>
        Int64 GetFileSize(string filePath);

        /// <summary>Delegate for getting a file's size and md5 hash.</summary>
        bool GetFileSizeAndHash(string filePath, out Int64 byteCount, out string md5Hash);

        // --- Directory Management ---
        /// <summary>Delegate for creating a directory.</summary>
        bool CreateDirectory(string directoryPath);

        /// <summary>Delegate for deleting a directory.</summary>
        bool DeleteDirectory(string directoryPath);

        /// <summary>Moves a directory.</summary>
        bool MoveDirectory(string sourcePath, string destinationPath);

        /// <summary>Delegate for getting the directories at a location.</summary>
        IList<string> GetDirectories(string directoryPath);
    }
}
