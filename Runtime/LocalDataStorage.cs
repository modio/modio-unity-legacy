using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;
using ModIO.LocalDataIOCallbacks;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class LocalDataStorage
    {
        // TODO(@jackson): REMOVE!
        public class TEMP_PLATFORM_IO_ASYNC
        {
            public static void ReadFile(string path, ReadFileCallback callback)
            {
                byte[] data = null;
                bool success = LocalDataStorage.PLATFORM_IO.ReadFile(path, out data);

                if(callback != null)
                {
                    callback.Invoke(path, success, data);
                }
            }

            public static void WriteFile(string path, byte[] data, WriteFileCallback callback)
            {
                bool success = LocalDataStorage.PLATFORM_IO.WriteFile(path, data);

                if(callback != null)
                {
                    callback.Invoke(path, success);
                }
            }
        }

        // ---------[ Constants ]---------
        /// <summary>Defines the I/O functions to use for this platform.</summary>
        public static readonly IPlatformIO PLATFORM_IO;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static LocalDataStorage()
        {
            // Selects the platform appropriate functions
            #if UNITY_EDITOR
                LocalDataStorage.PLATFORM_IO = new SystemIOWrapper_Editor();
            #else
                LocalDataStorage.PLATFORM_IO = new SystemIOWrapper();
            #endif
        }

        // ---------[ Data Management Interface ]---------
        // ------ File I/O ------
        /// <summary>Reads a file.</summary>
        public static void ReadFile(string path, ReadFileCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.ReadFile(path, onComplete);
        }

        /// <summary>Reads a file and parses the data as a JSON object instance.</summary>
        public static void ReadJSONFile<T>(string path, ReadJSONFileCallback<T> onComplete)
        {
            Debug.Assert(onComplete != null);

            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.ReadFile(path, (p, success, data) =>
            {
                T jsonObject;

                if(success)
                {
                    success = IOUtilities.TryParseUTF8JSONData<T>(data, out jsonObject);

                    if(!success)
                    {
                        Debug.LogWarning("[mod.io] Failed parse file content as JSON Object."
                                         + "\nFile: " + path + "\n\n");
                    }
                }
                else
                {
                    jsonObject = default(T);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(path, success, jsonObject);
                }
            });
        }

        /// <summary>Writes a file.</summary>
        public static void WriteFile(string path, byte[] data, WriteFileCallback onComplete)
        {
            #if DEBUG
            if(data.Length == 0)
            {
                Debug.Log("[mod.io] Writing 0-byte user file to: " + path);
            }
            #endif // DEBUG

            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.WriteFile(path, data, onComplete);
        }

        /// <summary>Writes a JSON file.</summary>
        public static void WriteJSONFile<T>(string path, T jsonObject, WriteFileCallback onComplete)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null && data.Length > 0)
            {
                LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.WriteFile(path, data, onComplete);
            }
            else
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + path + "\n\n");

                if(onComplete != null)
                {
                    onComplete.Invoke(path, false);
                }
            }
        }

        /// <summary>Writes a JSON file.</summary>
        public static bool WriteJSONFile<T>(string path, T jsonObject)
        {
            bool success = false;
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null && data.Length > 0)
            {
                success = LocalDataStorage.PLATFORM_IO.WriteFile(path, data);
            }
            else
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + path + "\n\n");
            }

            return success;
        }

        // ------ File Management ------
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

        /// <summary>Checks for the existence of a file.</summary>
        public static bool GetFileExists(string path)
        {
            return LocalDataStorage.PLATFORM_IO.GetFileExists(path);
        }

        /// <summary>Gets the size of a file in bytes.</summary>
        public static Int64 GetFileSize(string path)
        {
            return LocalDataStorage.PLATFORM_IO.GetFileSize(path);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash)
        {
            return LocalDataStorage.PLATFORM_IO.GetFileSizeAndHash(path, out byteCount, out md5Hash);
        }

        /// <summary>Gets the files at a location.</summary>
        public static IList<string> GetFiles(string path, string nameFilter, bool recurseSubdirectories)
        {
            return LocalDataStorage.PLATFORM_IO.GetFiles(path, nameFilter, recurseSubdirectories);
        }

        // ------ Directory Management ------
        /// <summary>Creates a directory.</summary>
        public static bool CreateDirectory(string path)
        {
            return LocalDataStorage.PLATFORM_IO.CreateDirectory(path);
        }

        /// <summary>Deletes a directory.</summary>
        public static bool DeleteDirectory(string path)
        {
            return LocalDataStorage.PLATFORM_IO.DeleteDirectory(path);
        }

        /// <summary>Moves a directory.</summary>
        public static bool MoveDirectory(string source, string destination)
        {
            return LocalDataStorage.PLATFORM_IO.MoveDirectory(source, destination);
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public static bool GetDirectoryExists(string path)
        {
            return LocalDataStorage.PLATFORM_IO.GetDirectoryExists(path);
        }

        /// <summary>Gets a list of directories found at the given location.</summary>
        public static IList<string> GetDirectories(string path)
        {
            return LocalDataStorage.PLATFORM_IO.GetDirectories(path);
        }
    }
}
