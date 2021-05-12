namespace ModIO
{
    /// <summary>Defines the static interface for the compression operations.</summary>
    public static class CompressionModule
    {
        // ---------[ Constants ]---------
        /// <summary>Compression implementation to use.</summary>
        public static readonly ICompressionImpl IMPLEMENTATION;

        // ---------[ Initialization ]---------
        /// <summary>Loads the compression implementation.</summary>
        static CompressionModule()
        {
            CompressionModule.IMPLEMENTATION = null;
        }

        // ---------[ Interface ]---------
        /// <summary>Extracts the contents of an archive.</summary>
        public static bool ExtractAll(string archivePath, string targetDirectory)
        {
            return CompressionModule.IMPLEMENTATION.ExtractAll(archivePath, targetDirectory);
        }

        /// <summary>Compresses the contents of a directory.</summary>
        public static bool CompressDirectory(string directoryPath, string outputPath)
        {
            return CompressionModule.IMPLEMENTATION.CompressDirectory(directoryPath, outputPath);
        }

        /// <summary>Compresses a single file into an output archive.</summary>
        public static bool CompressFile(string filePath, string outputPath)
        {
            return CompressionModule.IMPLEMENTATION.CompressFile(filePath, outputPath);
        }
    }
}
