#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;

using Directory = System.IO.Directory;
using Path = System.IO.Path;

using UnityEditor;
using UnityEngine;

namespace ModIO.EditorCode
{
    // NOTE(@jackson): Could do with a beauty-pass
    [CustomEditor(typeof(ScriptableModProfile))]
    public class ModProfileEditor : UnityEditor.Editor
    {
        // ------[ NESTED CLASSES ]------
        protected class LoadingProfileViewPart : IModProfileViewPart
        {
            public void OnEnable(SerializedProperty editableProfileProperty, ModProfile baseProfile,
                                 UserProfile user)
            {
            }
            public void OnDisable() {}
            public void OnUpdate() {}
            public bool IsRepaintRequired()
            {
                return false;
            }

            public void OnGUI()
            {
                EditorGUILayout.HelpBox("Loading Mod Profile. Please wait...", MessageType.Info);
            }
        }

        // ------[ SERIALIZED PROPERTIES ]------
        private SerializedProperty modIdProperty;
        private SerializedProperty editableModProfileProperty;

        // ------[ EDITOR CACHING ]------
        private ModProfile profile;
        private int initializedProfileId;
        private UserProfile user;

        // ------[ VIEW INFORMATION ]------
        private IModProfileViewPart[] profileViewParts;
        private bool isProfileSyncing;
        protected Vector2 scrollPos;
        protected bool isRepaintRequired;

        // Profile Initialization
        private bool isModListLoading;
        private int modInitializationOptionIndex;
        private ModProfile[] modList;
        private string[] modOptions;

        // Profile Load Fail
        private string profileGetErrorMessage;

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            // Grab Serialized Properties
            serializedObject.Update();
            modIdProperty = serializedObject.FindProperty("modId");
            initializedProfileId = modIdProperty.intValue;
            editableModProfileProperty = serializedObject.FindProperty("editableModProfile");
            isModListLoading = false;
            profileViewParts = new IModProfileViewPart[] { new LoadingProfileViewPart() };

            // Profile Initialization
            if(modIdProperty.intValue == ScriptableModProfile.UNINITIALIZED_MOD_ID)
            {
                this.profile = null;

                if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
                {
                    this.isModListLoading = true;
                    this.modOptions = new string[] { "Loading..." };

                    Action<WebRequestError> onError = (e) =>
                    { isModListLoading = false; };

                    ModManager.GetAuthenticatedUserProfile((userProfile) => {
                        this.user = userProfile;

                        // - Find User Mods -
                        Action<List<ModProfile>> onGetUserMods = (profiles) =>
                        {
                            modInitializationOptionIndex = 0;
                            modList = profiles.ToArray();
                            modOptions = new string[modList.Length];
                            for(int i = 0; i < modList.Length; ++i)
                            {
                                ModProfile mod = modList[i];
                                modOptions[i] = mod.name;
                            }

                            isModListLoading = false;
                        };

                        ModManager.FetchAuthenticatedUserMods(onGetUserMods, onError);
                    }, onError);
                }
                else
                {
                    this.modOptions = new string[0];
                }

                modInitializationOptionIndex = 0;
            }
            else
            {
                // Initialize View
                profile = null;

                if(modIdProperty.intValue == 0)
                {
                    profile = null;

                    profileViewParts = CreateProfileViewParts();

                    foreach(IModProfileViewPart viewPart in profileViewParts)
                    {
                        viewPart.OnEnable(editableModProfileProperty, null, this.user);
                    };
                }
                else
                {
                    System.Action<ModProfile> onGetProfile = (p) =>
                    {
                        profile = p;

                        profileViewParts = CreateProfileViewParts();

                        foreach(IModProfileViewPart viewPart in profileViewParts)
                        {
                            viewPart.OnEnable(editableModProfileProperty, p, this.user);
                        };

                        profileGetErrorMessage = string.Empty;
                    };

                    System.Action<WebRequestError> onGetProfileError = (e) =>
                    {
                        profile = null;

                        profileViewParts = CreateProfileViewParts();

                        foreach(IModProfileViewPart viewPart in profileViewParts)
                        {
                            viewPart.OnEnable(editableModProfileProperty, null, this.user);
                        };

                        profileGetErrorMessage =
                            ("Unable to fetch the mod profile data on the server.\n"
                             + e.displayMessage);
                    };

                    ModManager.GetModProfile(modIdProperty.intValue, onGetProfile,
                                             onGetProfileError);
                }
            }

            scrollPos = Vector2.zero;
            isProfileSyncing = false;

            // Events
            EditorApplication.update += OnUpdate;
            LoginWindow.userLoggedIn += OnUserLogin;
        }

