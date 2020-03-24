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
        /// <summary>Delegate for the read file callback.</summary>
        public delegate void ReadFileCallback(bool success, byte[] data, string filePath);

        /// <summary>Delegate for the read JSON file callback.</summary>
        public delegate void ReadJSONFileCallback<T>(bool success, T jsonObject, string filePath);

        /// <summary>Delegate for write/delete file callbacks.</summary>
        public delegate void WriteFileCallback(bool success, string filePath);

        /// <summary>The collection of platform specific functions.</summary>
        public struct PlatformFunctions
        {
            // --- Delegates ---
            /// <summary>Delegate for reading a file.</summary>
            public delegate void ReadFileDelegate(string filePath, ReadFileCallback callback);

            /// <summary>Delegate for writing a file.</summary>
            public delegate void WriteFileDelegate(string filePath, byte[] data, WriteFileCallback callback);

            /// <summary>Delegate for deleting a file.</summary>
            public delegate void DeleteFileDelegate(string filePath, WriteFileCallback callback);

            /// <summary>Delegate for clearing all data.</summary>
            public delegate void ClearAllDataDelegate(WriteFileCallback callback);

            // --- Fields ---
            /// <summary>Delegate for reading a file.</summary>
            public ReadFileDelegate ReadFile;

            /// <summary>Delegate for writing a file.</summary>
            public WriteFileDelegate WriteFile;

            /// <summary>Delegate for deleting a file.</summary>
            public DeleteFileDelegate DeleteFile;

            /// <summary>Delegate for clearing all data.</summary>
            public ClearAllDataDelegate ClearAllData;
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

        // ---------[ Platform I/O ]---------
        #if true // --- Standalone I/O ---

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            public static PlatformFunctions GetPlatformFunctions_Standalone()
            {
                return new PlatformFunctions()
                {
                    ReadFile = ReadFile_Standalone,
                    WriteFile = WriteFile_Standalone,
                    // DeleteFile = DeleteFile_Standalone,
                    // ClearAllData = ClearAllData_Standalone,
                };
            }

            /// <summary>Reads a data file. (Standalone Application)</summary>
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

            /// <summary>Writes a data file. (Standalone Application)</summary>
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
        #endif
    }
}
