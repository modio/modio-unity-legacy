using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class LoginDialog : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        public event Action<APIMessage> onSecurityCodeSent;
        public event Action<string> onUserOAuthTokenReceived;
        public event Action<string> onInvalidSubmissionAttempted;
        public event Action<WebRequestError> onAPIRequestError;

        [Serializable]
        public struct InputStateDisplays
        {
            public GameObject invalid;
            public GameObject email;
            public GameObject securityCode;
        }

        [Header("Settings")]
        // TODO(@jackson): localize
        [Tooltip("Invalid Submission Message")]
        public string invalidSubmissionMessage = "Input needs to be either a valid email address or the 5-Digit authentication code.";

        [Header("UI Components")]
        [Tooltip("Objects to toggle depending on the state of the input field validation.")]
        public InputStateDisplays displayForInputState;
        public InputField inputField;


        // --------[ INITIALIZATION ]---------
        public void Initialize()
        {
            inputField.text = string.Empty;
            OnTextInputUpdated();
        }

        private void Start()
        {
            inputField.onEndEdit.AddListener(val =>
            {
                if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    TrySubmitAuthentication();
                }
            });


            this.onSecurityCodeSent += (m) =>
            {
                inputField.text = string.Empty;
                inputField.interactable = true;
            };

            this.onUserOAuthTokenReceived += (t) =>
            {
                inputField.text = string.Empty;
                inputField.interactable = true;
            };

            this.onAPIRequestError += (e) =>
            {
                inputField.interactable = true;
            };
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
                APIClient.SendSecurityCode(trimmedInput,
                                           (m) => onSecurityCodeSent(m),
                                           (e) => onAPIRequestError(e));
            }
            else if(Utility.IsSecurityCode(trimmedInput))
            {
                APIClient.GetOAuthToken(trimmedInput.ToUpper(),
                                        (s) => onUserOAuthTokenReceived(s),
                                        (e) => onAPIRequestError(e));
            }
            else
            {
                StartCoroutine(DisableInteractivity(2f));

                if(onInvalidSubmissionAttempted != null)
                {
                    onInvalidSubmissionAttempted(invalidSubmissionMessage);
                }
            }
        }

        private System.Collections.IEnumerator DisableInteractivity(float seconds)
        {
            inputField.interactable = false;

            yield return new WaitForSeconds(seconds);

            inputField.interactable = true;
        }
    }
}
