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
            GetWindow<ModSubmissionToolWindow>("Mod Submission Tool");
        }

        // ------[ WINDOW FIELDS ]---------
        // - Login -
        private bool isInputtingEmail;
        private string emailAddressInput;
        private string securityCodeInput;
        private bool isRequestSending;

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            ModManager.Initialize();

            isInputtingEmail = true;
            emailAddressInput = "";
            securityCodeInput = "";
            isRequestSending = false;
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
            LayoutAccountHeader();

            // using (new EditorGUI.DisabledScope(Application.isPlaying))
            // {
            //     scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            //         activeView.OnGUI(sceneData);
            //     EditorGUILayout.EndScrollView();
            // }

            // wasPlaying = isPlaying;
        }

        // ------[ ACCOUNT HEADER ]------
        protected virtual void LayoutAccountHeader()
        {
            if(ModManager.GetActiveUser() != null)
            {
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
                                Repaint();
                            }
                        };
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // TODO(@jackson): Improve with deselection/reselection of text on submit
                EditorGUILayout.LabelField("LOG IN TO/REGISTER YOUR MOD.IO ACCOUNT");

                using (new EditorGUI.DisabledScope(isRequestSending))
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
                            isRequestSending = true;

                            Action endRequestSendingAndInputEmail = () =>
                            {
                                isRequestSending = false;
                                isInputtingEmail = true;
                            };

                            Action endRequestSendingAndInputCode = () =>
                            {
                                isRequestSending = false;
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
                                                            (u) => { isRequestSending = false; Repaint(); },
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
        }

        // ---------[ FIELDS ]---------
        // public void OnGUI()
        // {

        //
        //     }
        // }
    }
}

#endif