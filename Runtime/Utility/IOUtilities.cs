﻿using System;
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
            if(data == null || data.Length == 0) { return null; }

            Texture2D texture = new Texture2D(0,0);
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

            if(path.Length == 0) { return string.Empty; }

            // get parent directory and remove
            string folderName = path;
            string parentDirectory = Path.GetDirectoryName(path);
            if(!String.IsNullOrEmpty(parentDirectory))
            {
                folderName = path.Substring(parentDirectory.Length + 1);
            }

            return folderName;
        }

        /// <summary>Determines if the final character of the string is a directory separator.</summary>
        public static bool PathEndsWithDirectorySeparator(string path)
        {
            Debug.Assert(path != null);

            if(path.Length == 0) { return false; }

            char lastCharacter = path[path.Length - 1];
            return (lastCharacter == Path.DirectorySeparatorChar
                    || lastCharacter == Path.AltDirectorySeparatorChar);
        }

        /// <summary>Collection of invalid Windows file names.</summary>
        public static readonly string[] INVALID_FILENAMES_WIN = new string[]
        {
            "AUX",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "CON",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9",
            "NUL",
            "PRN",
        };

        /// <summary>Max file name length.</summary>
        public const int MAX_FILENAME_LENGTH = 255;

        /// <summary>Illegal character regex.</summary>
        public static readonly string ILLEGAL_CHAR_REGEX = string.Format("[{0}]", Regex.Escape("\\/?\"<>|:*%.\0" + new string(Path.GetInvalidFileNameChars())));

        /// <summary>Replaces any illegal filename characters to create an OS safe file name.</summary>
        public static string MakeValidFileName(string input, string extension = null)
        {
            Debug.Assert(input != null);
            Debug.Assert(extension == null || extension.Length < IOUtilities.MAX_FILENAME_LENGTH - 2);

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
            else if(extension.Length > 0
                    && extension[0] != '.')
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
    }
}
