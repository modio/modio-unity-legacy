using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class UserProfileDisplay : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public delegate void OnClickDelegate(UserProfileDisplay display,
                                             int userId);
        public event OnClickDelegate onClick;

        [Header("UI Components")]
        public Text usernameDisplay;
        public Text lastOnlineDisplay;
        public UserAvatarDisplay avatarDisplay;

        // --- DISPLAY DATA ---
        private int m_userId = -1;
        private List<TextLoadingOverlay> m_loadingOverlays = null;

        // ---------[ INITIALIZATION ]---------
        public void Initialize()
        {
            // text loading
            TextLoadingOverlay[] childLoadingOverlays = this.gameObject.GetComponentsInChildren<TextLoadingOverlay>(true);

            m_loadingOverlays = new List<TextLoadingOverlay>();
            foreach(TextLoadingOverlay loadingOverlay in childLoadingOverlays)
            {
                if(loadingOverlay.textDisplayComponent == usernameDisplay
                   || loadingOverlay.textDisplayComponent == lastOnlineDisplay)
                {
                    m_loadingOverlays.Add(loadingOverlay);
                }
            }

            // avatar
            if(avatarDisplay != null)
            {
                avatarDisplay.Initialize();
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        public void DisplayProfile(UserProfile profile)
        {
            Debug.Assert(profile != null);

            m_userId = profile.id;

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(false);
            }

            if(usernameDisplay != null)
            {
                usernameDisplay.enabled = true;
                usernameDisplay.text = profile.username;
            }
            if(lastOnlineDisplay != null)
            {
                lastOnlineDisplay.enabled = true;
                lastOnlineDisplay.text = ServerTimeStamp.ToLocalDateTime(profile.lastOnline).ToString();
            }
            if(avatarDisplay != null)
            {
                avatarDisplay.DisplayAvatar(profile);
            }
        }

        public void DisplayLoading(int userId = -1)
        {
            m_userId = userId;

            foreach(TextLoadingOverlay loadingOverlay in m_loadingOverlays)
            {
                loadingOverlay.gameObject.SetActive(true);
            }

            if(usernameDisplay != null)
            {
                usernameDisplay.enabled = false;
            }
            if(lastOnlineDisplay != null)
            {
                lastOnlineDisplay.enabled = false;
            }

            if(avatarDisplay != null)
            {
                avatarDisplay.DisplayLoading(userId);
            }
        }

        // ---------[ EVENT HANDLING ]---------
        public void NotifyClicked()
        {
            if(this.onClick != null)
            {
                this.onClick(this, m_userId);
            }
        }
    }
}
