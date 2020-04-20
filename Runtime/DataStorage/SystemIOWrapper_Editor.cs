#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality and adds AssetDatabase refreshes.</summary>
    public class SystemIOWrapper_Editor : SystemIOWrapper
    {
        /// <summary>Determines whether an AssetDatabase refresh is applicable.</summary>
        public bool IsPathWithinEditorAssetDatabase(string path)
        {
            return path.StartsWith(Application.dataPath);
        }

        // ---------[ IPlatformIO Interface ]---------
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public override bool ReadFile(string path, out byte[] data)
        {
            return base.ReadFile(path, out data);
        }

        /// <summary>Writes a file.</summary>
        public override bool WriteFile(string path, byte[] data)
        {
            return base.WriteFile(path, data);
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public override bool DeleteFile(string path)
        {
            return base.DeleteFile(path);
        }

        /// <summary>Moves a file.</summary>
        public override bool MoveFile(string source, string destination)
        {
            return base.MoveFile(source, destination);
        }

        /// <summary>Checks for the existence of a file.</summary>
        public override bool GetFileExists(string path)
        {
            return base.GetFileExists(path);
        }

        /// <summary>Gets the size of a file.</summary>
        public override Int64 GetFileSize(string path)
        {
            return base.GetFileSize(path);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public override bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash)
        {
            return base.GetFileSizeAndHash(path, out byteCount, out md5Hash);
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public override bool CreateDirectory(string path)
        {
            return base.CreateDirectory(path);
        }

        /// <summary>Deletes a directory.</summary>
        public override bool DeleteDirectory(string path)
        {
            return base.DeleteDirectory(path);
        }

        /// <summary>Moves a directory.</summary>
        public override bool MoveDirectory(string source, string destination)
        {
            return base.MoveDirectory(source, destination);
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public override IList<string> GetDirectories(string path)
        {
            return base.GetDirectories(path);
        }
    }
}

#endif // UNITY_EDITOR
