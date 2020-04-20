using System;
using System.Collections.Generic;

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

        /// <summary>Gets the size of a file.</summary>
        bool GetFileExists(string path);

        /// <summary>Gets the size of a file.</summary>
        Int64 GetFileSize(string path);

        /// <summary>Gets the size and md5 hash of a file.</summary>
        bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash);

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        bool CreateDirectory(string path);

        /// <summary>Deletes a directory.</summary>
        bool DeleteDirectory(string path);

        /// <summary>Moves a directory.</summary>
        bool MoveDirectory(string source, string destination);

        /// <summary>Gets the sub-directories at a location.</summary>
        IList<string> GetDirectories(string path);
    }
}
