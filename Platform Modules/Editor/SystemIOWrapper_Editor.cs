#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality and adds AssetDatabase refreshes.</summary>
    public class SystemIOWrapper_Editor : SystemIOWrapper
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

        /// <summary>User Data directory path.</summary>
        private static readonly string USER_DATA_DIRECTORY = IOUtilities.CombinePath(ROOT_DATA_DIRECTORY, "User");

        // ---------[ IPlatformIO Interface ]---------
        // --- Accessors ---
        /// <summary>Temporary Data directory path.</summary>
        public override string TemporaryDataDirectory
        {
            get { return SystemIOWrapper_Editor.TEMPORARY_DATA_DIRECTORY; }
        }

        /// <summary>Persistent Data directory path.</summary>
        public override string PersistentDataDirectory
        {
            get { return SystemIOWrapper_Editor.PERSISTENT_DATA_DIRECTORY; }
        }

        // ---------[ IUserDataIO Interface ]---------
        // --- Initialization ---
        /// <summary>Determines the user directory for a given user id.</summary>
        protected override string GenerateActiveUserDirectory(string platformUserId)
        {
            string dir = SystemIOWrapper_Editor.USER_DATA_DIRECTORY;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                dir = IOUtilities.CombinePath(SystemIOWrapper_Editor.USER_DATA_DIRECTORY, folderName);
            }

            return dir;
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public override void SetActiveUser(string platformUserId, UserDataIOCallbacks.SetActiveUserCallback<string> callback)
        {
            base.SetActiveUser(platformUserId, callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.m_activeUserDirectory)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public override void SetActiveUser(int platformUserId, UserDataIOCallbacks.SetActiveUserCallback<int> callback)
        {
            base.SetActiveUser(platformUserId, callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.m_activeUserDirectory)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }
        }

        // ---------[ Core Functionality ]---------
        // --- File I/O ---
        /// <summary>Writes a file.</summary>
        public override bool WriteFile(string path, byte[] data)
        {
            bool success = base.WriteFile(path, data);

            if(success
               && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public override bool DeleteFile(string path)
        {
            bool success = base.DeleteFile(path);

            if(success
               && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        /// <summary>Moves a file.</summary>
        public override bool MoveFile(string source, string destination)
        {
            bool success = base.MoveFile(source, destination);
            bool isInDatabase = (SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(source)
                                 || SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(destination));

            if(success
               && isInDatabase
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public override bool CreateDirectory(string path)
        {
            bool success = base.CreateDirectory(path);

            if(success
               && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        /// <summary>Deletes a directory.</summary>
        public override bool DeleteDirectory(string path)
        {
            bool success = base.DeleteDirectory(path);

            if(success
               && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        /// <summary>Moves a directory.</summary>
        public override bool MoveDirectory(string source, string destination)
        {
            bool success = base.MoveDirectory(source, destination);
            bool isInDatabase = (SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(source)
                                 || SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(destination));

            if(success
               && isInDatabase
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        // ---------[ UTIL ]---------
        /// <summary>Determines whether an AssetDatabase refresh is applicable.</summary>
        public static bool IsPathWithinEditorAssetDatabase(string path)
        {
            return path.StartsWith(Application.dataPath);
        }
    }
}

#endif // UNITY_EDITOR
