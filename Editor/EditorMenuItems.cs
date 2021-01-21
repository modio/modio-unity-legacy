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
            new MenuItem("Tools/mod.io/Editor Data/", false, 1);
        }

        [MenuItem("Tools/mod.io/Editor Data/Locate...", false)]
        public static void LocateEditorDirectory()
        {
            DataStorage.CreateDirectory(DataStorage.PersistentDataDirectory,
                                        (p,s) => Application.OpenURL(DataStorage.PersistentDataDirectory));
        }

        [MenuItem("Tools/mod.io/Editor Data/Clear all data", false)]
        public static void ClearEditorData()
        {
            Directory.Delete(DataStorage.PersistentDataDirectory);
            Directory.Delete(DataStorage.TemporaryDataDirectory);

            Debug.Log("[mod.io] Editor data store cleared.");
        }

        [MenuItem("Tools/mod.io/Editor Data/Clear user data", false)]
        public static void ClearUserData()
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

        [MenuItem("Tools/mod.io/Editor Data/Clear installation data", false)]
        public static void ClearCachedModData()
        {
            Directory.Delete(ModManager.INSTALLATION_DIRECTORY);
            Debug.Log("[mod.io] Installation data .");
        }

        [MenuItem("Tools/mod.io/Update Color Scheme Applicators", false, 2)]
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
