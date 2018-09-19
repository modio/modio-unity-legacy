using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class LoginDialog : MonoBehaviour
{
    public event Action<string> onUserOAuthTokenReceived;

    public InputField inputField;
    public Button submitButton;

    private void Start()
    {
        inputField.onEndEdit.AddListener(val =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSubmitButtonClicked();
            }
        });
    }

    public void Initialize()
    {
        inputField.text = string.Empty;

        submitButton.GetComponentInChildren<Text>().text = "Invalid Email/Security Code";
        submitButton.interactable = false;
    }

    public void OnTextInputUpdated()
    {
        string trimmedInput = inputField.text.Trim().Replace(" ", "");

        if(Utility.IsEmail(trimmedInput))
        {
            submitButton.GetComponentInChildren<Text>().text = "Request Security Code";
            submitButton.interactable = true;
        }
        else if(Utility.IsSecurityCode(trimmedInput))
        {
            submitButton.GetComponentInChildren<Text>().text = "Authorize";
            submitButton.interactable = true;
        }
        else
        {
            submitButton.GetComponentInChildren<Text>().text = "Invalid Email/Security Code";
            submitButton.interactable = false;
        }
    }

    public void OnSubmitButtonClicked()
    {
        string trimmedInput = inputField.text.Trim();

        inputField.interactable = false;
        submitButton.interactable = false;

        Action<WebRequestError> onError = (e) =>
        {
            Debug.LogWarning(e.ToUnityDebugString());

            inputField.interactable = true;

            submitButton.interactable = true;
        };

        if(Utility.IsEmail(trimmedInput))
        {
            Action<APIMessage> onGetSecurityCode = (m) =>
            {
                Debug.Log(m.message);

                inputField.text = string.Empty;
                inputField.interactable = true;

                submitButton.interactable = true;
            };

            APIClient.SendSecurityCode(trimmedInput,
                                       onGetSecurityCode,
                                       onError);
        }
        else if(Utility.IsSecurityCode(trimmedInput))
        {
            Action<string> onOAuthTokenReceived = (t) =>
            {
                inputField.text = string.Empty;
                inputField.interactable = true;

                submitButton.interactable = true;

                if(onUserOAuthTokenReceived != null)
                {
                    onUserOAuthTokenReceived(t);
                }
            };

            APIClient.GetOAuthToken(trimmedInput.ToUpper(),
                                    onOAuthTokenReceived,
                                    onError);
        }
    }
}
