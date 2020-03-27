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

        /// <summary>Delegate for CreateFile/Directory callbacks.</summary>
        public delegate void CreateCallback(string path, bool success);

        /// <summary>Delegate for DeleteFile/Directory callbacks.</summary>
        public delegate void DeleteCallback(string path, bool success);

        /// <summary>Delegate for MoveFile/Directory callbacks.</summary>
        public delegate void MoveCallback(string sourcePath, string destinationPath, bool success);

        /// <summary>Delegate for GetFileExists callback.</summary>
        public delegate void GetFileExistsCallback(string path, bool doesFileExist);

        /// <summary>Delegate for GetFileSize callback.</summary>
        public delegate void GetFileSizeCallback(string path, Int64 byteCount);

        /// <summary>Delegate for GetFileSizeAndHash callback.</summary>
        public delegate void GetFileSizeAndHashCallback(string path, Int64 byteCount, string md5Hash);

        // ---------[ I/O Functionality ]---------
        /// <summary>Defines the functions needed for a complete platform IO.</summary>
        public interface IPlatformIO
        {
            /// <summary>Delegate for reading a file.</summary>
            void ReadFile(string filePath, ReadFileCallback callback);

            /// <summary>Delegate for writing a file.</summary>
            void WriteFile(string filePath, byte[] data, WriteFileCallback callback);

            /// <summary>Delegate for deleting a file.</summary>
            void DeleteFile(string filePath, DeleteCallback callback);

            /// <summary>Delegate for moving a file.</summary>
            void MoveFile(string sourceFilePath, string destinationFilePath, MoveCallback callback);

            /// <summary>Delegate for creating a directory.</summary>
            void CreateDirectory(string directoryPath, CreateCallback callback);

            /// <summary>Delegate for deleting a directory.</summary>
            void DeleteDirectory(string directoryPath, DeleteCallback callback);

            /// <summary>Gets the size of a file.</summary>
            void GetFileExists(string filePath, GetFileExistsCallback callback);

            /// <summary>Delegate for getting a file's size.</summary>
            void GetFileSize(string filePath, GetFileSizeCallback callback);

            /// <summary>Delegate for getting a file's size and md5 hash.</summary>
            void GetFileSizeAndHash(string filePath, GetFileSizeAndHashCallback callback);
        }

        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly IPlatformIO PLATFORM_IO;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static DataStorage()
        {
            #if true
                DataStorage.PLATFORM_IO = new StandaloneIO();
            #endif
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Reads a file.</summary>
        public static void ReadFile(string filePath, ReadFileCallback callback)
        {
            DataStorage.PLATFORM_IO.ReadFile(filePath, callback);
        }

        /// <summary>Reads a JSON file and parses the data as a new object instance.</summary>
        public static void ReadJSONFile<T>(string filePath, ReadJSONFileCallback<T> callback)
        {
            DataStorage.PLATFORM_IO.ReadFile(filePath, (path, success, data) =>
            {
                T jsonObject;

                if(success)
                {
                    if(!IOUtilities.TryParseUTF8JSONData<T>(data, out jsonObject))
                    {
                        success = false;

                        Debug.LogWarning("[mod.io] Failed translate file data into JSON Object."
                                         + "\nFile: " + filePath + "\n\n");
                    }
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
            #if DEBUG
            if(data.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + filePath);
            }
            #endif // DEBUG

            DataStorage.PLATFORM_IO.WriteFile(filePath, data, callback);
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
            DataStorage.PLATFORM_IO.DeleteFile(filePath, callback);
        }

        /// <summary>Moves a file.</summary>
        public static void MoveFile(string sourceFilePath, string destinationFilePath, MoveCallback callback)
        {
            DataStorage.PLATFORM_IO.MoveFile(sourceFilePath, destinationFilePath, callback);
        }

        /// <summary>Creates a directory.</summary>
        public static void CreateDirectory(string directoryPath, CreateCallback callback)
        {
            DataStorage.PLATFORM_IO.CreateDirectory(directoryPath, callback);
        }

        /// <summary>Deletes a directory.</summary>
        public static void DeleteDirectory(string directoryPath, DeleteCallback callback)
        {
            DataStorage.PLATFORM_IO.DeleteDirectory(directoryPath, callback);
        }

        /// <summary>Gets the size of a file.</summary>
        public static void GetFileSize(string filePath, GetFileSizeCallback callback)
        {
            DataStorage.PLATFORM_IO.GetFileSize(filePath, callback);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static void GetFileSizeAndHash(string filePath, GetFileSizeAndHashCallback callback)
        {
            DataStorage.PLATFORM_IO.GetFileSizeAndHash(filePath, callback);
        }

    }
}
