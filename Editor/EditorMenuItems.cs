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

        [MenuItem("Tools/mod.io/Debugging/Clear All Cached Data", false)]
        public static void ClearCache()
        {
            DataStorage.DeleteDirectory(CacheClient.cacheDirectory, (success, path) =>
            {
                if(success)
                {
                    Debug.Log("[mod.io] Cache Cleared.");
                }
                else
                {
                    Debug.Log("[mod.io] Failed to clear cache.");
                }
            });
        }

        [MenuItem("Tools/mod.io/Debugging/Clear All User Data", false)]
        public static void ClearAllUserData()
        {
            UserDataStorage.ClearAllDataCallback onClear = (success) =>
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
            };

            if(!UserDataStorage.isInitialized)
            {
                UserDataStorage.InitializeForUser(null, () => UserDataStorage.ClearAllData(onClear));
            }
            else
            {
                UserDataStorage.ClearAllData(onClear);
            }
        }
        [MenuItem("Tools/mod.io/Debugging/Clear Game Data", false)]
        public static void ClearCachedGameProfile()
        {
            DataStorage.DeleteFile(CacheClient.gameProfileFilePath, (s,p) =>
            {
                if(s)
                {
                    Debug.Log("[mod.io] Cached Game Data Deleted.");
                }
                else
                {
                    Debug.Log("[mod.io] Failed to delete Cached Game Data.");
                }
            });
        }
        [MenuItem("Tools/mod.io/Debugging/Clear Mod Data", false)]
        public static void ClearCachedModData()
        {
            string modDir = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");

            DataStorage.DeleteDirectory(modDir, (success, path) =>
            {
                if(success)
                {
                    Debug.Log("[mod.io] Cached Mod Data Deleted.");
                }
                else
                {
                    Debug.Log("[mod.io] Failed to clear cached Mod Data.");
                }
            });
        }

        [MenuItem("Tools/mod.io/Debugging/Delete Installed Mods", false)]
        public static void RemoveAllInstalledMods()
        {
            DataStorage.DeleteDirectory(ModManager.installationDirectory, (success, path) =>
            {
                if(success)
                {
                    Debug.Log("[mod.io] Mod Installation Data removed.");
                }
                else
                {
                    Debug.Log("[mod.io] Failed to removed installed mods.");
                }
            });
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
