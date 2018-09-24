using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class LoginDialog : MonoBehaviour
{
    public event Action<APIMessage> onSecurityCodeSent;
    public event Action<string> onUserOAuthTokenReceived;
    public event Action<WebRequestError> onAPIRequestError;

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


        this.onSecurityCodeSent += (m) =>
        {
            inputField.text = string.Empty;
            inputField.interactable = true;

            submitButton.GetComponentInChildren<Text>().text = "Invalid Email/Security Code";
        };

        this.onUserOAuthTokenReceived += (t) =>
        {
            inputField.text = string.Empty;
            inputField.interactable = true;

            submitButton.GetComponentInChildren<Text>().text = "Invalid Email/Security Code";

            Debug.Log("onUserOAuthTokenReceived");
        };

        this.onAPIRequestError += (e) =>
        {
            inputField.interactable = true;
            submitButton.interactable = true;
        };
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
    }
}
