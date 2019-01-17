#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public static class EditorMenuItems
    {
        static EditorMenuItems()
        {
            new MenuItem("mod.io/Clear Cache/", false, 1);
        }

        [MenuItem("mod.io/Locate Cache...", false, 0)]
        public static void LocateCache()
        {
            EditorUtility.RevealInFinder(CacheClient.settings.directory);
        }

        [MenuItem("mod.io/Clear Cache/ALL", false, 0)]
        public static void ClearCache()
        {
            if(IOUtilities.DeleteDirectory(CacheClient.settings.directory))
            {
                Debug.Log("[mod.io] Cache Cleared.");
            }

            // NOTE(@jackson): Can throw an exception but I don't care?
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CacheClient.settings.directory));
        }

        [MenuItem("mod.io/Clear Cache/User Data", false, 1)]
        public static void ClearCachedAuthenticatedUserData()
        {
            if(CacheClient.DeleteAuthenticatedUser())
            {
                Debug.Log("[mod.io] Cached User Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/Game Data", false, 1)]
        public static void ClearCachedGameProfile()
        {
            if(IOUtilities.DeleteFile(CacheClient.gameProfileFilePath))
            {
                Debug.Log("[mod.io] Cached Game Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/Mod Data", false, 1)]
        public static void ClearCachedModData()
        {
            if(IOUtilities.DeleteDirectory(CacheClient.settings.directory + "mods/"))
            {
                Debug.Log("[mod.io] Cached Mod Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/User Profiles", false, 1)]
        public static void ClearCachedUserProfiles()
        {
            if(IOUtilities.DeleteDirectory(CacheClient.settings.directory + "users/"))
            {
                Debug.Log("[mod.io] Cached User Profiles Deleted.");
            }
        }
    }
}
#endif
