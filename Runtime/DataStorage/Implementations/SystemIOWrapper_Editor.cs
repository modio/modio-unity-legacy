#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality and adds AssetDatabase refreshes.</summary>
    public class SystemIOWrapper_Editor : SystemIOWrapper
    {
        // --- Initialization ---
        public SystemIOWrapper_Editor()
            : base(PluginSettings.data.installationDirectoryEditor,
                   PluginSettings.data.cacheDirectoryEditor,
                   PluginSettings.data.userDirectoryEditor)
        {
        }

        /// <summary>Determines whether an AssetDatabase refresh is applicable.</summary>
        public static bool IsPathWithinEditorAssetDatabase(string path)
        {
            return path.StartsWith(Application.dataPath);
        }

        // --- File I/O ---
        /// <summary>Writes a file.</summary>
        public override bool WriteFile(string path, byte[] data)
        {
            bool success = base.WriteFile(path, data);

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
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

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
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
            bool isInDatabase =
                (SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(source)
                 || SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(destination));

            if(success && isInDatabase && !Application.isPlaying)
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

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
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

            if(success && SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(path)
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
            bool isInDatabase =
                (SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(source)
                 || SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(destination));

            if(success && isInDatabase && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }

            return success;
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public override void SetActiveUser(
            string platformUserId, UserDataIOCallbacks.SetActiveUserCallback<string> callback)
        {
            base.SetActiveUser(platformUserId, callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.UserDirectory)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public override void SetActiveUser(int platformUserId,
                                           UserDataIOCallbacks.SetActiveUserCallback<int> callback)
        {
            base.SetActiveUser(platformUserId, callback);

            if(SystemIOWrapper_Editor.IsPathWithinEditorAssetDatabase(this.UserDirectory)
               && !Application.isPlaying)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}

#endif // UNITY_EDITOR
