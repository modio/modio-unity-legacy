#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class UninitializedSceneView
    {
        private int modInitializationOptionIndex;

        // ---[ ISceneEditorView Interface ]---
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public void OnGUI()
        {
            // - Select Mod -
            // TODO(@jackson): Filter by editable
            ModInfo[] modList = ModManager.GetMods(GetAllModsFilter.None);
            string[] modOptions = new string[modList.Length + 1];

            modOptions[0] = "[NEW MOD]";

            for(int i = 0; i < modList.Length; ++i)
            {
                ModInfo mod = modList[i];
                modOptions[i+1] = mod.name;
            }

            modInitializationOptionIndex = EditorGUILayout.Popup("Select Mod For Scene", modInitializationOptionIndex, modOptions, null);
                

            if(GUILayout.Button("Initialize Scene"))
            {
                ModInfo modInfo;
                if(modInitializationOptionIndex > 0)
                {
                    modInfo = modList[modInitializationOptionIndex - 1];
                }
                else
                {
                    modInfo = new ModInfo();
                }

                EditorApplication.delayCall += () => InitializeSceneForModding(modInfo);
            }
        }

        protected virtual void InitializeSceneForModding(ModInfo modInfo)
        {
            GameObject sd_go = new GameObject("ModIO Scene Data");
            sd_go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

            EditorSceneData sceneData = sd_go.AddComponent<EditorSceneData>();
            sceneData.modInfo = EditableModInfo.FromModInfo(modInfo);

            sceneData.buildProfile = new ModfileProfile();
            sceneData.buildProfile.modId = modInfo.id;
            sceneData.buildProfile.modfileId = 0;

            Undo.RegisterCreatedObjectUndo(sd_go, "Initialize scene");
        }
    }
}

#endif
