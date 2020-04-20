using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;
using ModIO.DataStorageCallbacks;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class LocalDataStorage
    {
        // ---------[ Constants ]---------
        /// <summary>Defines the I/O functions to use for this platform.</summary>
        public static readonly IPlatformIO PLATFORM_IO;

        /// <summary>Defines the async I/O functions to use for this platform.</summary>
        public static readonly IPlatformIOAsync PLATFORM_IO_ASYNC;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static LocalDataStorage()
        {
            #if true
                LocalDataStorage.PLATFORM_IO = new SystemIO();
                LocalDataStorage.PLATFORM_IO_ASYNC = new StandaloneIO();
            #endif
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Reads a file.</summary>
        public static void ReadFileAsync(string path, ReadFileCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.ReadFile(path, callback);
        }

        /// <summary>Reads a JSON file and parses the data as a new object instance.</summary>
        public static void ReadJSONFile<T>(string path, ReadJSONFileCallback<T> callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.ReadFile(path, (p, success, data) =>
            {
                T jsonObject;

                if(success)
                {
                    if(!IOUtilities.TryParseUTF8JSONData<T>(data, out jsonObject))
                    {
                        success = false;

                        Debug.LogWarning("[mod.io] Failed translate file data into JSON Object."
                                         + "\nFile: " + path + "\n\n");
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
        public static void WriteFile(string path, byte[] data, WriteFileCallback callback)
        {
            #if DEBUG
            if(data.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + path);
            }
            #endif // DEBUG

            LocalDataStorage.PLATFORM_IO_ASYNC.WriteFile(path, data, callback);
        }

        /// <summary>Writes a JSON file.</summary>
        public static void WriteJSONFile<T>(string path, T jsonObject, WriteFileCallback callback)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                LocalDataStorage.WriteFile(path, data, callback);
            }
            else if(callback != null)
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + path + "\n\n");

                callback.Invoke(path, false);
            }
        }

        /// <summary>Deletes a file.</summary>
        public static void DeleteFile(string path, DeleteFileCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.DeleteFile(path, callback);
        }

        /// <summary>Moves a file.</summary>
        public static void MoveFile(string source, string destination, MoveFileCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.MoveFile(source, destination, callback);
        }

        /// <summary>Creates a directory.</summary>
        public static void CreateDirectory(string path, CreateDirectoryCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.CreateDirectory(path, callback);
        }

        /// <summary>Deletes a directory.</summary>
        public static void DeleteDirectory(string path, DeleteDirectoryCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.DeleteDirectory(path, callback);
        }

        /// <summary>Moves a directory.</summary>
        public static void MoveDirectory(string source, string destination, MoveDirectoryCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.MoveDirectory(source, destination, callback);
        }

        /// <summary>Gets the size of a file.</summary>
        public static void GetFileSize(string path, GetFileSizeCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.GetFileSize(path, callback);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.GetFileSizeAndHash(path, callback);
        }

        /// <summary>Gets a list of directories found at the given location.</summary>
        public static void GetDirectories(string path, GetDirectoriesCallback callback)
        {
            LocalDataStorage.PLATFORM_IO_ASYNC.GetDirectories(path, callback);
        }
    }
}
