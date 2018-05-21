#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace ModIO
{
    // NOTE(@jackson): Could do with a beauty-pass
    // TODO(@jackson): Needs to still allow editing if loading fails
    [CustomEditor(typeof(ScriptableModProfile))]
    public class ModProfileEditor : Editor
    {
        // ------[ NESTED CLASSES ]------
        protected class LoadingProfileViewPart : IModProfileViewPart
        {
            public void OnEnable(SerializedProperty editableProfileProperty, ModProfile baseProfile, UserProfile user) {}
            public void OnDisable(){}
            public void OnUpdate(){}
            public bool IsRepaintRequired() { return false; }

            public void OnGUI()
            {
                EditorGUILayout.HelpBox("Loading Mod Profile. Please wait...",
                                        MessageType.Info);
            }
        }

        // ------[ SERIALIZED PROPERTIES ]------
        private SerializedProperty modIdProperty;
        private SerializedProperty editableModProfileProperty;

        // ------[ EDITOR CACHING ]------
        private ModProfile profile;
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

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            // Grab Serialized Properties
            serializedObject.Update();
            modIdProperty = serializedObject.FindProperty("modId");
            editableModProfileProperty = serializedObject.FindProperty("editableModProfile");
            isModListLoading = false;
            profileViewParts = new IModProfileViewPart[]
            {
                new LoadingProfileViewPart()
            };

            // Profile Initialization
            if(modIdProperty.intValue == ScriptableModProfile.UNINITIALIZED_MOD_ID)
            {
                this.profile = null;

                string userAuthToken = CacheClient.LoadAuthenticatedUserToken();

                if(!String.IsNullOrEmpty(userAuthToken))
                {
                    APIClient.userAuthorizationToken = userAuthToken;

                    this.isModListLoading = true;
                    this.modOptions = new string[]{ "Loading..." };

                    Action<WebRequestError> onError = (e) =>
                    {
                        APIClient.LogError(e);
                        isModListLoading = false;
                    };

                    ModManager.GetAuthenticatedUserProfile((userProfile) =>
                    {
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

                        ModManager.GetAuthenticatedUserMods(onGetUserMods, onError);
                    },
                    onError);
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

                System.Action<ModProfile> onGetProfile = (p) =>
                {
                    profileViewParts = CreateProfileViewParts();

                    foreach(IModProfileViewPart viewPart in profileViewParts)
                    {
                        viewPart.OnEnable(editableModProfileProperty, p, this.user);
                    };
                };

                ModManager.GetModProfile(modIdProperty.intValue,
                                         onGetProfile,
                                         APIClient.LogError);
            }

            scrollPos = Vector2.zero;
            isProfileSyncing = false;

            // Events
            EditorApplication.update += OnUpdate;
            LoginWindow.userLoggedIn += OnUserLogin;
        }

        protected virtual void OnDisable()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnDisable();
            }

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
            return new IModProfileViewPart[]
            {
                new ModProfileInfoViewPart(),
                new ModMediaViewPart(),
            };
        }

        // ------[ GUI ]------
        public override void OnInspectorGUI()
        {
            if(serializedObject.FindProperty("modId").intValue < 0)
            {
                LayoutProfileInitialization();
            }
            else
            {
                serializedObject.Update();

                bool isProfileSyncRequested = false;

                using(new EditorGUI.DisabledScope(modIdProperty.intValue == 0))
                {
                    isProfileSyncRequested = GUILayout.Button("Pull Server Updates");
                    EditorGUILayout.Space();
                }

                using(new EditorGUI.DisabledScope(isProfileSyncing))
                {
                    foreach(IModProfileViewPart viewPart in profileViewParts)
                    {
                        viewPart.OnGUI();
                    }
                    serializedObject.ApplyModifiedProperties();
                }

                if(isProfileSyncRequested)
                {
                    isProfileSyncing = true;
                    APIClient.GetMod(modIdProperty.intValue,
                    (modProfile) =>
                    {
                        CacheClient.SaveModProfile(modProfile);
                        this.profile = modProfile;

                        ScriptableModProfile smp = this.target as ScriptableModProfile;
                        Undo.RecordObject(smp, "Update Mod Profile");
                        smp.editableModProfile.ApplyBaseProfileChanges(profile);

                        isProfileSyncing = false;

                        this.OnDisable();
                        this.OnEnable();
                    },
                    (e) =>
                    {
                        isProfileSyncing = false;
                        Debug.LogWarning(e.ToUnityDebugString());
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
                EditorGUILayout.HelpBox("Log in required to load existing mods",
                                        MessageType.Info);
                if(GUILayout.Button("Log In to mod.io"))
                {
                    LoginWindow.GetWindow<LoginWindow>("Login to mod.io");
                }
            }
            else if(modOptions.Length > 0)
            {
                using(new EditorGUI.DisabledScope(isModListLoading))
                {
                    modInitializationOptionIndex = EditorGUILayout.Popup("Select Mod", modInitializationOptionIndex, modOptions, null);
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
                            string smpDir = System.IO.Path.GetDirectoryName(smpFilePath);

                            int profileCount
                            = System.IO.Directory.GetFiles(smpDir, profile.name + "*.asset").Length;

                            string fileNameAddition = (profileCount > 0
                                                       ? " (" + profileCount.ToString() + ")"
                                                       : "");

                            AssetDatabase.RenameAsset(smpFilePath, profile.name + fileNameAddition + ".asset");

                            OnDisable();
                            OnEnable();
                            isRepaintRequired = true;
                        };
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No loadable mod profiles detected.",
                                        MessageType.Info);
            }
        }

        // ------[ UPDATE ]------
        public virtual void OnUpdate()
        {
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
