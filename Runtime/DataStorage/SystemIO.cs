using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class SystemIO : IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public byte[] ReadFile(string filePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Writes a file.</summary>
        public bool WriteFile(string filePath, byte[] data)
        {
            throw new System.NotImplementedException();
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public bool DeleteFile(string filePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Moves a file.</summary>
        public bool MoveFile(string sourceFilePath, string destinationFilePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the size of a file.</summary>
        public bool GetFileExists(string filePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the size of a file.</summary>
        public Int64 GetFileSize(string filePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public bool GetFileSizeAndHash(string filePath, out Int64 byteCount, out string md5Hash)
        {
            throw new System.NotImplementedException();
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public bool CreateDirectory(string directoryPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Deletes a directory.</summary>
        public bool DeleteDirectory(string directoryPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Moves a directory.</summary>
        public bool MoveDirectory(string sourcePath, string destinationPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public IList<string> GetDirectories(string directoryPath)
        {
            throw new System.NotImplementedException();
        }
    }
}
