#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality and adds AssetDatabase refreshes.</summary>
    public class UserDataIO_Editor : UserDataIOBase, IUserDataIO<object>
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>User Data directory path.</summary>
        private static readonly string USER_DATA_DIRECTORY = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                                                     "Resources",
                                                                                     "mod.io",
                                                                                     "Editor",
                                                                                     PluginSettings.GAME_ID.ToString("x4"),
                                                                                     "User");

        // ---------[ IUserDataIO Interface ]---------
        // --- Accessors ---
        /// <summary>The directory for the active user's data.</summary>
        protected string m_activeUserDirectory = null;

        /// <summary>Active User Data directory.</summary>
        public override string ActiveUserDirectory
        {
            get
            {
                if(this.m_activeUserDirectory == null)
                {
                    this.m_activeUserDirectory = this.GenerateActiveUserDirectory(null);
                }
                return this.m_activeUserDirectory;
            }
        }

        // --- Initialization ---
        /// <summary>Determines the user directory for a given user id.</summary>
        protected virtual string GenerateActiveUserDirectory(string platformUserId)
        {
            string dir = UserDataIO_Editor.USER_DATA_DIRECTORY;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                dir = IOUtilities.CombinePath(UserDataIO_Editor.USER_DATA_DIRECTORY, folderName);
            }

            return dir;
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public override void SetActiveUser(string platformUserId, UserDataIOCallbacks.SetActiveUserCallback<string> callback)
        {
            this.m_activeUserDirectory = this.GenerateActiveUserDirectory(platformUserId);

            bool success = SystemIOWrapper.CreateDirectory(this.ActiveUserDirectory);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public override void SetActiveUser(int platformUserId, UserDataIOCallbacks.SetActiveUserCallback<int> callback)
        {
            this.m_activeUserDirectory = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

            bool success = SystemIOWrapper.CreateDirectory(this.ActiveUserDirectory);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public void SetActiveUser(object platformUserId, UserDataIOCallbacks.SetActiveUserCallback<object> callback)
        {
            if(platformUserId != null)
            {
                this.m_activeUserDirectory = this.GenerateActiveUserDirectory(platformUserId.ToString());
            }
            else
            {
                this.m_activeUserDirectory = this.GenerateActiveUserDirectory(null);
            }

            bool success = SystemIOWrapper.CreateDirectory(this.ActiveUserDirectory);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }
    }
}

#endif // UNITY_EDITOR
