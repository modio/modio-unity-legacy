#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModIO
{
    public class ModSceneEditorWindow : EditorWindow
    {
        private const int GAME_ID = 0;
        private const string API_KEY = "";

        private Scene currentScene;
        private EditorSceneData sceneData = null;

        private ModInfoEditor infoEditor;

        [MenuItem("ModIO/Mod Scene Info Editor")]
        public static void ShowWindow()
        {
            GetWindow<ModSceneEditorWindow>("Mod Inspector");
        }

        private void OnEnable()
        {
            ModManager.Initialize(GAME_ID, API_KEY);
            infoEditor = new ModInfoEditor();
        }

        private void OnGUI()
        {
            // TODO(@jackson): Make scrollable
            if(currentScene != SceneManager.GetActiveScene())
            {
                Debug.Log("Scene change detected. Loading Mod Data");

                currentScene = SceneManager.GetActiveScene();

                sceneData = Object.FindObjectOfType<EditorSceneData>();
            }

            if(sceneData == null)
            {
                GameObject sd_go = new GameObject("ModIO Scene Data");
                sd_go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

                sceneData = sd_go.AddComponent<EditorSceneData>();
            }

            int modOptionIndex = 0;
            ModInfo[] modList = ModManager.GetMods(GetAllModsFilter.None);
            string[] modOptions = new string[modList.Length + 1];

            modOptions[0] = "[NEW MOD]";

            for(int i = 0; i < modList.Length; ++i)
            {
                ModInfo mod = modList[i];
                modOptions[i+1] = mod.name;

                if(sceneData.modInfo.id == mod.id)
                {
                    modOptionIndex = i + 1;
                }
            }

            modOptionIndex = EditorGUILayout.Popup("Select Mod", modOptionIndex, modOptions, null);

            if(modOptionIndex > 0)
            {
                ModInfo selectedMod = modList[modOptionIndex - 1];

                if(selectedMod.id != sceneData.modInfo.id)
                {
                    Undo.RecordObject(sceneData, "Select Scene ModInfo");
                    sceneData.modInfo = EditableModInfo.FromModInfo(selectedMod);
                }
            }

            SerializedObject serializedSceneData = new SerializedObject(sceneData);
            infoEditor.DisplayAsProperty(serializedSceneData.FindProperty("modInfo"));
            serializedSceneData.ApplyModifiedProperties();
        }
    }
}

#endif