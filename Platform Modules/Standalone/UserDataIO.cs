using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality and adds AssetDatabase refreshes.</summary>
    public class UserDataIO : UserDataIOBase, IUserDataIO<string>, IUserDataIO<int>
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>User Data directory path.</summary>
        private static readonly string USER_DATA_DIRECTORY = IOUtilities.CombinePath(UnityEngine.Application.persistentDataPath,
                                                                                     "modio_" + PluginSettings.GAME_ID.ToString("x8"));

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
            string dir = UserDataIO.USER_DATA_DIRECTORY;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                dir = IOUtilities.CombinePath(UserDataIO.USER_DATA_DIRECTORY, folderName);
            }

            return dir;
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(string platformUserId, UserDataIOCallbacks.SetActiveUserCallback<string> callback)
        {
            this.m_activeUserDirectory = this.GenerateActiveUserDirectory(platformUserId);

            bool success = SystemIOWrapper.CreateDirectory(this.ActiveUserDirectory);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(int platformUserId, UserDataIOCallbacks.SetActiveUserCallback<int> callback)
        {
            this.m_activeUserDirectory = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

            bool success = SystemIOWrapper.CreateDirectory(this.ActiveUserDirectory);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }
    }
}
