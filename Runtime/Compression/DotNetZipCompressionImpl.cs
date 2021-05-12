namespace ModIO
{
    /// <summary>A wrapper for the DotNetZip library that matches the ICompressionImpl interface.</summary>
    public class DotNetZipCompressionImpl : ICompressionImpl
    {
        // ---------[ Interface ]---------
        /// <summary>Extracts the contents of an archive.</summary>
        public bool ExtractAll(string archivePath, string targetDirectory)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Compresses the contents of a directory.</summary>
        public bool CompressDirectory(string directoryPath, string outputPath)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Compresses a single file into an output archive.</summary>
        public bool CompressFile(string filePath, string outputPath)
        {
            throw new System.NotImplementedException();
        }
    }
}
