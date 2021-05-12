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
    }
}
