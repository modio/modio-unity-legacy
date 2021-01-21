#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality and adds AssetDatabase refreshes.</summary>
    public class PlatformIO_Editor : PlatformIOBase
    {
        // ---------[ Initialization ]---------
        /// <summary>Sets an instance of this class as the DataStorage IO Module.</summary>
        [RuntimeInitializeOnLoadMethod]
        static void InitializeAsDataStorageModule()
        {
            DataStorage.SetIOModule(new PlatformIO_Editor(), true);
        }

        // ---------[ CONSTANTS ]---------
        /// <summary>Temporary Data directory path.</summary>
        public static readonly string TEMPORARY_DATA_DIRECTORY = IOUtilities.CombinePath(System.IO.Directory.GetCurrentDirectory(),
                                                                                         "Temp",
                                                                                         "mod.io",
                                                                                         PluginSettings.GAME_ID.ToString("x8"));

        /// <summary>Persistent Data directory path.</summary>
        public static readonly string PERSISTENT_DATA_DIRECTORY = IOUtilities.CombinePath(System.IO.Directory.GetCurrentDirectory(),
                                                                                          "mod.io",
                                                                                          PluginSettings.GAME_ID.ToString("x8"));

        // ---------[ IPlatformIO Interface ]---------
        // --- Accessors ---
        /// <summary>Temporary Data directory path.</summary>
        public override string TemporaryDataDirectory
        {
            get { return PlatformIO_Editor.TEMPORARY_DATA_DIRECTORY; }
        }

        /// <summary>Persistent Data directory path.</summary>
        public override string PersistentDataDirectory
        {
            get { return PlatformIO_Editor.PERSISTENT_DATA_DIRECTORY; }
        }
    }
}

#endif // UNITY_EDITOR
