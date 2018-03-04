#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModIO
{
    // TODO(@jackson): Needs beauty-pass
    // TODO(@jackson): Force repaint on Callbacks
    // TODO(@jackson): Implement client-side error-checking in submission
    // TODO(@jackson): Check if undos are necessary
    // TODO(@jackson): Check for scene change between callbacks
    public abstract class ModSceneEditorWindow : EditorWindow
    {
        private enum ModPanelView
        {
            Profile,
            Media,
            ModfileManagement,
        }

        // ------[ WINDOW FIELDS ]---------
        private Scene currentScene;
        private EditorSceneData sceneData;
        private bool wasPlaying;
        private bool isModUploading;
        private int activeTabbedViewIndex;

        private Vector2 scrollPos;


        // --- Scene Initialization ---
        private int modInitializationOptionIndex;

        private void OnEnable()
        {
            ModManager.Initialize();

            currentScene = new Scene();
            wasPlaying = Application.isPlaying;
            sceneData = null;
            activeTabbedViewIndex = 0;

            GetEditorHeader().OnEnable();
        }

        private void OnDisable()
        {
            GetEditorHeader().OnDisable();
        }

        protected virtual void OnSceneChange()
        {
            // - Initialize Scene Variables -
            currentScene = SceneManager.GetActiveScene();
            sceneData = Object.FindObjectOfType<EditorSceneData>();
            
            activeTabbedViewIndex = 0;
            modInitializationOptionIndex = 0;
            scrollPos = Vector2.zero;
            isModUploading = false;
        }

        protected abstract ISceneEditorHeader GetEditorHeader();

        protected abstract ISceneEditorView[] GetTabbedViews();

        private void DisplayUninitializedSceneOptions()
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

                InitializeSceneForModding(modInfo);
            }
        }

        private void InitializeSceneForModding(ModInfo modInfo)
        {
            GameObject sd_go = new GameObject("ModIO Scene Data");
            sd_go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

            sceneData = sd_go.AddComponent<EditorSceneData>();
            sceneData.modInfo = EditableModInfo.FromModInfo(modInfo);

            sceneData.buildProfile = new ModfileProfile();
            sceneData.buildProfile.modId = modInfo.id;
            sceneData.buildProfile.modfileId = 0;

            Undo.RegisterCreatedObjectUndo(sd_go, "Initialize scene");
        }

        // ---------[ GUI DISPLAY ]---------
        protected virtual void OnGUI()
        {
            bool isPlaying = Application.isPlaying;

            // - Update Data -
            if(currentScene != SceneManager.GetActiveScene()
               || (isPlaying != wasPlaying))
            {
                OnSceneChange();
            }

            // ---[ Display ]---
            GetEditorHeader().OnGUI();

            EditorGUILayout.Space();

            // ---[ Main Panel ]---
            if(sceneData == null)
            {
                DisplayUninitializedSceneOptions();
            }
            else
            {
                int prevViewIndex = activeTabbedViewIndex;
                ISceneEditorView[] tabbedViews = this.GetTabbedViews();

                EditorGUILayout.BeginHorizontal();
                    for(int i = 0;
                        i < tabbedViews.Length;
                        ++i)
                    {
                        if(GUILayout.Button(tabbedViews[i].GetViewHeader()))
                        {
                            activeTabbedViewIndex = i;
                        }
                    }
                EditorGUILayout.EndHorizontal();

                if(prevViewIndex != activeTabbedViewIndex)
                {
                    scrollPos = Vector2.zero;

                    tabbedViews[prevViewIndex].OnDisable();
                    tabbedViews[activeTabbedViewIndex].OnEnable();
                }

                using (new EditorGUI.DisabledScope(isModUploading || Application.isPlaying))
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                    tabbedViews[activeTabbedViewIndex].OnGUI(sceneData);
                
                    EditorGUILayout.EndScrollView();
                }
            }

            wasPlaying = isPlaying;
        }
    }
}

#endif