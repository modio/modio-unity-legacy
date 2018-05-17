#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace ModIO
{
    // NOTE(@jackson): Could do with a beauty-pass
    // NOTE(@jackson): Present a login prompt for load (or force login)?
    // TODO(@jackson): Reload on user log
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
                EditorGUILayout.HelpBox("Load Mod Profile. Please wait...",
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
        protected Vector2 scrollPos;
        protected bool isRepaintRequired;
        // Profile Initialization
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

            // Profile Initialization
            if(modIdProperty.intValue == ScriptableModProfile.UNINITIALIZED_MOD_ID)
            {
                this.profile = null;

                string userAuthToken = CacheClient.LoadAuthenticatedUserOAuthToken();

                if(!String.IsNullOrEmpty(userAuthToken))
                {
                    APIClient.userAuthorizationToken = userAuthToken;

                    ModManager.GetAuthenticatedUserProfile((userProfile) =>
                    {
                        this.user = userProfile;

                        // - Find User Mods -
                        Action<List<ModTeamMember>> onGetModTeam = (modTeam) =>
                        {
                            System.Func<ModProfile, bool> userIsTeamMember = (p) =>
                            {
                                foreach(var teamMember in modTeam)
                                {
                                    if(teamMember.user.id == this.user.id
                                       && (int)teamMember.accessLevel >= (int)ModTeamMemberAccessLevel.Administrator)
                                    {
                                        return true;
                                    }
                                }
                                return false;
                            };

                            modInitializationOptionIndex = 0;
                            modList = CacheClient.AllModProfiles().Where(userIsTeamMember).ToArray();
                            modOptions = new string[modList.Length];
                            for(int i = 0; i < modList.Length; ++i)
                            {
                                ModProfile mod = modList[i];
                                modOptions[i] = mod.name;
                            }
                        };

                        ModManager.GetModTeam(modIdProperty.intValue,
                                              onGetModTeam,
                                              APIClient.LogError);
                    },
                    APIClient.LogError);

                }

                modInitializationOptionIndex = -1;
            }
            else
            {
                // Initialize View
                profile = null;
                profileViewParts = new IModProfileViewPart[]
                {
                    new LoadingProfileViewPart()
                };

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

            // Events
            EditorApplication.update += OnUpdate;
        }

        protected virtual void OnDisable()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnDisable();
            }

            EditorApplication.update -= OnUpdate;
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
                foreach(IModProfileViewPart viewPart in profileViewParts)
                {
                    viewPart.OnGUI();
                }
                serializedObject.ApplyModifiedProperties();
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

            if(modList.Length > 0)
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

                        OnDisable();
                        OnEnable();
                        isRepaintRequired = true;
                    };
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
