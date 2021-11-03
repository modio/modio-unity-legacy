using System;
using System.Text;
using System.Text.RegularExpressions;
using Path = System.IO.Path;

using Newtonsoft.Json;

using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ModIO
{
    public static class IOUtilities
    {
        /// <summary>Parse data as image.</summary>
        public static Texture2D ParseImageData(byte[] data)
        {
            if(data == null || data.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(0, 0);
            texture.LoadImage(data);
            return texture;
        }

        /// <summary>Attempts to parse the data of a JSON file.</summary>
        public static bool TryParseUTF8JSONData<T>(byte[] data, out T jsonObject)
        {
            bool success = false;

            if(data != null)
            {
                try
                {
                    string dataString = Encoding.UTF8.GetString(data);
                    jsonObject = JsonConvert.DeserializeObject<T>(dataString);
                    success = true;
                }
                catch
                {
                    jsonObject = default(T);
                    success = false;
                }
            }
            else
            {
                jsonObject = default(T);
            }

            return success;
        }

        /// <summary>Generates the byte array for a JSON representation.</summary>
        public static byte[] GenerateUTF8JSONData<T>(T jsonObject)
        {
            Debug.Assert(jsonObject != null);

            byte[] data = null;

            try
            {
                string dataString = JsonConvert.SerializeObject(jsonObject);
                data = Encoding.UTF8.GetBytes(dataString);
            }
            catch
            {
                data = null;
            }

            return data;
        }

        /// <summary>Creates a path using System.IO.Path.Combine().</summary>
        public static string CombinePath(params string[] pathElements)
        {
            Debug.Assert(pathElements != null);

            string retVal = string.Empty;

            if(pathElements != null)
            {
                foreach(string pathElem in pathElements)
                {
                    if(!string.IsNullOrEmpty(pathElem))
                    {
                        retVal = Path.Combine(retVal, pathElem);
                    }
                }
            }

            return retVal;
        }

        /// <summary>Gets the name of the item (file/folder) at the given path.</summary>
        public static string GetPathItemName(string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            // remove any separators
            while(IOUtilities.PathEndsWithDirectorySeparator(path))
            {
                path = path.Remove(path.Length - 1);
            }

            if(path.Length == 0)
            {
                return string.Empty;
            }

            // get parent directory and remove
            string folderName = path;
            string parentDirectory = Path.GetDirectoryName(path);
            if(!String.IsNullOrEmpty(parentDirectory))
            {
                folderName = path.Substring(parentDirectory.Length + 1);
            }

            return folderName;
        }

        /// <summary>Determines if the final character of the string is a directory
        /// separator.</summary>
        public static bool PathEndsWithDirectorySeparator(string path)
        {
            Debug.Assert(path != null);

            if(path.Length == 0)
            {
                return false;
            }

            char lastCharacter = path[path.Length - 1];
            return (lastCharacter == Path.DirectorySeparatorChar
                    || lastCharacter == Path.AltDirectorySeparatorChar);
        }

        /// <summary>Collection of invalid Windows file names.</summary>
        public static readonly string[] INVALID_FILENAMES_WIN = new string[] {
            "AUX",  "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "CON",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "NUL",  "PRN",
        };

        /// <summary>Max file name length.</summary>
        public const int MAX_FILENAME_LENGTH = 255;

        /// <summary>Illegal character regex.</summary>
        public static readonly string ILLEGAL_CHAR_REGEX = string.Format(
            "[{0}]", Regex.Escape("\\/?\"<>|:*%.\0" + new string(Path.GetInvalidFileNameChars())));

        /// <summary>Replaces any illegal filename characters to create an OS safe file
        /// name.</summary>
        public static string MakeValidFileName(string input, string extension = null)
        {
            Debug.Assert(input != null);
            Debug.Assert(extension == null
                         || extension.Length < IOUtilities.MAX_FILENAME_LENGTH - 2);

            // format extension
            if(extension == null)
            {
                int periodIndex = input.LastIndexOf(".");
                if(periodIndex >= 0)
                {
                    extension = input.Substring(periodIndex);
                    input = input.Substring(0, periodIndex);
                }
                else
                {
                    extension = string.Empty;
                }
            }
            else if(extension.Length > 0 && extension[0] != '.')
            {
                extension = "." + extension;
            }

            // check illegal filenames
            if(input.Length == 0)
            {
                input = "_unknown";
            }
            else
            {
                bool wasFixed = false;

                string inputUpper = input.ToUpper();
                foreach(string illegalName in IOUtilities.INVALID_FILENAMES_WIN)
                {
                    if(inputUpper == illegalName)
                    {
                        input = "_" + input + "_";
                        wasFixed = true;
                        break;
                    }
                }

                if(!wasFixed)
                {
                    Regex r = new Regex("\\s");
                    input = r.Replace(input, "");
                    r = new Regex(ILLEGAL_CHAR_REGEX);
                    input = r.Replace(input, "_");
                }
            }

            // check length
            if(input.Length + extension.Length > IOUtilities.MAX_FILENAME_LENGTH)
            {
                input = input.Substring(0, IOUtilities.MAX_FILENAME_LENGTH);
            }

            return input + extension;
        }

        // ---------[ Obsolete ]---------
        /// <summary>[Obsolete] Loads an entire binary file as a byte array.</summary>
        [Obsolete("Use DataStorage.ReadFile() instead.")]
        public static bool TryLoadBinaryFile(string filePath, out byte[] output)
        {
            bool success = false;
            byte[] data = null;

            DataStorage.ReadFile(filePath, (p, s, d) => {
                success = s;
                data = d;
            });

            output = data;

            return success;
        }

        /// <summary>[Obsolete] Loads an entire binary file as a byte array.</summary>
        [Obsolete("Use DataStorage.ReadFile() instead.")]
        public static byte[] LoadBinaryFile(string filePath)
        {
            byte[] data = null;

            DataStorage.ReadFile(filePath, (p, s, d) => { data = d; });

            return data;
        }

        /// <summary>[Obsolete] Loads the image data from a file into a new Texture.</summary>
        [Obsolete("Use DataStorage.ReadFile() and IOUtilities.ParseImageData() instead.")]
        public static Texture2D ReadImageFile(string filePath)
        {
            Texture2D parsed = null;
            byte[] data = LoadBinaryFile(filePath);

            if(data != null)
            {
                parsed = IOUtilities.ParseImageData(data);
            }

            return parsed;
        }

        /// <summary>[Obsolete] Loads the image data from a file into a new Texture.</summary>
        [Obsolete("Use DataStorage.ReadFile() and IOUtilities.ParseImageData() instead.")]
        public static bool TryReadImageFile(string filePath, out Texture2D texture)
        {
            Texture2D parsed = null;
            bool success = false;
            byte[] data = null;

            success = TryLoadBinaryFile(filePath, out data);

            if(success)
            {
                parsed = IOUtilities.ParseImageData(data);
            }

            texture = parsed;
            return success;
        }

        /// <summary>[Obsolete] Reads an entire file and parses the JSON Object it
        /// contains.</summary>
        [Obsolete("Use DataStorage.ReadJSONFile() instead.")]
        public static T ReadJsonObjectFile<T>(string path)
        {
            T result = default(T);

            DataStorage.ReadJSONFile<T>(path, (p, s, r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Reads an entire file and parses the JSON Object it
        /// contains.</summary>
        [Obsolete("Use DataStorage.ReadJSONFile() instead.")]
        public static bool TryReadJsonObjectFile<T>(string path, out T jsonObject)
        {
            T result = default(T);
            bool success = false;

            DataStorage.ReadJSONFile<T>(path, (p, s, r) => {
                success = s;
                result = r;
            });

            jsonObject = result;
            return success;
        }

        /// <summary>[Obsolete] Writes an entire binary file.</summary>
        [Obsolete("Use DataStorage.WriteFile() instead.")]
        public static bool WriteBinaryFile(string path, byte[] data)
        {
            bool result = false;
            DataStorage.WriteFile(path, data, (p, s) => result = s);
            return result;
        }

        /// <summary>[Obsolete] Writes a texture to a PNG file.</summary>
        [Obsolete("Use DataStorage.WriteFile() and Texture2D.EncodeToPNG() instead.")]
        public static bool WritePNGFile(string path, Texture2D texture)
        {
            byte[] data = null;
            bool result = false;

            if(texture != null)
            {
                data = texture.EncodeToPNG();

                if(data != null)
                {
                    DataStorage.WriteFile(path, data, (p, s) => result = s);
                }
            }

            return result;
        }

        /// <summary>[Obsolete] Writes an object to a file in the JSON Object format.</summary>
        [Obsolete("Use DataStorage.WriteJSONFile() instead.")]
        public static bool WriteJsonObjectFile<T>(string filePath, T jsonObject)
        {
            bool result = false;
            DataStorage.WriteJSONFile<T>(filePath, jsonObject, (p, s) => result = s);
            return result;
        }

        /// <summary>[Obsolete] Deletes a file.</summary>
        [Obsolete("Use DataStorage.DeleteFile() instead.")]
        public static bool DeleteFile(string filePath)
        {
            bool result = false;
            DataStorage.DeleteFile(filePath, (p, s) => result = s);
            return result;
        }

        /// <summary>[Obsolete] Creates a directory.</summary>
        [Obsolete("Use DataStorage.CreateDirectory() instead.")]
        public static bool CreateDirectory(string directoryPath)
        {
            bool result = false;
            DataStorage.CreateDirectory(directoryPath, (p, s) => result = s);
            return result;
        }

        /// <summary>[Obsolete] Deletes a directory.</summary>
        [Obsolete("Use DataStorage.DeleteDirectory() instead.")]
        public static bool DeleteDirectory(string directoryPath)
        {
            bool result = false;
            DataStorage.DeleteDirectory(directoryPath, (p, s) => result = s);
            return result;
        }

        /// <summary>[Obsolete] Gets the size (in bytes) of a given file.</summary>
        [Obsolete("Use DataStorage.GetFileSizeAndHash() instead.")]
        public static Int64 GetFileSize(string filePath)
        {
            Int64 byteCount = -1;

            DataStorage.GetFileSizeAndHash(
                filePath, (path, success, fileSize, fileHash) => { byteCount = fileSize; });

            return byteCount;
        }

        /// <summary>[Obsolete] Calculates the MD5 Hash for a given file.</summary>
        [Obsolete("Use DataStorage.GetFileSizeAndHash() instead.")]
        public static string CalculateFileMD5Hash(string filePath)
        {
            string hash = string.Empty;

            DataStorage.GetFileSizeAndHash(
                filePath, (path, success, fileSize, fileHash) => { hash = fileHash; });

            return hash;
        }
    }
}
