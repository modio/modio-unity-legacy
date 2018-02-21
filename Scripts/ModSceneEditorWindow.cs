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
    public class ModSceneEditorWindow : EditorWindow
    {
        private const int GAME_ID = 0;
        private const string API_KEY = "";

        private Scene currentScene;
        private EditorSceneData sceneData = null;

        private Vector2 scrollPos;

        [MenuItem("ModIO/Mod Scene Info Editor")]
        public static void ShowWindow()
        {
            GetWindow<ModSceneEditorWindow>("Mod Inspector");
        }

        private void OnEnable()
        {
            ModManager.Initialize(GAME_ID, API_KEY);
        }

        // ---------[ HEADER DISPLAYS ]---------
        private bool inputEmail = true;
        private string emailAddressInput = "";
        private string securityCodeInput = "";
        private bool isRequestSending = false;

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

        // ---------[ UNITIALIZED SCENE ]---------
        private int modInitializationOptionIndex = 0;
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

            Undo.RegisterCreatedObjectUndo(sd_go, "Initialize scene");
        }

        private void UploadMod()
        {
            // TODO(@jackson): Assert scene being saved
            isModUploading = true;

            ModManager.SubmitMod(sceneData.modInfo,
                                 (mod) => { sceneData.modInfo = EditableModInfo.FromModInfo(mod); isModUploading = false; },
                                 (e) => { isModUploading = false; });
        }

        // ---------[ GUI DISPLAY ]---------
        private bool isModUploading = false;
        private void OnGUI()
        {
            // - Update Data -
            if(currentScene != SceneManager.GetActiveScene())
            {
                Debug.Log("Scene change detected. Loading Mod Data");

                currentScene = SceneManager.GetActiveScene();

                sceneData = Object.FindObjectOfType<EditorSceneData>();
                scrollPos = Vector2.zero;
                modInitializationOptionIndex = 0;
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
                    using (new EditorGUI.DisabledScope(isModUploading))
                    {
                        // - Upload -
                        if(GUILayout.Button("UPLOAD TO MOD.IO"))
                        {
                            UploadMod();
                        }

                        // - Mod Info -
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                        {
                            SerializedObject serializedSceneData = new SerializedObject(sceneData);
                            SceneDataInspector.DisplayAsObject(serializedSceneData);
                            serializedSceneData.ApplyModifiedProperties();
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
        }
    }
}

#endif