using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class LoginDialog : MonoBehaviour, IBrowserView
    {
        // ---------[ FIELDS ]---------
        [Serializable]
        public struct InputStateDisplays
        {
            public GameObject invalid;
            public GameObject email;
            public GameObject securityCode;
        }

        [Header("Settings")]
        [Tooltip("Invalid Submission Message")]
        public string invalidSubmissionMessage =
            "Input needs to be either a valid email address or the 5-Digit authentication code.";
        [Tooltip("Email Refused Message")]
        public string emailRefusedMessage =
            ("The email address was rejected by the mod.io server."
             + "\nPlease correct any mistakes, or try another email address.");

        [Header("UI Components")]
        [Tooltip("Objects to toggle depending on the state of the input field validation.")]
        public InputStateDisplays displayForInputState;
        public InputField inputField;

        // --- IBrowserView Implementation ---
        /// <summary>Canvas Group.</summary>
        public CanvasGroup canvasGroup
        {
            get {
                return this.gameObject.GetComponent<CanvasGroup>();
            }
        }

        /// <summary>Reset selection on hide.</summary>
        bool IBrowserView.resetSelectionOnHide
        {
            get {
                return true;
            }
        }

        /// <summary>Is the view a root view or window view?</summary>
        bool IBrowserView.isRootView
        {
            get {
                return false;
            }
        }

        /// <summary>The priority to focus the selectables.</summary>
        private List<Selectable> m_onFocusPriority = new List<Selectable>();

        /// <summary>The priority to focus the selectables.</summary>
        List<Selectable> IBrowserView.onFocusPriority
        {
            get {
                return this.m_onFocusPriority;
            }
        }

        // --------[ INITIALIZATION ]---------
        /// <summary>Build the focus priority list.</summary>
        private void Awake()
        {
            this.m_onFocusPriority = new List<Selectable>() {
                this.inputField,
            };
        }

        private void OnEnable()
        {
            // update button state
            OnTextInputUpdated();
        }

        private void Start()
        {
            inputField.onEndEdit.AddListener(val => {
                if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    TrySubmitAuthentication();
                }
            });
        }

        // ---------[ EVENTS ]---------
        public void OnTextInputUpdated()
        {
            string trimmedInput = inputField.text.Trim().Replace(" ", "");
            bool isEmail = Utility.IsEmail(trimmedInput);
            bool isCode = !isEmail && Utility.IsSecurityCode(trimmedInput);

            if(displayForInputState.invalid != null)
            {
                displayForInputState.invalid.SetActive(!isEmail && !isCode);
            }
            if(displayForInputState.email != null)
            {
                displayForInputState.email.SetActive(isEmail);
            }
            if(displayForInputState.securityCode != null)
            {
                displayForInputState.securityCode.SetActive(isCode);
            }
        }

        public void TrySubmitAuthentication()
        {
            string trimmedInput = inputField.text.Trim();

            inputField.interactable = false;

            if(Utility.IsEmail(trimmedInput))
            {
                APIClient.SendSecurityCode(trimmedInput, OnSecurityCodeSent,
                                           (e) => ProcessWebRequestError(e, false));
            }
            else if(Utility.IsSecurityCode(trimmedInput))
            {
                UserAccountManagement.AuthenticateWithSecurityCode(
                    trimmedInput.ToUpper(), OnAuthenticated,
                    (e) => ProcessWebRequestError(e, true));
            }
            else
            {
                StartCoroutine(DisableInteractivity(2f));

                MessageSystem.QueueMessage(MessageDisplayData.Type.Error, invalidSubmissionMessage);
            }
        }

        private void OnSecurityCodeSent(APIMessage apiMessage)
        {
            inputField.text = string.Empty;
            inputField.interactable = true;

            MessageSystem.QueueMessage(MessageDisplayData.Type.Success, apiMessage.message);
        }

        private void OnAuthenticated(UserProfile u)
        {
            inputField.text = string.Empty;
            inputField.interactable = true;

            MessageSystem.QueueMessage(MessageDisplayData.Type.Success, "Login Successful");

            ViewManager.instance.CloseWindowedView(this);
            ModBrowser.instance.OnUserLogin();
        }

        private void ProcessWebRequestError(WebRequestError e, bool isSecurityCode)
        {
            if(e.webRequest.responseCode == 401 && isSecurityCode)
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error, e.errorMessage);
            }
            else if(e.webRequest.responseCode == 422 && !isSecurityCode)
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error, emailRefusedMessage);
            }
            else
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Warning, e.displayMessage);
            }


            inputField.interactable = true;
        }

        private System.Collections.IEnumerator DisableInteractivity(float seconds)
        {
            inputField.interactable = false;

            yield return new WaitForSecondsRealtime(seconds);

            inputField.interactable = true;
        }

// ---------[ OBSOLETE ]---------
#pragma warning disable 0067
        [Obsolete("No longer trigger by this object.")]
        public event Action<string> onInvalidSubmissionAttempted;
        [Obsolete("No longer trigger by this object.")]
        public event Action<string> onEmailRefused;
        [Obsolete("No longer trigger by this object.")]
        public event Action<APIMessage> onSecurityCodeSent;
        [Obsolete("No longer trigger by this object.")]
        public event Action<string> onSecurityCodeRefused;
        [Obsolete("No longer trigger by this object.")]
        public event Action<string> onUserOAuthTokenReceived;
        [Obsolete("No longer trigger by this object.")]
        public event Action<WebRequestError> onWebRequestError;
#pragma warning restore 0067
    }
}
