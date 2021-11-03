#if UNITY_EDITOR

using System;

using UnityEditor;
using UnityEngine;

namespace ModIO.EditorCode
{
    public class LoginWindow : EditorWindow
    {
        // ---------[ MEMBERS ]---------
        public static event Action<UserProfile> userLoggedIn;
        private static bool isAwaitingServerResponse = false;

        private bool isInputtingEmail;
        private string emailAddressInput;
        private string securityCodeInput;
        private bool isLoggedIn;

        private string helpMessage = string.Empty;
        private MessageType helpType = MessageType.Info;

        // ---------[ INITIALIZATION ]---------
        protected virtual void OnEnable()
        {
            isInputtingEmail = true;
            emailAddressInput = "";
            securityCodeInput = "";
            isLoggedIn = false;
        }

        protected virtual void OnGUI()
        {
            EditorGUILayout.LabelField("LOG IN TO/REGISTER YOUR MOD.IO ACCOUNT");

            using(new EditorGUI.DisabledScope(isAwaitingServerResponse || isLoggedIn))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    using(new EditorGUI.DisabledScope(isInputtingEmail))
                    {
                        if(GUILayout.Button("Email"))
                        {
                            isInputtingEmail = true;
                        }
                    }
                    using(new EditorGUI.DisabledScope(!isInputtingEmail))
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
                    emailAddressInput =
                        EditorGUILayout.TextField("Email Address", emailAddressInput);
                }
                else
                {
                    securityCodeInput =
                        EditorGUILayout.TextField("Security Code", securityCodeInput);
                }

                EditorGUILayout.BeginHorizontal();
                {
                    if(GUILayout.Button("Submit"))
                    {
                        isAwaitingServerResponse = true;
                        GUIUtility.keyboardControl = 0;

                        Action<string, MessageType> endRequestSendingAndInputEmail = (m, t) =>
                        {
                            isAwaitingServerResponse = false;
                            isInputtingEmail = true;
                            helpMessage = m;
                            helpType = t;
                            Repaint();
                        };

                        Action<string, MessageType> endRequestSendingAndInputCode = (m, t) =>
                        {
                            isAwaitingServerResponse = false;
                            isInputtingEmail = false;
                            helpMessage = m;
                            helpType = t;
                            Repaint();
                        };

                        if(isInputtingEmail)
                        {
                            securityCodeInput = "";

                            APIClient.SendSecurityCode(
                                emailAddressInput,
                                m => endRequestSendingAndInputCode(m.message, MessageType.Info),
                                e => endRequestSendingAndInputEmail(ConvertErrorToHelpString(e),
                                                                    MessageType.Error));
                        }
                        else
                        {
                            UserAccountManagement.AuthenticateWithSecurityCode(
                                securityCodeInput,
                                (u) => {
                                    helpMessage = ("Welcome " + u.username
                                                   + "! You have successfully logged in."
                                                   + " Feel free to close this window.");
                                    isLoggedIn = true;

                                    LoginWindow.isAwaitingServerResponse = false;
                                    Repaint();

                                    if(userLoggedIn != null)
                                    {
                                        userLoggedIn(u);
                                    }
                                },
                                (e) => {
                                    endRequestSendingAndInputCode(ConvertErrorToHelpString(e),
                                                                  MessageType.Error);
                                });
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if(!String.IsNullOrEmpty(helpMessage))
            {
                EditorGUILayout.HelpBox(helpMessage, helpType);
            }
        }

        private string ConvertErrorToHelpString(WebRequestError error)
        {
            if(error.fieldValidationMessages != null && error.fieldValidationMessages.Count > 0)
            {
                var helpString = new System.Text.StringBuilder();

                foreach(string message in error.fieldValidationMessages.Values)
                {
                    helpString.Append(message + "\n");
                }

                helpString.Length -= 1;

                return helpString.ToString();
            }
            else
            {
                return error.displayMessage;
            }
        }
    }
}

#endif
