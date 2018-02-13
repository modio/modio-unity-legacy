#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModIO
{
    public class ModInfoEditorWindow : EditorWindow
    {
        private const int GAME_ID = 0;
        private const string API_KEY = "";

        private Scene currentScene;
        private EditorSceneData sceneData = null;

        private EditableModInfo modInfo = null;
        private List<ModTag> modTags = null;

        [MenuItem("ModIO/Mod Inspector")]
        public static void ShowWindow()
        {
            GetWindow<ModInfoEditorWindow>("Mod Inspector");
        }

        public void LoadModInfo(ModInfo mod)
        {
            modInfo = EditableModInfo.FromModInfo(mod);
            // TODO(@jackson): Load ModTags
        }

        private void OnEnable()
        {
            ModManager.Initialize(GAME_ID, API_KEY);

            // EditorSceneManager.newSceneCreated; // This event is called after a new Scene has been created.
            // EditorSceneManager.sceneClosed; // This event is called after a Scene has been closed in the editor.
            // EditorSceneManager.sceneClosing; // This event is called before closing an open Scene after you have requested that the Scene is closed.
            // EditorSceneManager.sceneOpened; // This event is called after a Scene has been opened in the editor.
            // EditorSceneManager.sceneOpening; // This event is called before opening an existing Scene.
            // EditorSceneManager.sceneSaved += SaveModInfoToSceneAsset;
            // EditorSceneManager.sceneSaving; // This event is called before a Scene is saved disk after you have requested the Scene to be saved.
        }

        private void OnDisable()
        {
            // EditorSceneManager.sceneSaved -= SaveModInfoToSceneAsset;
        }

        private void AttemptLoadModInfo()
        {
            // Find ModIOSceneData
            // AssetDatabase.LoadAssetAtPath();
            // Create/Load ModInfo
        }

        private void OnGUI()
        {
            if(currentScene != SceneManager.GetActiveScene())
            {
                Debug.Log("Scene change detected. Loading Mod Data");

                currentScene = SceneManager.GetActiveScene();

                sceneData = Object.FindObjectOfType<EditorSceneData>();

                if(sceneData == null)
                {
                    GameObject sd_go = new GameObject("ModIO Scene Data");
                    sd_go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

                    sceneData = sd_go.AddComponent<EditorSceneData>();
                }
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

            EditorGUILayout.TextField("Mod Name", sceneData.modInfo.name, null);
        }
    }
}

#endif