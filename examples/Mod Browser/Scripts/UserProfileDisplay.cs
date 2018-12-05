using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

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
    private List<TextLoadingDisplay> m_loadingDisplays = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        // text loading
        TextLoadingDisplay[] childLoadingDisplays = this.gameObject.GetComponentsInChildren<TextLoadingDisplay>(true);

        m_loadingDisplays = new List<TextLoadingDisplay>();
        foreach(TextLoadingDisplay loadingDisplay in childLoadingDisplays)
        {
            if(loadingDisplay.valueDisplayComponent == usernameDisplay
               || loadingDisplay.valueDisplayComponent == lastOnlineDisplay)
            {
                m_loadingDisplays.Add(loadingDisplay);
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

        foreach(TextLoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(false);
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

        foreach(TextLoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(true);
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
