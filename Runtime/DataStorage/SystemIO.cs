using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class SystemIO : IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public byte[] ReadFile(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            byte[] data = null;

            if(File.Exists(filePath))
            {
                try
                {
                    data = File.ReadAllBytes(filePath);
                }
                catch(Exception e)
                {
                    data = null;

                    string warningInfo = ("[mod.io] Failed to read file.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            return data;
        }

        /// <summary>Writes a file.</summary>
        public bool WriteFile(string filePath, byte[] data)
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

            return success;
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public bool DeleteFile(string filePath)
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

            return success;
        }

        /// <summary>Moves a file.</summary>
        public bool MoveFile(string sourceFilePath, string destinationFilePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceFilePath));
            Debug.Assert(!string.IsNullOrEmpty(destinationFilePath));

            bool success = true;
            string failMessage = null;

            if(!File.Exists(sourceFilePath))
            {
                failMessage = ("Failed to move file as the source file does not exist."
                               + "\nSource File: " + sourceFilePath
                               + "\nDestination: " + destinationFilePath);
                success = false;
            }
            else
            {
                if(File.Exists(destinationFilePath))
                {
                    try
                    {
                        File.Delete(destinationFilePath);
                    }
                    catch(Exception e)
                    {
                        failMessage = ("Failed to move file as the existing file at the destination could not be deleted."
                                       + "\nSource File: " + sourceFilePath
                                       + "\nDestination: " + destinationFilePath
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));

                        success = false;
                    }
                }

                if(success)
                {
                    try
                    {
                        File.Move(sourceFilePath, destinationFilePath);
                    }
                    catch(Exception e)
                    {
                        success = false;

                        failMessage = ("Failed to move file."
                                       + "\nSource File: " + sourceFilePath
                                       + "\nDestination: " + destinationFilePath
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
        public bool GetFileExists(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            return File.Exists(filePath);
        }

        /// <summary>Gets the size of a file.</summary>
        public Int64 GetFileSize(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

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

            return byteCount;
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public bool GetFileSizeAndHash(string filePath, out Int64 byteCount, out string md5Hash)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            byteCount = -1;
            md5Hash = null;

            bool success = true;

            if(File.Exists(filePath))
            {
                try
                {
                    byteCount = (new FileInfo(filePath)).Length;
                }
                catch(Exception e)
                {
                    byteCount = -1;
                    success = false;

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
                            md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
                catch(Exception e)
                {
                    md5Hash = null;
                    success = false;

                    string warningInfo = ("[mod.io] Failed to calculate file hash.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
                }
            }
            else
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to get information for file. File does not exist.\nFile: "
                                      + filePath);

                Debug.LogWarning(warningInfo);
            }

            return success;
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public bool CreateDirectory(string directoryPath)
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

            return success;
        }

        /// <summary>Deletes a directory.</summary>
        public bool DeleteDirectory(string directoryPath)
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

            return success;
        }

        /// <summary>Moves a directory.</summary>
        public bool MoveDirectory(string sourcePath, string destinationPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourcePath));
            Debug.Assert(!string.IsNullOrEmpty(destinationPath));

            bool success = false;
            try
            {
                Directory.Move(sourcePath, destinationPath);
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to move directory."
                                      + "\nSource Directory: " + sourcePath
                                      + "\nDestination: " + destinationPath
                                      + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public IList<string> GetDirectories(string directoryPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(directoryPath));

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

            return subDirs;
        }
    }
}
