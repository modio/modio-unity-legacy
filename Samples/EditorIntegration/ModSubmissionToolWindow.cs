#if UNITY_EDITOR

// #define UPLOAD_MOD_BINARY_AS_DIRECTORY

using System;
using System.Collections.Generic;
using File = System.IO.File;
using Directory = System.IO.Directory;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModIO.EditorCode
{
    public class ModSubmissionToolWindow : EditorWindow
    {
        [MenuItem("Tools/mod.io/Mod Submission Tool")]
        public static void ShowWindow()
        {
            GetWindow<ModSubmissionToolWindow>("Submit Mod");
        }

// ---------[ CONSTANTS ]---------
#if !UPLOAD_MOD_BINARY_AS_DIRECTORY
        private readonly string[] modBinaryFileExtensionFilters = { "All Files", "" };
#endif

        // ---------[ WINDOW FIELDS ]---------
        private static bool isAwaitingServerResponse = false;

        private UserProfile user;
        // - Submission -
        private ScriptableModProfile profile;
        private string buildFilePath;
        private EditableModfile buildProfile;
        private string uploadSucceededMessage;
        private string uploadFailedMessage;

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            buildProfile = new EditableModfile();
            buildProfile.version.value = "0.0.0";
            uploadSucceededMessage = null;
            uploadFailedMessage = null;

            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                ModManager.GetAuthenticatedUserProfile((userProfile) => {
                    this.user = userProfile;
                    Repaint();
                }, null);
            }

            LoginWindow.userLoggedIn += OnUserLogin;
        }

        protected virtual void OnDisable()
        {
            LoginWindow.userLoggedIn -= OnUserLogin;
        }

        protected virtual void OnUserLogin(UserProfile userProfile)
        {
            this.OnDisable();
            this.OnEnable();
        }

        // ---------[ GUI ]---------
        protected virtual void Update()
        {
            if(this.user != null && this.user.id != LocalUser.Profile.id)
            {
                this.user = null;
                Repaint();
            }
        }

        protected virtual void OnGUI()
        {
            LayoutSubmissionFields();
        }

        // ------[ LOGIN PROMPT ]------
        protected virtual void LayoutSubmissionFields()
        {
            using(new EditorGUI.DisabledScope(isAwaitingServerResponse))
            {
                // - Account Header -
                EditorGUILayout.BeginHorizontal();
                {
                    if(this.user == null)
                    {
                        EditorGUILayout.LabelField("Not logged in to mod.io");
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("Log In"))
                        {
                            LoginWindow.GetWindow<LoginWindow>("Login to mod.io");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Logged in as:  " + this.user.username);
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("Log Out"))
                        {
                            EditorApplication.delayCall += () =>
                            {
                                if(EditorDialogs.ConfirmLogOut(this.user.username))
                                {
                                    this.user = null;

                                    LocalUser.instance = new LocalUser();
                                    LocalUser.Save();

                                    isAwaitingServerResponse = false;

                                    Repaint();
                                }
                            };
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                // - Submission Section -
                if(!String.IsNullOrEmpty(uploadSucceededMessage))
                {
                    EditorGUILayout.HelpBox(uploadSucceededMessage, MessageType.Info);
                }
                else if(!String.IsNullOrEmpty(uploadFailedMessage))
                {
                    EditorGUILayout.HelpBox(uploadFailedMessage, MessageType.Error);
                }
                else if(profile == null)
                {
                    EditorGUILayout.HelpBox("Please select a mod profile as a the upload target.",
                                            MessageType.Info);
                }
                else if(profile.modId > 0)
                {
                    EditorGUILayout.HelpBox(
                        profile.editableModProfile.name.value
                            + " will be updated as used as the upload target on the server.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        profile.editableModProfile.name.value
                            + " will be created as a new profile on the server.",
                        MessageType.Info);
                }
                EditorGUILayout.Space();


                // TODO(@jackson): Support mods that haven't been downloaded?
                profile = EditorGUILayout.ObjectField("Mod Profile", profile,
                                                      typeof(ScriptableModProfile), false)
                              as ScriptableModProfile;

                // - Build Profile -
                using(new EditorGUI.DisabledScope(profile == null))
                {
                    EditorGUILayout.BeginHorizontal();
                    if(EditorGUILayoutExtensions.BrowseButton(buildFilePath,
                                                              new GUIContent("Modfile")))
                    {
                        EditorApplication.delayCall += () =>
                        {
#if UPLOAD_MOD_BINARY_AS_DIRECTORY
                            string path = EditorUtility.OpenFolderPanel("Set Build Location", "",
                                                                        "ModBinary");
#else
                            string path = EditorUtility.OpenFilePanelWithFilters(
                                "Set Build Location", "", modBinaryFileExtensionFilters);
#endif

                            if(path.Length != 0)
                            {
                                buildFilePath = path;
                            }
                        };
                    }
                    if(EditorGUILayoutExtensions.ClearButton())
                    {
                        buildFilePath = string.Empty;
                    }
                    EditorGUILayout.EndHorizontal();

// - Build Profile -
#if UPLOAD_MOD_BINARY_AS_DIRECTORY
                    using(new EditorGUI.DisabledScope(!Directory.Exists(buildFilePath)))
#else
                    using(new EditorGUI.DisabledScope(!File.Exists(buildFilePath)))
#endif
                    {
                        // - Version -
                        EditorGUI.BeginChangeCheck();
                        buildProfile.version.value =
                            EditorGUILayout.TextField("Version", buildProfile.version.value);
                        if(EditorGUI.EndChangeCheck())
                        {
                            buildProfile.version.isDirty = true;
                        }
                        // - Changelog -
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PrefixLabel("Changelog");
                        buildProfile.changelog.value = EditorGUILayoutExtensions.MultilineTextField(
                            buildProfile.changelog.value);
                        if(EditorGUI.EndChangeCheck())
                        {
                            buildProfile.changelog.isDirty = true;
                        }
                        // - Metadata -
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PrefixLabel("Metadata");
                        buildProfile.metadataBlob.value =
                            EditorGUILayoutExtensions.MultilineTextField(
                                buildProfile.metadataBlob.value);
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
        }

        protected virtual void UploadToServer()
        {
            isAwaitingServerResponse = true;

            string profileFilePath = AssetDatabase.GetAssetPath(profile);

            Action<WebRequestError> onSubmissionFailed = (e) =>
            {
                EditorUtility.DisplayDialog("Upload Failed",
                                            "Failed to update the mod profile on the server.\n"
                                                + e.displayMessage,
                                            "Close");

                uploadFailedMessage = e.displayMessage;
                if(e.fieldValidationMessages != null && e.fieldValidationMessages.Count > 0)
                {
                    foreach(var kvp in e.fieldValidationMessages)
                    {
                        uploadFailedMessage += "\n [" + kvp.Key + "]: " + kvp.Value;
                    }
                }

                isAwaitingServerResponse = false;
                Repaint();
            };

            if(profile.modId > 0)
            {
                ModManager.SubmitModChanges(
                    profile.modId, profile.editableModProfile,
                    (m) => ModProfileSubmissionSucceeded(m, profileFilePath), onSubmissionFailed);
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
            if(updatedProfile == null)
            {
                isAwaitingServerResponse = false;
                return;
            }


            uploadFailedMessage = null;

            // Update ScriptableModProfile
            profile.modId = updatedProfile.id;
            profile.editableModProfile = EditableModProfile.CreateFromProfile(updatedProfile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

// Upload Build
#if UPLOAD_MOD_BINARY_AS_DIRECTORY
            if(Directory.Exists(buildFilePath))
#else
            if(File.Exists(buildFilePath))
#endif
            {
                Action<WebRequestError> onSubmissionFailed = (e) =>
                {
                    EditorUtility.DisplayDialog("Upload Failed",
                                                "Failed to upload the mod build to the server.\n"
                                                    + e.displayMessage,
                                                "Close");

                    uploadFailedMessage = e.displayMessage;
                    if(e.fieldValidationMessages != null && e.fieldValidationMessages.Count > 0)
                    {
                        foreach(var kvp in e.fieldValidationMessages)
                        {
                            uploadFailedMessage += "\n [" + kvp.Key + "]: " + kvp.Value;
                        }
                    }

                    isAwaitingServerResponse = false;
                    Repaint();
                };

#if UPLOAD_MOD_BINARY_AS_DIRECTORY
                ModManager.UploadModBinaryDirectory(
                    profile.modId, buildProfile, buildFilePath, true,
                    mf => NotifySubmissionSucceeded(updatedProfile.name, updatedProfile.profileURL),
                    onSubmissionFailed);
#else
                ModManager.UploadModBinary_Unzipped(
                    profile.modId, buildProfile, buildFilePath, true,
                    mf => NotifySubmissionSucceeded(updatedProfile.name, updatedProfile.profileURL),
                    onSubmissionFailed);
#endif
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
            Repaint();
        }
    }
}
#endif
