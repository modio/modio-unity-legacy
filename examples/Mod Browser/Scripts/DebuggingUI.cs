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
            cacheDirectoryDisplay.text = CacheClient.settings.directory;
        }
    }

    public void ClearCache()
    {
        string cacheDir = CacheClient.settings.directory;
        if(IOUtilities.DeleteDirectory(cacheDir))
        {
            string message = "[mod.io] Cache Cleared.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }
        }

        // NOTE(@jackson): Can throw an exception but I don't care?
        System.IO.Directory.CreateDirectory(cacheDir);
    }

    public void ClearCachedAuthenticatedUserData()
    {
        if(CacheClient.DeleteAuthenticatedUser())
        {
            string message = "[mod.io] Cached User Data Deleted.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }
        }
    }
    public void ClearCachedGameProfile()
    {
        if(CacheClient.DeleteFile(CacheClient.gameProfileFilePath))
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
        if(IOUtilities.DeleteDirectory(CacheClient.settings.directory + "mods/"))
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
        if(IOUtilities.DeleteDirectory(CacheClient.settings.directory + "users/"))
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
