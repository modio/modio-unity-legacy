#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using ModIO.UI;

namespace ModIO.Editor
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
            if(!System.IO.Directory.Exists(CacheClient.cacheDirectory))
            {
                IOUtilities.CreateDirectory(CacheClient.cacheDirectory);
            }

            EditorUtility.RevealInFinder(CacheClient.cacheDirectory);
        }

        [MenuItem("mod.io/Clear Cache/ALL", false, 0)]
        public static void ClearCache()
        {
            if(IOUtilities.DeleteDirectory(CacheClient.cacheDirectory))
            {
                Debug.Log("[mod.io] Cache Cleared.");
            }
        }

        [MenuItem("mod.io/Clear Cache/User Data", false, 1)]
        public static void ClearCachedAuthenticatedUserData()
        {
            UserAuthenticationData.instance = UserAuthenticationData.NONE;
            ModManager.SetSubscribedModIds(new int[0]);
            ModManager.SetEnabledModIds(new int[0]);

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

        [MenuItem("mod.io/Delete Installed Mods", false, 1)]
        public static void RemoveAllInstalledMods()
        {
            if(IOUtilities.DeleteDirectory(ModManager.installationDirectory))
            {
                Debug.Log("[mod.io] Mod Intallation Data removed.");
            }
        }

        [MenuItem("mod.io/Force Update ALL Color Scheme Applicators")]
        public static void ForceColorSchemeUpdate()
        {
            Resources.LoadAll<GraphicColorApplicator>(string.Empty);
            GraphicColorApplicator[] g_applicators = Resources.FindObjectsOfTypeAll<GraphicColorApplicator>();
            foreach(GraphicColorApplicator gca in g_applicators)
            {
                gca.UpdateColorScheme_withUndo();
            }

            // Apply to receivers
            Resources.LoadAll<SelectableColorApplicator>(string.Empty);
            SelectableColorApplicator[] s_applicators = Resources.FindObjectsOfTypeAll<SelectableColorApplicator>();
            foreach(SelectableColorApplicator sca in s_applicators)
            {
                sca.UpdateColorScheme_withUndo();
            }
        }
    }
}
#endif
