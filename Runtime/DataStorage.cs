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
        public delegate void ReadFileCallback(bool success, byte[] data, string filePath);

        /// <summary>Delegate for ReadJSONFile callback.</summary>
        public delegate void ReadJSONFileCallback<T>(bool success, T jsonObject, string filePath);

        /// <summary>Delegate for WriteFile callbacks.</summary>
        public delegate void WriteFileCallback(bool success, string filePath);

        /// <summary>Delegate for DeleteFile/Directory callbacks.</summary>
        public delegate void DeleteCallback(bool success, string path);

        /// <summary>Delegate for GetFileSize callback.</summary>
        public delegate void GetFileSizeCallback(Int64 byteCount, string filePath);

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
            DataStorage.PLATFORM.ReadFile(filePath,
                                          (s,d,p) => DataStorage.ParseJSONFile<T>(s,d,p,callback));
        }

        /// <summary>Completes the ReadJSONFile call.</summary>
        private static void ParseJSONFile<T>(bool success, byte[] data, string filePath,
                                             ReadJSONFileCallback<T> callback)
        {
            T jsonObject = default(T);

            if(success)
            {
                try
                {
                    string dataString = Encoding.UTF8.GetString(data);
                    jsonObject = JsonConvert.DeserializeObject<T>(dataString);
                }
                catch(Exception e)
                {
                    jsonObject = default(T);
                    success = false;

                    string warningInfo = ("[mod.io] Failed to parse data as JSON Object after reading file."
                                          + "\nFile: " + filePath
                                          + " [" + ValueFormatting.ByteCount(data.Length, string.Empty)
                                          + "]\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            callback.Invoke(success, jsonObject, filePath);
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
            Debug.Assert(jsonObject != null);

            byte[] data = null;

            try
            {
                string dataString = JsonConvert.SerializeObject(jsonObject);
                data = Encoding.UTF8.GetBytes(dataString);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed create JSON representation of object before writing file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                data = null;
            }

            if(data != null)
            {
                DataStorage.WriteFile(filePath, data, callback);
            }
            else if(callback != null)
            {
                callback.Invoke(false, filePath);
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

                callback.Invoke(success, data, filePath);
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
                    callback.Invoke(success, filePath);
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
                    callback.Invoke(success, filePath);
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
                    callback.Invoke(success, directoryPath);
                }
            }

            /// <summary>Gets the size of a file.</summary>
            public static void GetFileSize_Standalone(string filePath, GetFileSizeCallback callback)
            {
                Debug.Assert(!String.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                Debug.Assert(File.Exists(filePath));

                Int64 byteCount = -1;

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

                callback.Invoke(byteCount, filePath);
            }

        #endif
    }
}
