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
        // ---------[ CONSTANTS ]---------
        /// <summary>Root data directory.</summary>
        private static readonly string ROOT_DATA_DIRECTORY = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                                                     "Resources",
                                                                                     "mod.io",
                                                                                     "Editor",
                                                                                     PluginSettings.GAME_ID.ToString("x4"));

        /// <summary>Temporary Data directory path.</summary>
        private static readonly string TEMPORARY_DATA_DIRECTORY = IOUtilities.CombinePath(ROOT_DATA_DIRECTORY, "Temp");

        /// <summary>Persistent Data directory path.</summary>
        private static readonly string PERSISTENT_DATA_DIRECTORY = IOUtilities.CombinePath(ROOT_DATA_DIRECTORY, "Cache");

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
