using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class SystemIO : IPlatformIO
    {
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        public byte[] ReadFile(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            byte[] data = null;

            if(File.Exists(filePath))
            {
                try
                {
                    data = File.ReadAllBytes(filePath);
                }
                catch(Exception e)
                {
                    data = null;

                    string warningInfo = ("[mod.io] Failed to read file.\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }

            return data;
        }

        /// <summary>Writes a file.</summary>
        public bool WriteFile(string filePath, byte[] data)
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

            return success;
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public bool DeleteFile(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            bool success = false;
            try
            {
                if(File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                success = true;
            }
            catch(Exception e)
            {
                success = false;

                string warningInfo = ("[mod.io] Failed to delete file.\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        /// <summary>Moves a file.</summary>
        public bool MoveFile(string sourceFilePath, string destinationFilePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceFilePath));
            Debug.Assert(!string.IsNullOrEmpty(destinationFilePath));

            bool success = true;
            string failMessage = null;

            if(!File.Exists(sourceFilePath))
            {
                failMessage = ("Failed to move file as the source file does not exist."
                               + "\nSource File: " + sourceFilePath
                               + "\nDestination: " + destinationFilePath);
                success = false;
            }
            else
            {
                if(File.Exists(destinationFilePath))
                {
                    try
                    {
                        File.Delete(destinationFilePath);
                    }
                    catch(Exception e)
                    {
                        failMessage = ("Failed to move file as the existing file at the destination could not be deleted."
                                       + "\nSource File: " + sourceFilePath
                                       + "\nDestination: " + destinationFilePath
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));

                        success = false;
                    }
                }

                if(success)
                {
                    try
                    {
                        File.Move(sourceFilePath, destinationFilePath);
                    }
                    catch(Exception e)
                    {
                        success = false;

                        failMessage = ("Failed to move file."
                                       + "\nSource File: " + sourceFilePath
                                       + "\nDestination: " + destinationFilePath
                                       + "\n\n" + Utility.GenerateExceptionDebugString(e));
                    }
                }
            }

            if(!success)
            {
                Debug.LogWarning("[mod.io] " + failMessage);
            }

            return success;
        }

        /// <summary>Gets the size of a file.</summary>
        public bool GetFileExists(string filePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the size of a file.</summary>
        public Int64 GetFileSize(string filePath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public bool GetFileSizeAndHash(string filePath, out Int64 byteCount, out string md5Hash)
        {
            throw new System.NotImplementedException();
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public bool CreateDirectory(string directoryPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Deletes a directory.</summary>
        public bool DeleteDirectory(string directoryPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Moves a directory.</summary>
        public bool MoveDirectory(string sourcePath, string destinationPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public IList<string> GetDirectories(string directoryPath)
        {
            throw new System.NotImplementedException();
        }
    }
}
