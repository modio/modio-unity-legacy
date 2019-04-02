using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

using Exception = System.Exception;

namespace ModIO
{
    public static class IOUtilities
    {
        /// <summary>Reads an entire file and parses the JSON Object it contains.</summary>
        /// <para>This function is a simple wrapper for reading text from a file
        /// and deserializing it, but handles any exceptions, logging a debug
        /// warning if the read and/or deserialization fails.</para>
        /// <param name="filePath">Location of the file to be read</param>
        /// <returns>A new object containing the values parsed from the file. If
        /// the read or deserialization fails, this function returns `null` for
        /// objects or the default value for simple types.</returns>
        public static T ReadJsonObjectFile<T>(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            T jsonObject;
            TryReadJsonObjectFile(filePath, out jsonObject);
            return jsonObject;
        }

        /// <summary>Reads an entire file and parses the JSON Object it contains.</summary>
        public static bool TryReadJsonObjectFile<T>(string filePath, out T jsonObject)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            if(File.Exists(filePath))
            {
                try
                {
                    jsonObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                    return true;
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read json object from file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            jsonObject = default(T);
            return false;
        }

        /// <summary>Writes an object to a file in the JSON Object format.</summary>
        /// <para>This function is a simple wrapper for serializing an object
        /// and writing text to a file but handles any exceptions.</para>
        /// <para>If the target file does not exist it will be created, if it
        /// does exist, the target file will be overwritten.</para>
        /// <para>See also: [[ModIO.IOUtilities.ReadJsonObjectFile]],
        /// [System.IO.File.WriteAllText](https://docs.microsoft.com/en-us/dotnet/api/system.io.file.writealltext?view=netframework-4.7.2)
        /// </para>
        /// <param name="filePath">Location of the file to be write</param>
        /// <param name="jsonObject">Object to serialize.</param>
        /// <returns>**TRUE** if the object was serialized and written to a the
        /// file successfully.</returns>
        public static bool WriteJsonObjectFile<T>(string filePath,
                                                  T jsonObject)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, JsonConvert.SerializeObject(jsonObject));
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write json object to file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Loads an entire binary file as a byte array.</summary>
        /// <para>This function is a simple wrapper for
        /// <a href="https://msdn.microsoft.com/en-us/library/system.io.file.readallbytes(v=vs.110).aspx">File.ReadAllBytes</a>,
        /// but handles any exceptions, logging a debug warning if the read
        /// fails. It loads the entire binary file synchronously, and as such
        /// blocks the thread until the read is completed. Thus it is
        /// recommended that this function is only used for reading smaller
        /// files.</para>
        /// <param name="filePath">Location of the file to be read</param>
        /// <returns>The data read from the binary file. If the read fails, this
        /// function returns `null`.</returns>
        public static byte[] LoadBinaryFile(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            byte[] fileData = null;
            TryLoadBinaryFile(filePath, out fileData);
            return fileData;
        }

        /// <summary>Loads an entire binary file as a byte array.</summary>
        public static bool TryLoadBinaryFile(string filePath, out byte[] output)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            if(File.Exists(filePath))
            {
                try
                {
                    output = File.ReadAllBytes(filePath);
                    return true;
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read binary file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            output = null;
            return false;
        }

        /// <summary>Writes an entire binary file.</summary>
        /// <para>This function is a simple wrapper for
        /// [System.IO.File.WriteAllBytes](https://msdn.microsoft.com/en-us/library/system.io.file.writeallbytes(v=vs.110).aspx),
        /// but handles any exceptions.</para>
        /// <para>If the target file does not exist it will be created, if it
        /// does exist, the target file will be overwritten.</para>
        /// <para>It writes the entire binary file synchronously, and as such
        /// blocks the thread until the write is completed. Thus it is
        /// recommended that this function is only used for writing smaller
        /// files or on a separate thread.</para>
        /// <param name="filePath">Location of the file to be written</param>
        /// <param name="data">Data to write</param>
        /// <returns>**TRUE** if the file was successfully written.</returns>
        public static bool WriteBinaryFile(string filePath,
                                           byte[] data)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write binary file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Loads the image data from a file into a new Texture.</summary>
        /// <para>This function is a simple wrapper for reading image data from
        /// a file and creating a texture for it, but handles any exceptions.</para>
        /// <para>It loads the entire image file synchronously, and as such
        /// blocks the thread until the load is completed. Thus it is
        /// recommended that this function is only used for loading smaller
        /// files or on a separate thread.</para>
        /// <param name="filePath">Location of the image file to load.</param>
        /// <returns>A new
        /// [Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html)
        /// containing the image data parsed from the file. If the read fails,
        /// this function returns `null`.</returns>
        public static Texture2D ReadImageFile(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            Texture2D texture = null;
            TryReadImageFile(filePath, out texture);
            return texture;
        }

        /// <summary>Loads the image data from a file into a new Texture.</summary>
        public static bool TryReadImageFile(string filePath, out Texture2D texture)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            if(File.Exists(filePath))
            {
                byte[] imageData;
                bool readSuccessful = IOUtilities.TryLoadBinaryFile(filePath, out imageData);

                if(readSuccessful
                   && imageData != null
                   && imageData.Length > 0)
                {
                    texture = new Texture2D(0,0);
                    texture.LoadImage(imageData);
                    return true;
                }
            }

            texture = null;
            return false;
        }

