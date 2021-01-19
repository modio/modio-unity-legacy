#if UNITY_STANDALONE

using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class PlatformIO_Standalone : PlatformIOBase
    {
        // ---------[ Initialization ]---------
        /// <summary>Sets an instance of this class as the DataStorage IO Module.</summary>
        [RuntimeInitializeOnLoadMethod]
        static void InitializeAsDataStorageModule()
        {
            DataStorage.SetIOModule(new PlatformIO_Standalone());
        }

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
            get { return PlatformIO_Standalone.TEMPORARY_DATA_DIRECTORY; }
        }

        /// <summary>Persistent Data directory path.</summary>
        public override string PersistentDataDirectory
        {
            get { return PlatformIO_Standalone.PERSISTENT_DATA_DIRECTORY; }
        }
    }
}

#endif // UNITY_STANDALONE
