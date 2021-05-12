using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Interface for a compression implementation.</summary>
    public interface ICompressionImpl
    {
        // ---------[ Interface ]---------
        /// <summary>Extracts the contents of an archive.</summary>
        bool ExtractAll(string archivePath, string targetDirectory);

        /// <summary>Compresses the contents of a file collection into an output archive.</summary>
        bool CompressFileCollection(string rootDirectory, IEnumerable<string> fileCollection, string outputPath);

        /// <summary>Compresses a single file into an output archive.</summary>
        bool CompressFile(string filePath, string outputPath);
    }
}
