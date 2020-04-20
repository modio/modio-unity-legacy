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
                LocalDataStorage.PLATFORM_IO = new SystemIOWrapper();
                LocalDataStorage.PLATFORM_IO_ASYNC = new StandaloneIO();
            #endif
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Reads a file.</summary>
        public static bool ReadFile(string path, out byte[] data)
        {
            return LocalDataStorage.PLATFORM_IO.ReadFile(path, out data);
        }

        /// <summary>Reads a file and parses the data as a JSON object instance.</summary>
        public static bool ReadJSONFile<T>(string path, out T jsonObject)
        {
            bool success = false;
            byte[] data = null;

            success = LocalDataStorage.PLATFORM_IO.ReadFile(path, out data);

            if(success)
            {
                success = IOUtilities.TryParseUTF8JSONData<T>(data, out jsonObject);

                if(!success)
                {
                    Debug.LogWarning("[mod.io] Failed translate file data into JSON Object."
                                     + "\nFile: " + path + "\n\n");
                }
            }
            else
            {
                jsonObject = default(T);
            }

            return success;
        }

        /// <summary>Writes a file.</summary>
        public static bool WriteFile(string path, byte[] data)
        {
            #if DEBUG
            if(data.Length == 0)
            {
                Debug.Log("[mod.io] Writing 0-byte user file to: " + path);
            }
            #endif // DEBUG

            return LocalDataStorage.PLATFORM_IO.WriteFile(path, data);
        }

        /// <summary>Writes a JSON file.</summary>
        public static bool WriteJSONFile<T>(string path, T jsonObject)
        {
            bool success = false;
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null && data.Length > 0)
            {
                success = LocalDataStorage.WriteFile(path, data);
            }
            else
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + path + "\n\n");
            }

            return success;
        }

        /// <summary>Deletes a file.</summary>
        public static bool DeleteFile(string path)
        {
            return LocalDataStorage.PLATFORM_IO.DeleteFile(path);
        }

        /// <summary>Moves a file.</summary>
        public static bool MoveFile(string source, string destination)
        {
            return LocalDataStorage.PLATFORM_IO.MoveFile(source, destination);
        }

        /// <summary>Creates a directory.</summary>
        public static bool CreateDirectory(string path)
        {
            return LocalDataStorage.PLATFORM_IO.CreateDirectory(path);
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
