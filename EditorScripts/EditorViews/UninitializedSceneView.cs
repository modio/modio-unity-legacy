#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class UninitializedSceneView
    {
        private int modInitializationOptionIndex;
        private ModInfo[] modList;
        private string[] modOptions;

        // ---[ ISceneEditorView Interface ]---
        public virtual void OnEnable()
        {
            // TODO(@jackson): Filter by editable
            modInitializationOptionIndex = 0;
            modList = ModManager.GetMods(GetAllModsFilter.None);
            modOptions = new string[modList.Length];
            for(int i = 0; i < modList.Length; ++i)
            {
                ModInfo mod = modList[i];
                modOptions[i] = mod.name;
            }
        }
        public virtual void OnDisable() {}
        
        public void OnGUI()
        {
            // ---[ DISPLAY ]---
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Create New Mod Profile");
            if(GUILayout.Button("Create"))
            {
                EditorApplication.delayCall += () => InitializeSceneForModding(new ModInfo());
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Assign Existing Mod Profile");

            modInitializationOptionIndex = EditorGUILayout.Popup("Select Mod", modInitializationOptionIndex, modOptions, null);
            if(GUILayout.Button("Load"))
            {
                ModInfo modInfo = modList[modInitializationOptionIndex];
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
