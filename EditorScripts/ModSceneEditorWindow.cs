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

        private void UploadModInfo()
        {
            if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before uploading mod data"))
            {
                EditorSceneManager.SaveScene(currentScene);

                isModUploading = true;

                ModManager.SubmitModInfo(sceneData.modInfo,
                                         (mod) => { sceneData.modInfo = EditableModInfo.FromModInfo(mod); isModUploading = false; },
                                         (e) => { isModUploading = false; });
            }
        }

        private void SendModMediaChanges()
        {
            if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before uploading mod data"))
            {
                EditorSceneManager.SaveScene(currentScene);

                bool isAddCompleted = false;
                bool isDeleteCompleted = false;

                isModUploading = true;

                System.Action onAddCompleted = () =>
                {
                    // TODO(@jackson): Update the object with the changes
                    isAddCompleted = true;
                    if(isDeleteCompleted)
                    {
                        APIClient.GetMod(sceneData.modInfo.id,
                                         (mod) => { sceneData.modInfo = EditableModInfo.FromModInfo(mod); isModUploading = false; },
                                         (e) => { isModUploading = false; });
                    }
                };

                System.Action onDeleteCompleted = () =>
                {
                    // TODO(@jackson): Update the object with the changes
                    isDeleteCompleted = true;
                    if(isAddCompleted)
                    {
                        APIClient.GetMod(sceneData.modInfo.id,
                                         (mod) => { sceneData.modInfo = EditableModInfo.FromModInfo(mod); isModUploading = false; },
                                         (e) => { isModUploading = false; });
                    }
                };

                ModManager.AddModMedia(sceneData.modInfo.GetAddedMedia(),
                                       (m) => { onAddCompleted(); },
                                       (e) => { onAddCompleted(); });
                ModManager.DeleteModMedia(sceneData.modInfo.GetRemovedMedia(),
                                          (m) => { onDeleteCompleted(); },
                                          (e) => { onDeleteCompleted(); });
            }
        }

        private void UploadModBinary()
        {
            if(EditorSceneManager.EnsureUntitledSceneHasBeenSaved("The scene needs to be saved before publishing online"))
            {
                EditorSceneManager.SaveScene(currentScene);

                isModUploading = true;

                System.Action<Modfile> onUploadSucceeded = (mf) =>
                {
                    Debug.Log("Upload succeeded!");
                    isModUploading = false;
                };

                ModManager.UploadModBinary_Unzipped(sceneData.buildLocation,
                                                    sceneData.buildProfile,
                                                    true,
                                                    onUploadSucceeded,
                                                    (e) => isModUploading = false);
            }
        }

        // ---------[ GUI DISPLAY ]---------
        protected virtual void OnGUI()
        {
            bool isPlaying = Application.isPlaying;
            bool doUploadInfo = false;
            bool doUploadMedia = false;
            bool doUploadBinary = false;

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
                SerializedObject serializedSceneData = new SerializedObject(sceneData);


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

                    tabbedViews[activeTabbedViewIndex].OnGUI(serializedSceneData);

                    // switch(currentView)
                    // {
                    //     case ModPanelView.Profile:
                    //         ModProfileView.ModProfilePanel();

                    //         doUploadInfo = GUILayout.Button("Save To Server");
                    //     break;

                    //     case ModPanelView.Media:
                    //         ModMediaView.ModMediaPanel(serializedSceneData.FindProperty("modInfo"));

                    //         doUploadMedia = GUILayout.Button("Update Mod Media");
                    //     break;

                    //     case ModPanelView.ModfileManagement:
                    //         ModfileManagementView.ModfileManagementPanel();

                    //         doUploadBinary = GUILayout.Button("Publish Build to Mod.IO");
                    //     break;
                    // }
                
                    EditorGUILayout.EndScrollView();

                    serializedSceneData.ApplyModifiedProperties();
                }
            }

            wasPlaying = isPlaying;

            // - Final Actions -
            if(doUploadInfo)
            {
                EditorApplication.delayCall += UploadModInfo;
            }
            if(doUploadMedia)
            {
                EditorApplication.delayCall += SendModMediaChanges;
            }
            if(doUploadBinary)
            {
                EditorApplication.delayCall += UploadModBinary;
            }
        }
    }
}

#endif