        /// <summary>Writes a texture to a PNG file.</summary>
        /// <para>This function is a simple wrapper for writing image data to a
        /// file, but handles any exceptions.</para>
        /// <para>If the target file does not exist it will be created, if it
        /// does exist, the target file will be overwritten.</para>
        /// <para>It writes the entire image file synchronously, and as such
        /// blocks the thread until the write is completed. Thus it is
        /// recommended that this function is only used for writing smaller
        /// files or on a separate thread.</para>
        /// <param name="filePath">Location of the file to be written</param>
        /// <param name="data">Texture containing the image data to be written</param>
        /// <returns>**TRUE** if the file was successfully written.</returns>
        public static bool WritePNGFile(string filePath,
                                        Texture2D texture)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            Debug.Assert(Path.GetExtension(filePath).Equals(".png"),
                         "[mod.io] Images can only be saved in PNG format."
                         + "\n" + filePath
                         + " is an invalid file path.");

            return IOUtilities.WriteBinaryFile(filePath, texture.EncodeToPNG());
        }

        /// <summary>Deletes a file.</summary>
        /// <para>This function is a simple wrapper for
        /// [System.IO.File.Delete](https://msdn.microsoft.com/en-us/library/system.io.file.delete(v=vs.110).aspx),
        /// but handles any exceptions.</para>
        /// <param name="filePath">Location of the file to be deleted</param>
        /// <returns>**TRUE** if the file was successfully deleted.</returns>
        public static bool DeleteFile(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));

            try
            {
                if(File.Exists(filePath)) { File.Delete(filePath); }
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Creates a directory.</summary>
        public static bool CreateDirectory(string directoryPath)
        {
            Debug.Assert(!String.IsNullOrEmpty(directoryPath));

            try
            {
                Directory.CreateDirectory(directoryPath);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create directory."
                                      + "\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Deletes a directory.</summary>
        /// <para>This function is a simple wrapper for
        /// [System.IO.Directory.Delete](https://msdn.microsoft.com/en-us/library/system.io.directory.delete(v=vs.110).aspx),
        /// but handles any exceptions.</para>
        /// <param name="directoryPath">Location of the directory to be deleted</param>
        /// <returns>**TRUE** if the directory was successfully deleted.</returns>
        public static bool DeleteDirectory(string directoryPath)
        {
            Debug.Assert(!String.IsNullOrEmpty(directoryPath));

            try
            {
                if(Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete directory."
                                      + "\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Creates a path using System.IO.Path.Combine().</summary>
        public static string CombinePath(params string[] pathElements)
        {
            Debug.Assert(pathElements != null);

            string retVal = string.Empty;

            foreach(string pathElem in pathElements)
            {
                retVal = System.IO.Path.Combine(retVal, pathElem);
            }

            return retVal;
        }

        /// <summary>Gets the size (in bytes) of a given file.</summary>
        public static Int64 GetFileSize(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));
            Debug.Assert(File.Exists(filePath));

            try
            {
                return (new FileInfo(filePath)).Length;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to calculate file size."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
            }
            return -1;
        }

        /// <summary>Calculates the MD5 Hash for a given file.</summary>
        public static string CalculateFileMD5Hash(string filePath)
        {
            Debug.Assert(!String.IsNullOrEmpty(filePath));
            Debug.Assert(File.Exists(filePath));

            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = System.IO.File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        return hashString;
                    }
                }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to calculate file hash."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
            }

            return null;
        }

        /// <summary>Gets the name of the item (file/folder) at the given path.</summary>
        public static string GetPathItemName(string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            // remove any separators
            char lastCharacter = path[path.Length - 1];

            while(path.Length > 1
                  && (lastCharacter == Path.DirectorySeparatorChar
                      || lastCharacter == Path.DirectorySeparatorChar))
            {
                path = path.Remove(path.Length - 1);
                lastCharacter = path[path.Length - 1];
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
    }
}
