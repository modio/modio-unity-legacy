using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class DebuggingUI : MonoBehaviour
{
    public Text cacheDirectoryDisplay;
    public Text messageDisplay;

    public void OnEnable()
    {
        if(cacheDirectoryDisplay != null)
        {
            cacheDirectoryDisplay.text = CacheClient.cacheDirectory;
        }
    }

    public void ClearCache()
    {
        string cacheDir = CacheClient.cacheDirectory;
        if(IOUtilities.DeleteDirectory(cacheDir))
        {
            string message = "[mod.io] Cache Cleared.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }

            // NOTE(@jackson): Can throw an exception but I don't care?
            System.IO.Directory.CreateDirectory(cacheDir);
        }
    }

    public void ClearCachedAuthenticatedUserData()
    {
        UserAuthenticationData.instance = UserAuthenticationData.NONE;

        string message = "[mod.io] Cached User Data Deleted.";
        Debug.Log(message);
        if(messageDisplay != null)
        {
            messageDisplay.text = message;
        }
    }
    public void ClearCachedGameProfile()
    {
        if(IOUtilities.DeleteFile(CacheClient.gameProfileFilePath))
        {
            string message = "[mod.io] Cached Game Data Deleted.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }
        }
    }
    public void ClearCachedModData()
    {
        string modsDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");
        if(IOUtilities.DeleteDirectory(modsDir))
        {
            string message = "[mod.io] Cached Mod Data Deleted.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }
        }
    }
    public void ClearCachedUserProfiles()
    {
        string usersDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "users");
        if(IOUtilities.DeleteDirectory(usersDir))
        {
            string message = "[mod.io] Cached User Profiles Deleted.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }
        }
    }
}
