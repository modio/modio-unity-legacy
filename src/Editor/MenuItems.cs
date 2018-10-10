#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public static class MenuItems
    {
        static MenuItems()
        {
            new MenuItem("mod.io/Clear Cache/", false, 1);
        }

        [MenuItem("mod.io/Locate Cache...", false, 0)]
        public static void LocateCache()
        {
            EditorUtility.RevealInFinder(CacheClient.GetCacheDirectory());
        }

        [MenuItem("mod.io/Clear Cache/ALL", false, 0)]
        public static void ClearCache()
        {
            if(CacheClient.DeleteDirectory(CacheClient.GetCacheDirectory()))
            {
                Debug.Log("[mod.io] Cache Cleared.");
            }

            CacheClient.TrySetCacheDirectory(CacheClient.GetCacheDirectory());
        }

        [MenuItem("mod.io/Clear Cache/Authentication Data", false, 1)]
        public static void ClearCachedAuthentaticationData()
        {
            if(CacheClient.DeleteAuthenticatedUser())
            {
                Debug.Log("[mod.io] Cached Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/Game Profile", false, 1)]
        public static void ClearCachedGameProfile()
        {
            if(CacheClient.DeleteFile(CacheClient.gameProfileFilePath))
            {
                Debug.Log("[mod.io] Cached Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/Mod Data", false, 1)]
        public static void ClearCachedModData()
        {
            if(CacheClient.DeleteDirectory(CacheClient.GetCacheDirectory() + "mods/"))
            {
                Debug.Log("[mod.io] Cached Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/User Data", false, 1)]
        public static void ClearCachedUserData()
        {
            if(CacheClient.DeleteDirectory(CacheClient.GetCacheDirectory() + "users/"))
            {
                Debug.Log("[mod.io] Cached Data Deleted.");
            }
        }
    }
}
#endif
