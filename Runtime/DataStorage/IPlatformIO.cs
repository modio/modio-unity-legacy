using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Defines the functions necessary for a complete platform IO.</summary>
    public interface IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        byte[] ReadFile(string filePath);

        /// <summary>Writes a file.</summary>
        bool WriteFile(string filePath, byte[] data);

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        bool DeleteFile(string filePath);

        /// <summary>Moves a file.</summary>
        bool MoveFile(string sourceFilePath, string destinationFilePath);

        /// <summary>Gets the size of a file.</summary>
        bool GetFileExists(string filePath);

        /// <summary>Gets the size of a file.</summary>
        Int64 GetFileSize(string filePath);

        /// <summary>Gets the size and md5 hash of a file.</summary>
        bool GetFileSizeAndHash(string filePath, out Int64 byteCount, out string md5Hash);

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        bool CreateDirectory(string directoryPath);

        /// <summary>Deletes a directory.</summary>
        bool DeleteDirectory(string directoryPath);

        /// <summary>Moves a directory.</summary>
        bool MoveDirectory(string sourcePath, string destinationPath);

        /// <summary>Gets the sub-directories at a location.</summary>
        IList<string> GetDirectories(string directoryPath);
    }
}
