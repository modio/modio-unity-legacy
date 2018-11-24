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

    [Header("Settings")]
    public GameObject       textLoadingPrefab;

    [Header("UI Components")]
    public Text usernameDisplay;
    public Text lastOnlineDisplay;
    public UserAvatarDisplay avatarDisplay;

    [Header("Display Data")]
    [SerializeField] private int m_userId;

    // --- RUNTIME DATA ---
    private List<GameObject> m_loadingInstances = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        if(textLoadingPrefab != null)
        {
            m_loadingInstances = new List<GameObject>();

            if(usernameDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(usernameDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
            if(lastOnlineDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(lastOnlineDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
        }

        if(avatarDisplay != null)
        {
            avatarDisplay.Initialize();
        }
    }

    private GameObject InstantiateTextLoadingPrefab(RectTransform displayObjectTransform)
    {
        RectTransform parentRT = displayObjectTransform.parent as RectTransform;
        GameObject loadingGO = GameObject.Instantiate(textLoadingPrefab,
                                                      new Vector3(),
                                                      Quaternion.identity,
                                                      parentRT);

        RectTransform loadingRT = loadingGO.transform as RectTransform;
        loadingRT.anchorMin = displayObjectTransform.anchorMin;
        loadingRT.anchorMax = displayObjectTransform.anchorMax;
        loadingRT.offsetMin = displayObjectTransform.offsetMin;
        loadingRT.offsetMax = displayObjectTransform.offsetMax;

        return loadingGO;
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayProfile(UserProfile profile)
    {
        Debug.Assert(profile != null);

        if(m_loadingInstances != null)
        {
            foreach(GameObject loadingGO in m_loadingInstances)
            {
                loadingGO.SetActive(false);
            }
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
            avatarDisplay.DisplayProfile(profile);
        }
    }

    public void DisplayLoading()
    {
        if(usernameDisplay != null)
        {
            usernameDisplay.enabled = false;
        }
        if(lastOnlineDisplay != null)
        {
            lastOnlineDisplay.enabled = false;
        }
        if(m_loadingInstances != null)
        {
            foreach(GameObject loadingGO in m_loadingInstances)
            {
                loadingGO.SetActive(true);
            }
        }

        if(avatarDisplay != null)
        {
            avatarDisplay.DisplayLoading();
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