        protected virtual void OnDisable()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts) { viewPart.OnDisable(); }

            EditorApplication.update -= OnUpdate;
            LoginWindow.userLoggedIn -= OnUserLogin;
        }

        protected virtual void OnUserLogin(UserProfile userProfile)
        {
            this.user = userProfile;
            this.OnDisable();
            this.OnEnable();
        }

        protected virtual IModProfileViewPart[] CreateProfileViewParts()
        {
            return new IModProfileViewPart[] {
                new ModProfileInfoViewPart(),
                new ModMediaViewPart(),
            };
        }

        // ------[ GUI ]------
        public override void OnInspectorGUI()
        {
            if(serializedObject.FindProperty("modId").intValue
               == ScriptableModProfile.UNINITIALIZED_MOD_ID)
            {
                LayoutProfileInitialization();
            }
            else
            {
                serializedObject.Update();

                bool isProfileSyncRequested = false;

                if(!String.IsNullOrEmpty(profileGetErrorMessage))
                {
                    EditorGUILayout.HelpBox(profileGetErrorMessage, MessageType.Warning);
                }

                using(new EditorGUI.DisabledScope(isProfileSyncing))
                {
                    if(profileViewParts != null && profileViewParts.Length > 0)
                    {
                        using(new EditorGUI.DisabledScope(modIdProperty.intValue == 0))
                        {
                            string syncButtonText;
                            if(profile == null)
                            {
                                syncButtonText = "Retry Fetching Server Data";
                            }
                            else
                            {
                                syncButtonText = "Pull Server Data Updates";
                            }
                            isProfileSyncRequested = GUILayout.Button(syncButtonText);
                            EditorGUILayout.Space();
                        }
                    }

                    foreach(IModProfileViewPart viewPart in profileViewParts) { viewPart.OnGUI(); }
                    serializedObject.ApplyModifiedProperties();
                }

                if(isProfileSyncRequested)
                {
                    isProfileSyncing = true;
                    APIClient.GetMod(
                        modIdProperty.intValue,
                        (modProfile) => {
                            CacheClient.SaveModProfile(modProfile, null);
                            this.profile = modProfile;

                            ScriptableModProfile smp = this.target as ScriptableModProfile;
                            Undo.RecordObject(smp, "Update Mod Profile");
                            smp.editableModProfile.ApplyBaseProfileChanges(profile);

                            isProfileSyncing = false;
                            profileGetErrorMessage = null;

                            this.OnDisable();
                            this.OnEnable();
                        },
                        (e) => {
                            isProfileSyncing = false;
                            profileGetErrorMessage =
                                ("Unable to fetch the mod profile data on the server.\n"
                                 + e.displayMessage);
                        });
                }
            }

            isRepaintRequired = false;
        }

        protected virtual void LayoutProfileInitialization()
        {
            EditorGUILayout.LabelField("Initialize Mod Profile");

            // ---[ DISPLAY ]---
            EditorGUILayout.Space();

            if(GUILayout.Button("Create New"))
            {
                EditorApplication.delayCall += () =>
                {
                    ScriptableModProfile smp = this.target as ScriptableModProfile;
                    Undo.RecordObject(smp, "Initialize Mod Profile");

                    smp.modId = 0;
                    smp.editableModProfile = new EditableModProfile();

                    OnDisable();
                    OnEnable();
                    isRepaintRequired = true;
                };
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("---- OR ----");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Load Existing Profile");

            if(user == null)
            {
                EditorGUILayout.HelpBox("Log in required to load existing mods", MessageType.Info);
                if(GUILayout.Button("Log In to mod.io"))
                {
                    LoginWindow.GetWindow<LoginWindow>("Login to mod.io");
                }
            }
            else if(modOptions.Length > 0)
            {
                using(new EditorGUI.DisabledScope(isModListLoading))
                {
                    modInitializationOptionIndex = EditorGUILayout.Popup(
                        "Select Mod", modInitializationOptionIndex, modOptions);
                    if(GUILayout.Button("Load"))
                    {
                        ModProfile profile = modList[modInitializationOptionIndex];
                        EditorApplication.delayCall += () =>
                        {
                            ScriptableModProfile smp = this.target as ScriptableModProfile;
                            Undo.RecordObject(smp, "Initialize Mod Profile");

                            smp.modId = profile.id;
                            smp.editableModProfile = EditableModProfile.CreateFromProfile(profile);

                            string smpFilePath = AssetDatabase.GetAssetPath(smp);
                            string smpDir = Path.GetDirectoryName(smpFilePath);

                            int profileCount =
                                Directory
                                    .GetFiles(smpDir, profile.name + "*.asset",
                                              System.IO.SearchOption.TopDirectoryOnly)
                                    .Length;

                            string fileNameAddition =
                                (profileCount > 0 ? " (" + profileCount.ToString() + ")" : "");

                            AssetDatabase.RenameAsset(smpFilePath,
                                                      profile.name + fileNameAddition + ".asset");

                            OnDisable();
                            OnEnable();
                            isRepaintRequired = true;
                        };
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No loadable mod profiles detected.", MessageType.Info);
            }
        }

        // ------[ UPDATE ]------
        public virtual void OnUpdate()
        {
            if(modIdProperty.intValue != initializedProfileId)
            {
                this.OnDisable();
                this.OnEnable();
            }

            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnUpdate();
                isRepaintRequired |= viewPart.IsRepaintRequired();
            }

            if(isRepaintRequired)
            {
                Repaint();
            }
        }
    }
}

#endif
