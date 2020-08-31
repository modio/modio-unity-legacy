using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality for use in platform modules.</summary>
    public abstract class PlatformIOBase : IPlatformIO
    {
        // ---------[ IPlatformIO Interface ]---------
        // --- Accessors ---
        /// <summary>Temporary Data directory path.</summary>
        public abstract string TemporaryDataDirectory { get; }

        /// <summary>Persistent Data directory path.</summary>
        public abstract string PersistentDataDirectory { get; }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public virtual void ReadFile(string path,
                                     PlatformIOCallbacks.ReadFileCallback callback)
        {
            byte[] data = null;
            bool success = SystemIOWrapper.ReadFile(path, out data);

            if(callback != null)
            {
                callback.Invoke(path, success, data);
            }
        }

        /// <summary>Writes a file.</summary>
        public virtual void WriteFile(string path, byte[] data,
                                      PlatformIOCallbacks.WriteFileCallback callback)
        {
            bool success = SystemIOWrapper.WriteFile(path, data);

            if(callback != null)
            {
                callback.Invoke(path, success);
            }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public virtual void DeleteFile(string path,
                                       PlatformIOCallbacks.DeleteFileCallback callback)
        {
            bool success = SystemIOWrapper.DeleteFile(path);

            if(callback != null)
            {
                callback.Invoke(path, success);
            }
        }

        /// <summary>Moves a file.</summary>
        public virtual void MoveFile(string source, string destination,
                                     PlatformIOCallbacks.MoveFileCallback callback)
        {
            bool success = SystemIOWrapper.MoveFile(source, destination);

            if(callback != null)
            {
                callback.Invoke(source, destination, success);
            }
        }

        /// <summary>Checks for the existence of a file.</summary>
        public virtual void GetFileExists(string path,
                                          PlatformIOCallbacks.GetFileExistsCallback callback)
        {
            bool exists = SystemIOWrapper.GetFileExists(path);

            if(callback != null)
            {
                callback.Invoke(path, exists);
            }
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public virtual void GetFileSizeAndHash(string path,
                                               PlatformIOCallbacks.GetFileSizeAndHashCallback callback)
        {
            Int64 byteCount;
            string md5Hash;

            bool success = SystemIOWrapper.GetFileSizeAndHash(path, out byteCount, out md5Hash);

            if(callback != null)
            {
                callback.Invoke(path, success, byteCount, md5Hash);
            }
        }

        /// <summary>Gets the files at a location.</summary>
        public virtual void GetFiles(string path, string nameFilter, bool recurseSubdirectories,
                                     PlatformIOCallbacks.GetFilesCallback callback)
        {
            IList<string> files = SystemIOWrapper.GetFiles(path, nameFilter, recurseSubdirectories);

            if(callback != null)
            {
                callback.Invoke(path, files != null, files);
            }
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public virtual void CreateDirectory(string path,
                                            PlatformIOCallbacks.CreateDirectoryCallback callback)
        {
            bool success = SystemIOWrapper.CreateDirectory(path);

            if(callback != null)
            {
                callback.Invoke(path, success);
            }
        }

        /// <summary>Deletes a directory.</summary>
        public virtual void DeleteDirectory(string path,
                                            PlatformIOCallbacks.DeleteDirectoryCallback callback)
        {
            bool success = SystemIOWrapper.DeleteDirectory(path);

            if(callback != null)
            {
                callback.Invoke(path, success);
            }
        }

        /// <summary>Moves a directory.</summary>
        public virtual void MoveDirectory(string source, string destination,
                                          PlatformIOCallbacks.MoveDirectoryCallback callback)
        {
            bool success = SystemIOWrapper.MoveDirectory(source, destination);

            if(callback != null)
            {
                callback.Invoke(source, destination, success);
            }
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public virtual void GetDirectoryExists(string path,
                                               PlatformIOCallbacks.GetDirectoryExistsCallback callback)
        {
            bool exists = SystemIOWrapper.GetDirectoryExists(path);

            if(callback != null)
            {
                callback.Invoke(path, exists);
            }
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public virtual void GetDirectories(string path,
                                           PlatformIOCallbacks.GetDirectoriesCallback callback)
        {
            IList<string> dirs = SystemIOWrapper.GetDirectories(path);

            if(callback != null)
            {
                callback.Invoke(path, dirs != null, dirs);
            }
        }
    }
}
