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
            cacheDirectoryDisplay.text = CacheClient.GetCacheDirectory();
        }
    }

    public void ClearCache()
    {
        if(CacheClient.DeleteDirectory(CacheClient.GetCacheDirectory()))
        {
            string message = "[mod.io] Cache Cleared.";
            Debug.Log(message);
            if(messageDisplay != null)
            {
                messageDisplay.text = message;
            }
        }

        CacheClient.TrySetCacheDirectory(CacheClient.GetCacheDirectory());
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
        if(CacheClient.DeleteDirectory(CacheClient.GetCacheDirectory() + "mods/"))
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
        if(CacheClient.DeleteDirectory(CacheClient.GetCacheDirectory() + "users/"))
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
