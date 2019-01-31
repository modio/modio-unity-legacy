#if UNITY_EDITOR
using System.Collections.Generic;
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
            EditorUtility.RevealInFinder(CacheClient.cacheDirectory);
        }

        [MenuItem("mod.io/Clear Cache/ALL", false, 0)]
        public static void ClearCache()
        {
            if(IOUtilities.DeleteDirectory(CacheClient.cacheDirectory))
            {
                Debug.Log("[mod.io] Cache Cleared.");

                // NOTE(@jackson): Can throw an exception but I don't care?
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CacheClient.cacheDirectory));
            }
        }

        [MenuItem("mod.io/Clear Cache/User Data", false, 1)]
        public static void ClearCachedAuthenticatedUserData()
        {
            ModManager.activeUser = UserData.NONE;
            APIClient.userAuthorizationToken = null;

            PluginSettings settings = PluginSettings.LoadDefaults();
            settings.authenticationToken = string.Empty;
            PluginSettings.SaveDefaults(settings);

            Debug.Log("[mod.io] Cached User Data Deleted.");
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
            string modDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");
            if(IOUtilities.DeleteDirectory(modDir))
            {
                Debug.Log("[mod.io] Cached Mod Data Deleted.");
            }
        }
        [MenuItem("mod.io/Clear Cache/User Profiles", false, 1)]
        public static void ClearCachedUserProfiles()
        {
            string usersDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "users");
            if(IOUtilities.DeleteDirectory(usersDir))
            {
                Debug.Log("[mod.io] Cached User Profiles Deleted.");
            }
        }

        [MenuItem("mod.io/Remove Installed Mod Data", false, 1)]
        public static void RemoveAllInstalledMods()
        {
            if(IOUtilities.DeleteDirectory(ModManager.installDirectory))
            {
                Debug.Log("[mod.io] Mod Intallation Data removed.");
            }
        }
    }
}
#endif
