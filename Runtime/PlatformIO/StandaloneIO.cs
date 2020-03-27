using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Defines the standalone i/o functionality.</summary>
    public class StandaloneIO : DataStorage.IPlatformIO
    {
        /// <summary>Reads a file.</summary>
        public void ReadFile(string filePath, DataStorage.ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            Debug.Assert(callback != null);

            bool success = false;
            byte[] data = null;

            if(File.Exists(filePath))
            {
                try
                {
                    data = File.ReadAllBytes(filePath);
                    success = true;
                }
                catch(Exception e)
                {
                    data = null;

                    string warningInfo = ("[mod.io] Failed to read file.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            callback.Invoke(filePath, success, data);
        }

        /// <summary>Writes a file.</summary>
        public void WriteFile(string filePath, byte[] data, DataStorage.WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            Debug.Assert(data != null);

            bool success = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);
                success = true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write file.\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            if(callback != null)
            {
                callback.Invoke(filePath, success);
            }
        }

        /// <summary>Deletes a file.</summary>
        public void DeleteFile(string filePath, DataStorage.DeleteCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            bool success = false;
            try
            {
                if(File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to delete file.\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            if(callback != null)
            {
                callback.Invoke(filePath, success);
            }
        }

        /// <summary>Moves a file.</summary>
        public void MoveFile(string sourceFilePath, string destinationFilePath, DataStorage.MoveCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceFilePath));
            Debug.Assert(!string.IsNullOrEmpty(destinationFilePath));

            bool success = false;
            try
            {
                File.Move(sourceFilePath, destinationFilePath);
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to move file."
                                      + "\nSource File: " + sourceFilePath
                                      + "\nDestination: " + destinationFilePath
                                      + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            if(callback != null)
            {
                callback.Invoke(sourceFilePath, destinationFilePath, success);
            }
        }

        /// <summary>Creates a directory.</summary>
        public void CreateDirectory(string directoryPath, DataStorage.CreateCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(directoryPath));

            bool success = false;

            try
            {
                Directory.CreateDirectory(directoryPath);
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to create directory."
                                      + "\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            if(callback != null)
            {
                callback.Invoke(directoryPath, success);
            }
        }

        /// <summary>Deletes a directory.</summary>
        public void DeleteDirectory(string directoryPath, DataStorage.DeleteCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(directoryPath));

            bool success = false;
            try
            {
                if(Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to delete directory.\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            if(callback != null)
            {
                callback.Invoke(directoryPath, success);
            }
        }

        /// <summary>Checks whether a file exists.</summary>
        public void GetFileExists(string filePath, DataStorage.GetExistsCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            Debug.Assert(callback != null);

            callback.Invoke(filePath, File.Exists(filePath));
        }

        /// <summary>Gets the size of a file.</summary>
        public void GetFileSize(string filePath, DataStorage.GetFileSizeCallback callback)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));
            Debug.Assert(callback != null);

            Int64 byteCount = -1;

            if(File.Exists(filePath))
            {
                try
                {
                    byteCount = (new FileInfo(filePath)).Length;
                }
                catch(Exception e)
                {
                    byteCount = -1;

                    string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }
            }

            callback.Invoke(filePath, byteCount);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public void GetFileSizeAndHash(string filePath, DataStorage.GetFileSizeAndHashCallback callback)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));
            Debug.Assert(callback != null);

            Int64 byteCount = -1;
            string hashString = null;

            if(File.Exists(filePath))
            {
                try
                {
                    byteCount = (new FileInfo(filePath)).Length;
                }
                catch(Exception e)
                {
                    byteCount = -1;

                    string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }

                try
                {
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            var hash = md5.ComputeHash(stream);
                            hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
                catch(Exception e)
                {
                    hashString = null;

                    string warningInfo = ("[mod.io] Failed to calculate file hash.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }
            }

            callback.Invoke(filePath, byteCount, hashString);
        }

        /// <summary>Gets a list of directories contained in the given location.</summary>
        public void GetDirectories(string directoryPath, Action<string, IList<string>> callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(directoryPath));
            Debug.Assert(callback != null);

            string[] subDirs = null;

            if(Directory.Exists(directoryPath))
            {
                try
                {
                    subDirs = Directory.GetDirectories(directoryPath);
                }
                catch(Exception e)
                {
                    subDirs = null;

                    string warningInfo = ("[mod.io] Failed to get directories.\nDirectory: " + directoryPath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            callback.Invoke(directoryPath, subDirs);
        }
    }
}
