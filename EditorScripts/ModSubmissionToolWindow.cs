#if UNITY_EDITOR

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModIO
{
    public class ModSubmissionToolWindow : EditorWindow
    {
        [MenuItem("mod.io/Mod Submission Tool")]
        public static void ShowWindow()
        {
            GetWindow<ModSubmissionToolWindow>("Submit Mod");
        }

        // ------[ WINDOW FIELDS ]---------
        private static bool isAwaitingServerResponse = false;
        // - Login -
        private bool isInputtingEmail;
        private string emailAddressInput;
        private string securityCodeInput;
        // - Submission -
        private ScriptableModProfile profile;
        private AssetBundle build;
        private EditableModfile buildProfile;

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            ModManager.Initialize();

            isInputtingEmail = true;
            emailAddressInput = "";
            securityCodeInput = "";

            buildProfile = new EditableModfile();
            buildProfile.version.value = "0.0.0";
        }

        protected virtual void OnDisable()
        {
        }

        // ---------[ UPDATES ]---------
        protected virtual void OnInspectorUpdate()
        {
            // TODO(@jackson): Repaint once uploaded
            // if(isRepaintRequired
            //    || wasActiveViewDisabled != activeView.IsViewDisabled()
            //    || wasHeaderDisabled != GetEditorHeader().IsInteractionDisabled())
            // {
            //     Repaint();
            //     isRepaintRequired = false;
            // }
            // wasActiveViewDisabled = activeView.IsViewDisabled();
            // wasHeaderDisabled = GetEditorHeader().IsInteractionDisabled();
        }

        // ---------[ GUI ]---------
        protected virtual void OnGUI()
        {
            if(ModManager.GetActiveUser() == null)
            {
                LayoutLoginPrompt();
            }
            else
            {
                LayoutSubmissionFields();
            }
        }

        // ------[ LOGIN PROMPT ]------
        protected virtual void LayoutLoginPrompt()
        {
            // TODO(@jackson): Improve with deselection/reselection of text on submit
            EditorGUILayout.LabelField("LOG IN TO/REGISTER YOUR MOD.IO ACCOUNT");

            using (new EditorGUI.DisabledScope(isAwaitingServerResponse))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    using (new EditorGUI.DisabledScope(isInputtingEmail))
                    {
                        if(GUILayout.Button("Email"))
                        {
                            isInputtingEmail = true;
                        }
                    }
                    using (new EditorGUI.DisabledScope(!isInputtingEmail))
                    {
                        if(GUILayout.Button("Security Code"))
                        {
                            isInputtingEmail = false;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();


                if(isInputtingEmail)
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
                        isAwaitingServerResponse = true;

                        Action endRequestSendingAndInputEmail = () =>
                        {
                            isAwaitingServerResponse = false;
                            isInputtingEmail = true;
                        };

                        Action endRequestSendingAndInputCode = () =>
                        {
                            isAwaitingServerResponse = false;
                            isInputtingEmail = false;
                        };

                        if(isInputtingEmail)
                        {
                            securityCodeInput = "";

                            ModManager.RequestSecurityCode(emailAddressInput,
                                                           m => endRequestSendingAndInputCode(),
                                                           e => endRequestSendingAndInputEmail());
                        }
                        else
                        {
                            Action<string> onTokenReceived = (token) =>
                            {
                                ModManager.TryLogUserIn(token,
                                                        (u) => { isAwaitingServerResponse = false; Repaint(); },
                                                        e => endRequestSendingAndInputCode());
                            };

                            ModManager.RequestOAuthToken(securityCodeInput,
                                                         onTokenReceived,
                                                         e => endRequestSendingAndInputCode());
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        protected virtual void LayoutSubmissionFields()
        {
            // - Account Header -
            string username = ModManager.GetActiveUser().username;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Logged in as:  " + username);
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Log Out"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if(EditorDialogs.ConfirmLogOut(username))
                        {
                            ModManager.LogUserOut();

                            isInputtingEmail = true;
                            emailAddressInput = "";
                            securityCodeInput = "";
                            isAwaitingServerResponse = false;

                            Repaint();
                        }
                    };
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // - Submission Section -
            if(profile == null)
            {
                EditorGUILayout.HelpBox("Please select a mod profile as a the upload target.",
                                        MessageType.Info);
            }
            else if(profile.modId > 0)
            {
                EditorGUILayout.HelpBox(profile.editableModProfile.name.value
                                        + " will be updated as used as the upload target on the server.",
                                        MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(profile.editableModProfile.name.value
                                        + " will be created as a new profile on the server.",
                                        MessageType.Info);
            }
            EditorGUILayout.Space();


            // TODO(@jackson): Support mods that haven't been downloaded
            profile = EditorGUILayout.ObjectField("Mod Profile",
                                                  profile,
                                                  typeof(ScriptableModProfile),
                                                  false) as ScriptableModProfile;

            using(new EditorGUI.DisabledScope(profile == null))
            {
                build = EditorGUILayout.ObjectField("Add Build",
                                                    build,
                                                    typeof(AssetBundle),
                                                    false) as AssetBundle;
    
                // - Build Profile -
                using(new EditorGUI.DisabledScope(build == null))
                {
                    // - Version -
                    EditorGUI.BeginChangeCheck();
                        buildProfile.version.value = EditorGUILayout.TextField("Version",
                                                                               buildProfile.version.value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        buildProfile.version.isDirty = true;
                    }
                    // - Changelog -
                    EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PrefixLabel("Changelog");
                        buildProfile.changelog.value = EditorGUILayoutExtensions.MultilineTextField(buildProfile.changelog.value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        buildProfile.changelog.isDirty = true;
                    }
                    // - Metadata -
                    EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PrefixLabel("Metadata");
                        buildProfile.metadataBlob.value = EditorGUILayoutExtensions.MultilineTextField(buildProfile.metadataBlob.value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        buildProfile.metadataBlob.isDirty = true;
                    }
                }

                // TODO(@jackson): if(profile) -> show build list?
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button("Upload to Server"))
                    {
                        UploadToServer();
                    }
                    GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        // TODO(@jackson): Check hash of build for potential match
        // TODO(@jackson): Check profile errors
        protected virtual void UploadToServer()
        {
            isAwaitingServerResponse = true;

            string profileFilePath = AssetDatabase.GetAssetPath(profile);

            Action<WebRequestError> onSubmissionFailed = (e) =>
            {
                // TODO(@jackson): Dialog Window?
                isAwaitingServerResponse = false;
            };

            if(profile.modId > 0)
            {
                ModManager.SubmitModChanges(profile.modId,
                                            profile.editableModProfile,
                                            (m) => ModProfileSubmissionSucceeded(m, profileFilePath),
                                            onSubmissionFailed);
            }
            else
            {
                ModManager.SubmitNewMod(profile.editableModProfile,
                                        (m) => ModProfileSubmissionSucceeded(m, profileFilePath),
                                        onSubmissionFailed);
            }
        }

        private void ModProfileSubmissionSucceeded(ModProfile updatedProfile,
                                                   string profileFilePath)
        {
            // Update ScriptableModProfile
            profile.modId = updatedProfile.id;
            profile.editableModProfile = EditableModProfile.CreateFromProfile(updatedProfile);
            AssetDatabase.CreateAsset(profile, profileFilePath);

            // Upload Build
            if(build != null)
            {
                Action<WebRequestError> onSubmissionFailed = (e) =>
                {
                    // TODO(@jackson): Dialog Window?
                    isAwaitingServerResponse = false;
                };

                string buildFilePath = AssetDatabase.GetAssetPath(build);
                ModManager.UploadModBinary_Unzipped(profile.modId,
                                                    buildProfile,
                                                    buildFilePath,
                                                    true,
                                                    mf => NotifySubmissionSucceeded(updatedProfile.name,
                                                                                    updatedProfile.profileURL),
                                                    onSubmissionFailed);
            }
            else
            {
                NotifySubmissionSucceeded(updatedProfile.name, updatedProfile.profileURL);
            }
        }

        private void NotifySubmissionSucceeded(string modName, string modProfileURL)
        {
            EditorUtility.DisplayDialog("Submission Successful",
                                        modName + " was successfully updated on the server."
                                        + "\nView the changes here: " + modProfileURL,
                                        "Close");
            isAwaitingServerResponse = false;
        }
    }
}
#endif
