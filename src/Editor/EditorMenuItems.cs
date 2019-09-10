#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using ModIO.UI;

namespace ModIO.EditorCode
{
    public static class EditorMenuItems
    {
        static EditorMenuItems()
        {
            new MenuItem("Tools/mod.io/Edit Settings", false, 0);
            new MenuItem("Tools/mod.io/Debugging/", false, 1);
            new MenuItem("Tools/mod.io/Tools/", false, 1);
        }

        [MenuItem("Tools/mod.io/Debugging/Locate Cache...", false)]
        public static void LocateCache()
        {
            if(!System.IO.Directory.Exists(CacheClient.cacheDirectory))
            {
                IOUtilities.CreateDirectory(CacheClient.cacheDirectory);
            }

            EditorUtility.RevealInFinder(CacheClient.cacheDirectory);
        }

        [MenuItem("Tools/mod.io/Debugging/Clear All Cached Data", false)]
        public static void ClearCache()
        {
            if(IOUtilities.DeleteDirectory(CacheClient.cacheDirectory))
            {
                Debug.Log("[mod.io] Cache Cleared.");
            }
        }

        [MenuItem("Tools/mod.io/Debugging/Clear User Data", false)]
        public static void ClearCachedAuthenticatedUserData()
        {
            UserAuthenticationData.instance = UserAuthenticationData.NONE;
            ModManager.SetSubscribedModIds(new int[0]);
            ModManager.SetEnabledModIds(new int[0]);

            Debug.Log("[mod.io] Cached User Data Deleted.");
        }
        [MenuItem("Tools/mod.io/Debugging/Clear Game Data", false)]
        public static void ClearCachedGameProfile()
        {
            if(IOUtilities.DeleteFile(CacheClient.gameProfileFilePath))
            {
                Debug.Log("[mod.io] Cached Game Data Deleted.");
            }
        }
        [MenuItem("Tools/mod.io/Debugging/Clear Mod Data", false)]
        public static void ClearCachedModData()
        {
            string modDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");
            if(IOUtilities.DeleteDirectory(modDir))
            {
                Debug.Log("[mod.io] Cached Mod Data Deleted.");
            }
        }
        [MenuItem("Tools/mod.io/Debugging/Clear User Profiles", false)]
        public static void ClearCachedUserProfiles()
        {
            string usersDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "users");
            if(IOUtilities.DeleteDirectory(usersDir))
            {
                Debug.Log("[mod.io] Cached User Profiles Deleted.");
            }
        }

        [MenuItem("Tools/mod.io/Debugging/Delete Installed Mods", false)]
        public static void RemoveAllInstalledMods()
        {
            if(IOUtilities.DeleteDirectory(ModManager.installationDirectory))
            {
                Debug.Log("[mod.io] Mod Intallation Data removed.");
            }
        }

        [MenuItem("Tools/mod.io/Force Update ALL Color Scheme Applicators", false)]
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
