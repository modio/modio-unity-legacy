using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Provides a base implementation of IUserDataIO for platforms that use System.IO.</summary>
    public abstract class UserDataIOBase : IUserDataIO
    {
        // ---------[ IUserDataIO Interface ]---------
        /// <summary>Active User Data directory.</summary>
        public abstract string ActiveUserDirectory { get; }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public virtual void ReadFile(string relativePath, UserDataIOCallbacks.ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.ActiveUserDirectory, relativePath);
            byte[] data;
            bool success = SystemIOWrapper.ReadFile(path, out data);

            if(callback != null)
            {
                callback.Invoke(relativePath, success, data);
            }
        }

        /// <summary>Writes a file.</summary>
        public virtual void WriteFile(string relativePath, byte[] data, UserDataIOCallbacks.WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(data != null);

            string path = IOUtilities.CombinePath(this.ActiveUserDirectory, relativePath);
            bool success = SystemIOWrapper.WriteFile(path, data);

            if(callback != null) { callback.Invoke(relativePath, success); }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public virtual void DeleteFile(string relativePath, UserDataIOCallbacks.DeleteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(this.ActiveUserDirectory, relativePath);
            bool success = SystemIOWrapper.DeleteFile(path);

            if(callback != null) { callback.Invoke(relativePath, success); }
        }

        /// <summary>Deletes all of the active user's data.</summary>
        public virtual void ClearActiveUserData(UserDataIOCallbacks.ClearActiveUserDataCallback callback)
        {
            bool success = SystemIOWrapper.DeleteDirectory(this.ActiveUserDirectory);

            if(callback != null) { callback.Invoke(success); }
        }
    }
}
