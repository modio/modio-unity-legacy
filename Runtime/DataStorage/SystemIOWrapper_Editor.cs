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
        /// <summary>Determines whether an AssetDatabase refresh is applicable.</summary>
        public static bool IsPathWithinEditorAssetDatabase(string path)
        {
            return path.StartsWith(Application.dataPath);
        }

        // ---------[ IPlatformIO Interface ]---------
        // --- File I/O ---
        /// <summary>Writes a file.</summary>
        public override bool WriteFile(string path, byte[] data)
        {
            bool success = base.WriteFile(path, data);

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path))
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

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path))
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

            if(success && isInDatabase)
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

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path))
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        /// <summary>Deletes a directory.</summary>
        public override bool DeleteDirectory(string path)
        {
            bool success = base.DeleteDirectory(path);

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path))
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

            if(success && isInDatabase)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        // ---------[ IPlatformUserDataIO Interface ]---------
        /// <summary>Defines the base directory for the user-specific data.</summary>
        public static readonly string EDITOR_RESOURCES_FOLDER = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                                                        "Editor Default Resources",
                                                                                        "mod.io");

        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(string platformUserId, DataStorageCallbacks.SetActiveUserCallback<string> callback)
        {
            base.SetActiveUser(platformUserId, callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.userDir))
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(int platformUserId, DataStorageCallbacks.SetActiveUserCallback<int> callback)
        {
            base.SetActiveUser(platformUserId, callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.userDir))
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Determines the user directory for a given user id..</summary>
        protected virtual string GenerateActiveUserDirectory(string platformUserId)
        {
            string userDir = SystemIOWrapper_Editor.EDITOR_RESOURCES_FOLDER;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                userDir = IOUtilities.CombinePath(SystemIOWrapper.USER_DIR_ROOT, folderName);
            }

            return userDir;
        }

        /// <summary>Deletes all of the active user's data.</summary>
        public virtual void ClearActiveUserData(DataStorageCallbacks.ClearActiveUserDataCallback callback)
        {
            base.ClearActiveUserData(callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.userDir))
            {
                AssetDatabase.Refresh();
            }
        }
    }
}

#endif // UNITY_EDITOR
