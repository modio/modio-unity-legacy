using System.Collections.Generic;

using Exception = System.Exception;
using Debug = UnityEngine.Debug;
using Path = System.IO.Path;

namespace ModIO
{
    /// <summary>A wrapper for the DotNetZip library that matches the ICompressionImpl interface.</summary>
    public class DotNetZipCompressionImpl : ICompressionImpl
    {
        // ---------[ Interface ]---------
        /// <summary>Extracts the contents of an archive.</summary>
        public bool ExtractAll(string archivePath, string targetDirectory)
        {
            // early outs
            if(string.IsNullOrEmpty(archivePath))
            {
                Debug.LogWarning("[mod.io] Unable to extract archive to target directory."
                                 + "\narchivePath is NULL or EMPTY.");
                return false;
            }
            if(string.IsNullOrEmpty(targetDirectory))
            {
                Debug.LogWarning("[mod.io] Unable to extract archive to target directory."
                                 + "\ntargetDirectory is NULL or EMPTY.");
                return false;
            }

            // Extract
            bool success = false;

            try
            {
                using (var zip = Ionic.Zip.ZipFile.Read(archivePath))
                {
                    zip.ExtractAll(targetDirectory);

                    success = true;
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Unable to extract archive to target directory."
                                 + "\nArchive: " + archivePath
                                 + "\nTarget: " + targetDirectory
                                 + "\n\n"
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        /// <summary>Compresses the contents of a file collection into an output archive.</summary>
        public bool CompressFileCollection(string rootDirectory, IEnumerable<string> fileCollection, string outputPath)
        {
            // early outs
            if(string.IsNullOrEmpty(rootDirectory))
            {
                Debug.LogWarning("[mod.io] Unable to compress file collection to archive."
                                 + "\nrootDirectory is NULL or EMPTY.");
                return false;
            }
            if(fileCollection == null)
            {
                Debug.LogWarning("[mod.io] Unable to compress file collection to archive."
                                 + "\nfileCollection is NULL.");
                return false;
            }
            if(string.IsNullOrEmpty(outputPath))
            {
                Debug.LogWarning("[mod.io] Unable to compress file collection to archive."
                                 + "\noutputPath is NULL or EMPTY.");
                return false;
            }

            // compress
            bool success = false;
            string lastFilePath = string.Empty;

            try
            {
                using(var zip = new Ionic.Zip.ZipFile())
                {
                    foreach(string filePath in fileCollection)
                    {
                        lastFilePath = filePath;

                        string relativeFilePath = filePath.Substring(rootDirectory.Length);
                        string relativeDirectory = Path.GetDirectoryName(relativeFilePath);

                        zip.AddFile(filePath, relativeDirectory);
                    }

                    zip.Save(outputPath);

                    success = true;
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Unable to compress file collection to archive."
                                 + "\nLast Attempted File: " + lastFilePath
                                 + "\nOutput: " + outputPath
                                 + "\n\n"
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }

        /// <summary>Compresses a single file into an output archive.</summary>
        public bool CompressFile(string filePath, string outputPath)
        {
            // early outs
            if(string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[mod.io] Unable to compress file collection to archive."
                                 + "\nfilePath is NULL or EMPTY.");
                return false;
            }
            if(string.IsNullOrEmpty(outputPath))
            {
                Debug.LogWarning("[mod.io] Unable to compress file collection to archive."
                                 + "\noutputPath is NULL or EMPTY.");
                return false;
            }

            // compress
            bool success = false;

            try
            {
                using(var zip = new Ionic.Zip.ZipFile())
                {
                    zip.AddFile(filePath, "");
                    zip.Save(outputPath);
                    success = true;
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Unable to compress file to archive."
                                 + "\nFile: " + filePath
                                 + "\nOutput: " + outputPath
                                 + "\n\n"
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return success;
        }
    }
}
