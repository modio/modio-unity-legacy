using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class LocalDataStorage
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

        /// <summary>Delegate for GetFile/DirectoryExists callback.</summary>
        public delegate void GetExistsCallback(string path, bool doesExist);

        /// <summary>Delegate for GetFileSize callback.</summary>
        public delegate void GetFileSizeCallback(string path, Int64 byteCount);

        /// <summary>Delegate for GetFileSizeAndHash callback.</summary>
        public delegate void GetFileSizeAndHashCallback(string path, Int64 byteCount, string md5Hash);

        /// <summary>Delegate for GetDirectories callback.</summary>
        public delegate void GetDirectoriesCallback(string directoryPath, IList<string> directories);

        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly IPlatformIO PLATFORM_IO;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static LocalDataStorage()
        {
            #if true
                LocalDataStorage.PLATFORM_IO = new StandaloneIO();
            #endif
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Reads a file.</summary>
        public static void ReadFile(string filePath, ReadFileCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.ReadFile(filePath, callback);
        }

        /// <summary>Reads a JSON file and parses the data as a new object instance.</summary>
        public static void ReadJSONFile<T>(string filePath, ReadJSONFileCallback<T> callback)
        {
            LocalDataStorage.PLATFORM_IO.ReadFile(filePath, (path, success, data) =>
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

            LocalDataStorage.PLATFORM_IO.WriteFile(filePath, data, callback);
        }

        /// <summary>Writes a JSON file.</summary>
        public static void WriteJSONFile<T>(string filePath, T jsonObject, WriteFileCallback callback)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                LocalDataStorage.WriteFile(filePath, data, callback);
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
            LocalDataStorage.PLATFORM_IO.DeleteFile(filePath, callback);
        }

        /// <summary>Moves a file.</summary>
        public static void MoveFile(string sourceFilePath, string destinationFilePath, MoveCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.MoveFile(sourceFilePath, destinationFilePath, callback);
        }

        /// <summary>Creates a directory.</summary>
        public static void CreateDirectory(string directoryPath, CreateCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.CreateDirectory(directoryPath, callback);
        }

        /// <summary>Deletes a directory.</summary>
        public static void DeleteDirectory(string directoryPath, DeleteCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.DeleteDirectory(directoryPath, callback);
        }

        /// <summary>Moves a directory.</summary>
        public static void MoveDirectory(string sourcePath, string destinationPath, LocalDataStorage.MoveCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.MoveDirectory(sourcePath, destinationPath, callback);
        }

        /// <summary>Gets the size of a file.</summary>
        public static void GetFileSize(string filePath, GetFileSizeCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.GetFileSize(filePath, callback);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static void GetFileSizeAndHash(string filePath, GetFileSizeAndHashCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.GetFileSizeAndHash(filePath, callback);
        }

        /// <summary>Gets a list of directories found at the given location.</summary>
        public static void GetDirectories(string directoryPath, LocalDataStorage.GetDirectoriesCallback callback)
        {
            LocalDataStorage.PLATFORM_IO.GetDirectories(directoryPath, callback);
        }
    }
}
