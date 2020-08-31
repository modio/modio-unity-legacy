using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class PlatformIO : PlatformIOBase
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>Temporary Data directory path.</summary>
        private static readonly string TEMPORARY_DATA_DIRECTORY = IOUtilities.CombinePath(UnityEngine.Application.temporaryCachePath,
                                                                                          "modio_" + PluginSettings.GAME_ID.ToString("x8"));
        /// <summary>Persistent Data directory path.</summary>
        private static readonly string PERSISTENT_DATA_DIRECTORY = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                                                           "modio");

        // ---------[ IPlatformIO Interface ]---------
        // --- Accessors ---
        /// <summary>Temporary Data directory path.</summary>
        public override string TemporaryDataDirectory
        {
            get { return PlatformIO.TEMPORARY_DATA_DIRECTORY; }
        }

        /// <summary>Persistent Data directory path.</summary>
        public override string PersistentDataDirectory
        {
            get { return PlatformIO.PERSISTENT_DATA_DIRECTORY; }
        }
    }
}
