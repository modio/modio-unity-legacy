using UnityEngine;
using ModIO.PlatformIOCallbacks;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class DataStorage
    {
        // ---------[ Constants ]---------
        /// <summary>Defines the I/O functions to use for this platform.</summary>
        public static readonly IPlatformIO PLATFORM_IO;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static DataStorage()
        {
// Selects the platform appropriate functions
#if UNITY_EDITOR
            DataStorage.PLATFORM_IO = new SystemIOWrapper_Editor();
#else
            DataStorage.PLATFORM_IO = new SystemIOWrapper();
#endif

#if DEBUG

            // NOTE(@jackson): Due to hardcoded directory names the following configuration of
            // directories causes errors during the mod installation process.

            const string modCacheDir = "mods";

            string cacheDirNoSep = DataStorage.PLATFORM_IO.CacheDirectory;
            if(IOUtilities.PathEndsWithDirectorySeparator(cacheDirNoSep))
            {
                cacheDirNoSep = cacheDirNoSep.Substring(0, cacheDirNoSep.Length - 1);
            }

            string installDirNoSep = DataStorage.PLATFORM_IO.InstallationDirectory;
            if(IOUtilities.PathEndsWithDirectorySeparator(installDirNoSep))
            {
                installDirNoSep = installDirNoSep.Substring(0, installDirNoSep.Length - 1);
            }

            if(System.IO.Path.GetDirectoryName(installDirNoSep) == cacheDirNoSep
               && installDirNoSep.Substring(cacheDirNoSep.Length + 1) == modCacheDir)
            {
                Debug.LogError("[mod.io] The installation directory cannot be a directory named"
                               + " 'mods' and a child of the cache directory as this will cause"
                               + " issues during the installation process."
                               + "\nPlease change the values in your PluginSettings.");
            }

#endif
        }

        // ---------[ Data Management Interface ]---------
        // ------ Directories ------
        /// <summary>Directory to use for mod installations</summary>
        public static string INSTALLATION_DIRECTORY
        {
            get {
                return DataStorage.PLATFORM_IO.InstallationDirectory;
            }
        }

        /// <summary>Directory to use for cached server data</summary>
        public static string CACHE_DIRECTORY
        {
            get {
                return DataStorage.PLATFORM_IO.CacheDirectory;
            }
        }

        // ------ File I/O ------
        /// <summary>Reads a file.</summary>
        public static void ReadFile(string path, ReadFileCallback onComplete)
        {
            DataStorage.PLATFORM_IO.ReadFile(path, onComplete);
        }

        /// <summary>Reads a file and parses the data as a JSON object instance.</summary>
        public static void ReadJSONFile<T>(string path, ReadJSONFileCallback<T> onComplete)
        {
            Debug.Assert(onComplete != null);

            DataStorage.PLATFORM_IO.ReadFile(path, (p, success, data) => {
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
            DataStorage.PLATFORM_IO.WriteFile(path, data, onComplete);
        }

        /// <summary>Writes a JSON file.</summary>
        public static void WriteJSONFile<T>(string path, T jsonObject, WriteFileCallback onComplete)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null && data.Length > 0)
            {
                DataStorage.PLATFORM_IO.WriteFile(path, data, onComplete);
            }
            else
            {
                Debug.LogWarning(
                    "[mod.io] Failed create JSON representation of object before writing file."
                    + "\nFile: " + path + "\n\n");

                if(onComplete != null)
                {
                    onComplete.Invoke(path, false);
                }
            }
        }

        // ------ File Management ------
        /// <summary>Deletes a file.</summary>
        public static void DeleteFile(string path, DeleteFileCallback onComplete)
        {
            DataStorage.PLATFORM_IO.DeleteFile(path, onComplete);
        }

        /// <summary>Moves a file.</summary>
        public static void MoveFile(string source, string destination, MoveFileCallback onComplete)
        {
            DataStorage.PLATFORM_IO.MoveFile(source, destination, onComplete);
        }

        /// <summary>Checks for the existence of a file.</summary>
        public static void GetFileExists(string path, GetFileExistsCallback onComplete)
        {
            DataStorage.PLATFORM_IO.GetFileExists(path, onComplete);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback onComplete)
        {
            DataStorage.PLATFORM_IO.GetFileSizeAndHash(path, onComplete);
        }

        /// <summary>Gets the files at a location.</summary>
        public static void GetFiles(string path, string nameFilter, bool recurseSubdirectories,
                                    GetFilesCallback onComplete)
        {
            DataStorage.PLATFORM_IO.GetFiles(path, nameFilter, recurseSubdirectories, onComplete);
        }

        // ------ Directory Management ------
        /// <summary>Creates a directory.</summary>
        public static void CreateDirectory(string path, CreateDirectoryCallback onComplete)
        {
            DataStorage.PLATFORM_IO.CreateDirectory(path, onComplete);
        }

        /// <summary>Deletes a directory.</summary>
        public static void DeleteDirectory(string path, DeleteDirectoryCallback onComplete)
        {
            DataStorage.PLATFORM_IO.DeleteDirectory(path, onComplete);
        }

        /// <summary>Moves a directory.</summary>
        public static void MoveDirectory(string source, string destination,
                                         MoveDirectoryCallback onComplete)
        {
            DataStorage.PLATFORM_IO.MoveDirectory(source, destination, onComplete);
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public static void GetDirectoryExists(string path, GetDirectoryExistsCallback onComplete)
        {
            DataStorage.PLATFORM_IO.GetDirectoryExists(path, onComplete);
        }

        /// <summary>Gets a list of directories found at the given location.</summary>
        public static void GetDirectories(string path, GetDirectoriesCallback onComplete)
        {
            DataStorage.PLATFORM_IO.GetDirectories(path, onComplete);
        }
    }
}
