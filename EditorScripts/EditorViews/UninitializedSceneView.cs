#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class UninitializedSceneView : ISceneEditorView
    {
        // ---------[ FIELDS ]---------
        private int modInitializationOptionIndex;
        private ModProfile[] modList;
        private string[] modOptions;

        // ---[ ISceneEditorView Interface ]---
        public virtual string GetViewTitle() { return "New Mod Scene"; }
        public virtual void OnEnable()
        {
            // TODO(@jackson): Filter by editable
            modInitializationOptionIndex = 0;
            modList = ModManager.GetModProfiles(GetAllModsFilter.None);
            modOptions = new string[modList.Length];
            for(int i = 0; i < modList.Length; ++i)
            {
                ModProfile mod = modList[i];
                modOptions[i] = mod.name;
            }
        }
        public virtual void OnDisable() {}
        public virtual bool IsViewDisabled() { return false; }
        
        public void OnGUI(EditorSceneData sceneData)
        {
            // ---[ DISPLAY ]---
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Create New Mod Profile");
            if(GUILayout.Button("Create"))
            {
                EditorApplication.delayCall += () => InitializeSceneForModding(new ModProfile());
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Assign Existing Mod Profile");

            modInitializationOptionIndex = EditorGUILayout.Popup("Select Mod", modInitializationOptionIndex, modOptions, null);
            if(GUILayout.Button("Load"))
            {
                ModProfile profile = modList[modInitializationOptionIndex];
                EditorApplication.delayCall += () => InitializeSceneForModding(profile);
            }
        }

        protected virtual void InitializeSceneForModding(ModProfile profile)
        {
            GameObject sd_go = new GameObject("ModIO Scene Data");
            sd_go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

            EditorSceneData sceneData = sd_go.AddComponent<EditorSceneData>();
            sceneData.modProfileEdits = EditableModProfile.CreateFromProfile(profile);
            sceneData.modfileValues = new EditableModfile();

            Undo.RegisterCreatedObjectUndo(sd_go, "Initialize scene");
        }
    }
}

#endif
