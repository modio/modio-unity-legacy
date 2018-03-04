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
        private ModPanelView currentView;

        private Vector2 scrollPos;

        // --- Registration Variables ---
        private bool inputEmail;
        private string emailAddressInput;
        private string securityCodeInput;
        private bool isRequestSending;

        // --- Scene Initialization ---
        private int modInitializationOptionIndex;

        // ---- View Vars ---
        private bool isTagsExpanded = false;

        private void OnEnable()
        {
            ModManager.Initialize();

            currentScene = new Scene();
            wasPlaying = Application.isPlaying;
            sceneData = null;
            currentView = ModPanelView.Profile;

            // - Reset registration vars -
            inputEmail = true;
            emailAddressInput = "";
            securityCodeInput = "";
            isRequestSending = false;
        }

        protected virtual void OnSceneChange()
        {
            // - Initialize Scene Variables -
            currentScene = SceneManager.GetActiveScene();
            sceneData = Object.FindObjectOfType<EditorSceneData>();
            
            currentView = ModPanelView.Profile;
            modInitializationOptionIndex = 0;
            scrollPos = Vector2.zero;
            isModUploading = false;
        }

        private void DisplayModIOLoginPanel()
        {
            // TODO(@jackson): Improve with deselection/reselection of text on submit
            EditorGUILayout.LabelField("LOG IN TO/REGISTER YOUR MOD.IO ACCOUNT");

            using (new EditorGUI.DisabledScope(isRequestSending))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    using (new EditorGUI.DisabledScope(inputEmail))
                    {
                        if(GUILayout.Button("Email"))
                        {
                            inputEmail = true;
                        }
                    }
                    using (new EditorGUI.DisabledScope(!inputEmail))
                    {
                        if(GUILayout.Button("Security Code"))
                        {
                            inputEmail = false;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();


                if(inputEmail)
                {
                    emailAddressInput = EditorGUILayout.TextField("Email Address", emailAddressInput);
                }
                else
                {
                    securityCodeInput = EditorGUILayout.TextField("Security Code", securityCodeInput);
                }

                EditorGUILayout.BeginHorizontal();
                {
                    if(GUILayout.Button("Submit"))
                    {
                        isRequestSending = true;

                        if(inputEmail)
                        {
                            securityCodeInput = "";

                            ModManager.RequestSecurityCode(emailAddressInput,
                                                           (m) => { isRequestSending = false; inputEmail = false; },
                                                           (e) => { isRequestSending = false; });
                        }
                        else
                        {
                            ModManager.RequestOAuthToken(securityCodeInput,
                                                         (token) => ModManager.TryLogUserIn(token, u => isRequestSending = false, e => isRequestSending = false),
                                                         (e) => { isRequestSending = false; inputEmail = true; });
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DisplayModIOAccountHeader()
        {
            EditorGUILayout.LabelField("MOD.IO HEADER");

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Welcome " + ModManager.GetActiveUser().username);
                if(GUILayout.Button("Log Out"))
                {
                    ModManager.LogUserOut();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

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
            User activeUser = ModManager.GetActiveUser();
            if(activeUser == null)
            {
                DisplayModIOLoginPanel();
            }
            else
            {
                DisplayModIOAccountHeader();

                EditorGUILayout.Space();

                // ---[ Main Panel ]---
                if(sceneData == null)
                {
                    DisplayUninitializedSceneOptions();
                }
                else
                {
                    ModPanelView oldView = currentView;

                    EditorGUILayout.BeginHorizontal();
                        if(GUILayout.Button("Profile"))
                        {
                            currentView = ModPanelView.Profile;
                        }
                        if(GUILayout.Button("Files"))
                        {
                            currentView = ModPanelView.ModfileManagement;
                        }
                        if(GUILayout.Button("Media"))
                        {
                            currentView = ModPanelView.Media;
                        }
                    EditorGUILayout.EndHorizontal();

                    if(oldView != currentView)
                    {
                        scrollPos = Vector2.zero;
                    }


                    using (new EditorGUI.DisabledScope(isModUploading || Application.isPlaying))
                    {
                        SerializedObject serializedSceneData = new SerializedObject(sceneData);

                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                        // TODO(@jackson): Replace with a class instance?
                        switch(currentView)
                        {
                            case ModPanelView.Profile:
                                ModProfileView.ModProfilePanel(serializedSceneData.FindProperty("modInfo"),
                                                                sceneData.GetModLogoTexture(),
                                                                sceneData.GetModLogoSource(),
                                                                new List<string>(sceneData.modInfo.GetTagNames()),
                                                                ref isTagsExpanded);

                                doUploadInfo = GUILayout.Button("Save To Server");
                            break;

                            case ModPanelView.Media:
                                EditorModLayout.ModMediaPanel(serializedSceneData.FindProperty("modInfo"));

                                doUploadMedia = GUILayout.Button("Update Mod Media");
                            break;

                            case ModPanelView.ModfileManagement:
                                EditorModLayout.ModfileManagementPanel(serializedSceneData.FindProperty("buildLocation"),
                                                                       serializedSceneData.FindProperty("buildProfile"),
                                                                       serializedSceneData.FindProperty("setBuildAsPrimary"));

                                doUploadBinary = GUILayout.Button("Publish Build to Mod.IO");
                            break;
                        }
                    
                        EditorGUILayout.EndScrollView();

                        serializedSceneData.ApplyModifiedProperties();
                    }
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