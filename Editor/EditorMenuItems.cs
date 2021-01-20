#if UNITY_EDITOR
using System.Collections.Generic;
using File = System.IO.File;
using Directory = System.IO.Directory;

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

        [MenuItem("Tools/mod.io/Debugging/Clear All Cached Data", false)]
        public static void ClearCache()
        {
            Directory.Delete(DataStorage.PersistentDataDirectory);
            Debug.Log("[mod.io] Cache Cleared.");
        }

        [MenuItem("Tools/mod.io/Debugging/Clear All User Data", false)]
        public static void ClearAllUserData()
        {
            UserDataStorage.ClearActiveUserData((success) =>
            {
                LocalUser.instance = new LocalUser();
                LocalUser.isLoaded = true;

                if(success)
                {
                    Debug.Log("[mod.io] User Data Cleared.");
                }
                else
                {
                    Debug.Log("[mod.io] Failed to clear User Data.");
                }
            });
        }

        [MenuItem("Tools/mod.io/Debugging/Clear Game Data", false)]
        public static void ClearCachedGameProfile()
        {
            File.Delete(CacheClient.gameProfileFilePath);
            Debug.Log("[mod.io] Cached Game Data Deleted.");
        }

        [MenuItem("Tools/mod.io/Debugging/Clear Mod Data", false)]
        public static void ClearCachedModData()
        {
            string modDir = IOUtilities.CombinePath(DataStorage.PersistentDataDirectory, "mods");

            Directory.Delete(modDir);
            Debug.Log("[mod.io] Cached Mod Data Deleted.");
        }

        [MenuItem("Tools/mod.io/Debugging/Delete Installed Mods", false)]
        public static void RemoveAllInstalledMods()
        {
            Directory.Delete(ModManager.INSTALLATION_DIRECTORY);
            Debug.Log("[mod.io] Mod Installation Data removed.");
        }

        [MenuItem("Tools/mod.io/Update ALL Color Scheme Applicators", false)]
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
