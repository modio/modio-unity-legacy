using System;
using System.Collections.Generic;
using System.IO;

using ModIO.UserDataIOCallbacks;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class SystemIOWrapper : IPlatformIO, IPlatformUserDataIO
    {
        // ---------[ IPlatformIO Interface ]---------
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public virtual bool ReadFile(string path, out byte[] data)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            string errorMessage = null;

            if(File.Exists(path))
            {
                try
                {
                    data = File.ReadAllBytes(path);
                    return true;
                }
                catch(Exception e)
                {
                    errorMessage = Utility.GenerateExceptionDebugString(e);
                }
            }
            else // !File.Exists
            {
                errorMessage = "File does not exist.";
            }

            if(errorMessage != null)
            {
                Debug.LogWarning("[mod.io] Failed to read file."
                                 + "\nFile: " + path + "\n\n"
                                 + errorMessage);
            }

            data = null;
            return false;
        }

        /// <summary>Writes a file.</summary>
        public virtual bool WriteFile(string path, byte[] data)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(data != null);

            bool success = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, data);
                success = true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write file.\nFile: " + path + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public virtual bool DeleteFile(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            bool success = true;
            if(File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch(Exception e)
                {
                    success = false;

                    string warningInfo = ("[mod.io] Failed to delete file.\nFile: " + path + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }
            }

            return success;
        }

        /// <summary>Moves a file.</summary>
        public virtual bool MoveFile(string source, string destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(destination));

            bool success = true;
            string failMessage = null;

            if(!File.Exists(source))
            {
                failMessage = ("Failed to move file as the source file does not exist."
                               + "\nSource File: " + source
                               + "\nDestination: " + destination);
                success = false;
            }
            else
            {
                if(File.Exists(destination))
                {
                    try
                    {
                        File.Delete(destination);
                    }
                    catch(Exception e)
                    {
                        failMessage = ("Failed to move file as the existing file at the destination could not be deleted."
                                       + "\nSource File: " + source
                                       + "\nDestination: " + destination
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));

                        success = false;
                    }
                }

                if(success)
                {
                    try
                    {
                        File.Move(source, destination);
                    }
                    catch(Exception e)
                    {
                        success = false;

                        failMessage = ("Failed to move file."
                                       + "\nSource File: " + source
                                       + "\nDestination: " + destination
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));
                    }
                }
            }

            if(!success)
            {
                Debug.LogWarning("[mod.io] " + failMessage);
            }

            return success;
        }

        /// <summary>Gets the size of a file.</summary>
        public virtual bool GetFileExists(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            return File.Exists(path);
        }

        /// <summary>Gets the size of a file.</summary>
        public virtual Int64 GetFileSize(string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            Int64 byteCount = -1;

            if(File.Exists(path))
            {
                try
                {
                    byteCount = (new FileInfo(path)).Length;
                }
                catch(Exception e)
                {
                    byteCount = -1;

                    string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + path + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }
            }

            return byteCount;
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public virtual bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            byteCount = -1;
            md5Hash = null;

            bool success = true;

            if(File.Exists(path))
            {
                try
                {
                    byteCount = (new FileInfo(path)).Length;
                }
                catch(Exception e)
                {
                    byteCount = -1;
                    success = false;

                    string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + path + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }

                try
                {
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            var hash = md5.ComputeHash(stream);
                            md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
                catch(Exception e)
                {
                    md5Hash = null;
                    success = false;

                    string warningInfo = ("[mod.io] Failed to calculate file hash.\nFile: " + path + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }
            }
            else
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to get information for file. File does not exist.\nFile: "
                                      + path);

                Debug.LogWarning(warningInfo);
            }

            return success;
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public virtual bool CreateDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            bool success = false;

            try
            {
                Directory.CreateDirectory(path);
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to create directory."
                                      + "\nDirectory: " + path + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        /// <summary>Deletes a directory.</summary>
        public virtual bool DeleteDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            bool success = false;
            try
            {
                if(Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to delete directory.\nDirectory: " + path + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        /// <summary>Moves a directory.</summary>
        public virtual bool MoveDirectory(string source, string destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(destination));

            bool success = true;
            string failMessage = null;

            if(!Directory.Exists(source))
            {
                failMessage = ("Failed to move directory as the source directory does not exist."
                               + "\nSource Directory: " + source
                               + "\nDestination: " + destination);
                success = false;
            }
            else
            {
                if(Directory.Exists(destination))
                {
                    try
                    {
                        Directory.Delete(destination);
                    }
                    catch(Exception e)
                    {
                        failMessage = ("Failed to move directory as the existing directory at the destination could not be deleted."
                                       + "\nSource Directory: " + source
                                       + "\nDestination: " + destination
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));

                        success = false;
                    }
                }

                if(success)
                {
                    try
                    {
                        Directory.Move(source, destination);
                    }
                    catch(Exception e)
                    {
                        success = false;

                        failMessage = ("Failed to move directory."
                                       + "\nSource Directory: " + source
                                       + "\nDestination: " + destination
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));
                    }
                }
            }

            if(!success)
            {
                Debug.LogWarning("[mod.io] " + failMessage);
            }

            return success;
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public virtual IList<string> GetDirectories(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            string[] subDirs = null;

            if(Directory.Exists(path))
            {
                try
                {
                    subDirs = Directory.GetDirectories(path);
                }
                catch(Exception e)
                {
                    subDirs = null;

                    string warningInfo = ("[mod.io] Failed to get directories.\nDirectory: " + path + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            return subDirs;
        }

        // ---------[ IPlatformUserDataIO Interface ]---------
        /// <summary>Root directory for the user-specific data.</summary>
        public static readonly string USER_DIR_ROOT = IOUtilities.CombinePath(UnityEngine.Application.persistentDataPath,
                                                                              "modio-" + PluginSettings.data.gameId,
                                                                              "users");

        /// <summary>The directory for the active user's data.</summary>
        public string userDir = SystemIOWrapper.USER_DIR_ROOT;

        // --- Initialization ---
        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback)
        {
            this.userDir = this.GenerateActiveUserDirectory(platformUserId);

            bool success = this.CreateDirectory(this.userDir);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback)
        {
            this.userDir = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

            bool success = this.CreateDirectory(this.userDir);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Determines the user directory for a given user id..</summary>
        protected virtual string GenerateActiveUserDirectory(string platformUserId)
        {
            string userDir = SystemIOWrapper.USER_DIR_ROOT;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                userDir = IOUtilities.CombinePath(SystemIOWrapper.USER_DIR_ROOT, folderName);
            }

            return userDir;
        }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public void ReadFile(string relativePath, ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            byte[] data;
            bool success = this.ReadFile(path, out data);

            callback.Invoke(relativePath, success, data);
        }

        /// <summary>Writes a file.</summary>
        public void WriteFile(string relativePath, byte[] data, WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(data != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            bool success = this.WriteFile(path, data);

            if(callback != null) { callback.Invoke(relativePath, success); }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public void DeleteFile(string relativePath, DeleteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            bool success = this.DeleteFile(path);

            if(callback != null) { callback.Invoke(relativePath, success); }
        }

        /// <summary>Checks for the existence of a file.</summary>
        public void GetFileExists(string relativePath, GetFileExistsCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            bool doesExist = this.GetFileExists(path);

            callback.Invoke(relativePath, doesExist);
        }

        /// <summary>Gets the size of a file.</summary>
        public void GetFileSize(string relativePath, GetFileSizeCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            Int64 byteCount = this.GetFileSize(path);

            callback.Invoke(relativePath, byteCount);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public void GetFileSizeAndHash(string relativePath, GetFileSizeAndHashCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            Int64 byteCount;
            string md5Hash;
            bool success = this.GetFileSizeAndHash(path, out byteCount, out md5Hash);

            callback.Invoke(relativePath, success, byteCount, md5Hash);
        }

        /// <summary>Deletes all of the active user's data.</summary>
        public virtual void ClearActiveUserData(ClearActiveUserDataCallback callback)
        {
            bool success = this.DeleteDirectory(this.userDir);

            if(callback != null)
            {
                callback.Invoke(success);
            }
        }
    }
}
