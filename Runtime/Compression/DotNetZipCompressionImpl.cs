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
