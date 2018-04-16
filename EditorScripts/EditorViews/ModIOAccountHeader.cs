#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class ModIOAccountHeader : ISceneEditorHeader
    {
        // ---------[ FIELDS ]---------
        private bool isInputtingEmail;
        private string emailAddressInput;
        private string securityCodeInput;
        private bool isRequestSending;

        // - ISceneEditorView Interface -
        public void OnEnable()
        {
            isRequestSending = false;
            isInputtingEmail = false;
            emailAddressInput = "";
            securityCodeInput = "";
        }
        public void OnDisable() {}
        public bool IsInteractionDisabled()
        {
            return isRequestSending;
        }


        public void OnGUI()
        {
            if(ModManager.GetActiveUser() == null)
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

                            if(isInputtingEmail)
                            {
                                securityCodeInput = "";

                                ModManager.RequestSecurityCode(emailAddressInput,
                                                               (m) => { isRequestSending = false; isInputtingEmail = false; },
                                                               (e) => { isRequestSending = false; });
                            }
                            else
                            {
                                ModManager.RequestOAuthToken(securityCodeInput,
                                                             (token) => ModManager.TryLogUserIn(token, u => isRequestSending = false, e => isRequestSending = false),
                                                             (e) => { isRequestSending = false; isInputtingEmail = true; });
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                string username = ModManager.GetActiveUser().username;

                EditorGUILayout.LabelField("MOD.IO HEADER");

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Welcome " + username);
                    if(GUILayout.Button("Log Out"))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if(EditorDialogs.ConfirmLogOut(username))
                            {
                                ModManager.LogUserOut();
                            }
                        };
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
