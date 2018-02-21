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

        private void DisplayModIOLoginHeader()
        {
            // TODO(@jackson): Improve with deselection/reselection of text on submit
            EditorGUILayout.LabelField("LOG IN TO/REGISTER YOUR MOD.IO ACCOUNT");

            using (new EditorGUI.DisabledScope(isRequestSending))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    using (new EditorGUI.DisabledScope(inputEmail))
                    {
                        if(GUILayout.Button("Email"))//, GUI.skin.label))
                        {
                            inputEmail = true;
                        }
                    }
                    using (new EditorGUI.DisabledScope(!inputEmail))
                    {
                        if(GUILayout.Button("Security Code"))//, GUI.skin.label))
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
            EditorGUILayout.LabelField("Welcome " + ModManager.GetActiveUser().username);
            if(GUILayout.Button("Log Out"))
            {
                ModManager.LogUserOut();
            }
        }

        // ---------[ GUI DISPLAY ]---------
        private void OnGUI()
        {
            bool isModEditingEnabled = true;

            // - Update Data -
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

            // ---[ HEADER ]---
            User activeUser = ModManager.GetActiveUser();
            if(activeUser == null)
            {
                isModEditingEnabled = false;
                DisplayModIOLoginHeader();
            }
            else
            {
                DisplayModIOAccountHeader();
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

            // ---[ MOD INFO ]---
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

#endif