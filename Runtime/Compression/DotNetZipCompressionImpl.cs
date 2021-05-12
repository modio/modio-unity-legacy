using Exception = System.Exception;
using Debug = UnityEngine.Debug;

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

        /// <summary>Compresses the contents of a directory.</summary>
        public bool CompressDirectory(string directoryPath, string outputPath)
        {
            throw new System.NotImplementedException();
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
