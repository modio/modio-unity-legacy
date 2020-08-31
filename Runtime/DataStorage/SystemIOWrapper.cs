using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality for use in platform modules.</summary>
    public static class SystemIOWrapper
    {
        /// <summary>Reads a file.</summary>
        public static bool ReadFile(string path, out byte[] data)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!File.Exists(path))
            {
                data = null;
                return false;
            }

            try
            {
                data = File.ReadAllBytes(path);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to read file.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                data = null;
                return false;
            }
        }

        /// <summary>Writes a file.</summary>
        public static bool WriteFile(string path, byte[] data)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(data != null);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, data);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write file.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public static bool DeleteFile(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                if(File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete file.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Moves a file.</summary>
        public static bool MoveFile(string source, string destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(destination));

            try
            {
                File.Move(source, destination);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("Failed to move file."
                                      + "\nSource File: " + source
                                      + "\nDestination: " + destination
                                      + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Gets the size of a file.</summary>
        public static bool GetFileExists(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            return File.Exists(path);
        }

        /// <summary>Gets the size of a file.</summary>
        public static Int64 GetFileSize(string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            if(!File.Exists(path)) { return -1; }

            try
            {
                var fileInfo = new FileInfo(path);

                return fileInfo.Length;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return -1;
            }
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            byteCount = -1;
            md5Hash = null;

            if(!File.Exists(path)) { return false; }

            // get byteCount
            try
            {
                byteCount = (new FileInfo(path)).Length;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                byteCount = -1;
                return false;
            }

            // get hash
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
                string warningInfo = ("[mod.io] Failed to calculate file hash.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                md5Hash = null;
                return false;
            }

            // success!
            return true;
        }

        /// <summary>Gets the files at a location.</summary>
        public static IList<string> GetFiles(string path, string nameFilter, bool recurseSubdirectories)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!Directory.Exists(path)) { return null; }

            var searchOption = (recurseSubdirectories
                                ? SearchOption.AllDirectories
                                : SearchOption.TopDirectoryOnly);

            if(nameFilter == null)
            {
                nameFilter = "*";
            }

            return Directory.GetFiles(path, nameFilter, searchOption);
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public static bool CreateDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                Directory.CreateDirectory(path);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create directory.\nDirectory: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return true;
            }
        }

        /// <summary>Deletes a directory.</summary>
        public static bool DeleteDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                if(Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete directory.\nDirectory: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Moves a directory.</summary>
        public static bool MoveDirectory(string source, string destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(destination));

            try
            {
                Directory.Move(source, destination);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to move directory."
                                      + "\nSource Directory: " + source
                                      + "\nDestination: " + destination
                                      + "\n\n" + Utility.GenerateExceptionDebugString(e));
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public static bool GetDirectoryExists(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            return Directory.Exists(path);
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public static IList<string> GetDirectories(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!Directory.Exists(path)) { return null; }

            try
            {
                return Directory.GetDirectories(path);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to get directories.\nDirectory: " + path + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                return null;
            }
        }
    }
}
