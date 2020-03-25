using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class DataStorage
    {
        // ---------[ Nested Data-Types ]---------
        // --- Callbacks ---
        /// <summary>Delegate for ReadFile callback.</summary>
        public delegate void ReadFileCallback(string path, bool success, byte[] data);

        /// <summary>Delegate for ReadJSONFile callback.</summary>
        public delegate void ReadJSONFileCallback<T>(string path, bool success, T jsonObject);

        /// <summary>Delegate for WriteFile callbacks.</summary>
        public delegate void WriteFileCallback(string path, bool success);

        /// <summary>Delegate for DeleteFile/Directory callbacks.</summary>
        public delegate void DeleteCallback(string path, bool success);

        /// <summary>Delegate for GetFileSize callback.</summary>
        public delegate void GetFileSizeCallback(string path, Int64 byteCount);

        /// <summary>Delegate for GetFileSizeAndHash callback.</summary>
        public delegate void GetFileSizeAndHashCallback(string path, Int64 byteCount, string md5Hash);

        // --- I/O Functions ---
        /// <summary>Delegate for reading a file.</summary>
        public delegate void ReadFileDelegate(string filePath, ReadFileCallback callback);

        /// <summary>Delegate for writing a file.</summary>
        public delegate void WriteFileDelegate(string filePath, byte[] data, WriteFileCallback callback);

        /// <summary>Delegate for deleting a file.</summary>
        public delegate void DeleteFileDelegate(string filePath, DeleteCallback callback);

        /// <summary>Delegate for deleting a file.</summary>
        public delegate void DeleteDirectoryDelegate(string directoryPath, DeleteCallback callback);

        /// <summary>Delegate for getting a file's size.</summary>
        public delegate void GetFileSizeDelegate(string filePath, GetFileSizeCallback callback);

        /// <summary>Delegate for getting a file's size and md5 hash.</summary>
        public delegate void GetFileSizeAndHashDelegate(string filePath, GetFileSizeAndHashCallback callback);

        // --- Platform Functions ---
        /// <summary>The collection of platform specific functions.</summary>
        public struct PlatformFunctions
        {
            // --- Fields ---
            /// <summary>Delegate for reading a file.</summary>
            public ReadFileDelegate ReadFile;

            /// <summary>Delegate for writing a file.</summary>
            public WriteFileDelegate WriteFile;

            /// <summary>Delegate for deleting a file.</summary>
            public DeleteFileDelegate DeleteFile;

            /// <summary>Delegate for deleting a directory.</summary>
            public DeleteDirectoryDelegate DeleteDirectory;

            /// <summary>Delegate for getting a file's size.</summary>
            public GetFileSizeDelegate GetFileSize;

            /// <summary>Delegate for getting a file's size and md5 hash.</summary>
            public GetFileSizeAndHashDelegate GetFileSizeAndHash;
        }

        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly PlatformFunctions PLATFORM;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static DataStorage()
        {
            #if true
                DataStorage.PLATFORM = DataStorage.GetPlatformFunctions_Standalone();
            #endif

            Debug.Assert(DataStorage.PLATFORM.ReadFile != null);
            Debug.Assert(DataStorage.PLATFORM.WriteFile != null);
            Debug.Assert(DataStorage.PLATFORM.DeleteFile != null);
            Debug.Assert(DataStorage.PLATFORM.GetFileSize != null);
            Debug.Assert(DataStorage.PLATFORM.GetFileSizeAndHash != null);
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Reads a file.</summary>
        public static void ReadFile(string filePath, ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            DataStorage.PLATFORM.ReadFile(filePath, callback);
        }

        /// <summary>Reads a JSON file and parses the data as a new object instance.</summary>
        public static void ReadJSONFile<T>(string filePath, ReadJSONFileCallback<T> callback)
        {
            Debug.Assert(callback != null);

            DataStorage.PLATFORM.ReadFile(filePath, (path, success, data) =>
            {
                T jsonObject;

                if(success)
                {
                    success = IOUtilities.TryParseUTF8JSONData<T>(data, out jsonObject);
                }
                else
                {
                    jsonObject = default(T);
                }

                callback.Invoke(path, success, jsonObject);
            });
        }

        /// <summary>Writes a file.</summary>
        public static void WriteFile(string filePath, byte[] data, WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            Debug.Assert(data != null);

            #if DEBUG
            if(data.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + filePath);
            }
            #endif // DEBUG

            DataStorage.PLATFORM.WriteFile(filePath, data, callback);
        }

        /// <summary>Writes a JSON file.</summary>
        public static void WriteJSONFile<T>(string filePath, T jsonObject, WriteFileCallback callback)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                DataStorage.WriteFile(filePath, data, callback);
            }
            else if(callback != null)
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + filePath + "\n\n");

                callback.Invoke(filePath, false);
            }
        }

        /// <summary>Deletes a file.</summary>
        public static void DeleteFile(string filePath, DeleteCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            DataStorage.PLATFORM.DeleteFile(filePath, callback);
        }

        /// <summary>Deletes a directory.</summary>
        public static void DeleteDirectory(string directoryPath, DeleteCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(directoryPath));

            DataStorage.PLATFORM.DeleteDirectory(directoryPath, callback);
        }

        /// <summary>Gets the size of a file.</summary>
        public static void GetFileSize(string filePath, GetFileSizeCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            Debug.Assert(callback != null);

            DataStorage.PLATFORM.GetFileSize(filePath, callback);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static void GetFileSizeAndHash(string filePath, GetFileSizeAndHashCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            DataStorage.PLATFORM.GetFileSizeAndHash(filePath, callback);
        }

        // ---------[ Platform I/O ]---------
        #if true // --- Standalone I/O ---

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            public static PlatformFunctions GetPlatformFunctions_Standalone()
            {
                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_Standalone,
                    WriteFile = WriteFile_Standalone,
                    DeleteFile = DeleteFile_Standalone,
                    DeleteDirectory = DeleteDirectory_Standalone,
                    GetFileSize = GetFileSize_Standalone,
                    GetFileSizeAndHash = GetFileSizeAndHash_Standalone,
                };
            }

            /// <summary>Reads a file. (Standalone Application)</summary>
            public static void ReadFile_Standalone(string filePath, ReadFileCallback callback)
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

            /// <summary>Writes a file. (Standalone Application)</summary>
            public static void WriteFile_Standalone(string filePath, byte[] data, WriteFileCallback callback)
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

            /// <summary>Deletes a file. (Standalone Application)</summary>
            public static void DeleteFile_Standalone(string filePath, DeleteCallback callback)
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

            /// <summary>Deletes a directory. (Standalone Application)</summary>
            public static void DeleteDirectory_Standalone(string directoryPath, DeleteCallback callback)
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

            /// <summary>Gets the size of a file.</summary>
            public static void GetFileSize_Standalone(string filePath, GetFileSizeCallback callback)
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
            public static void GetFileSizeAndHash_Standalone(string filePath, GetFileSizeAndHashCallback callback)
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

        #endif
    }
}
