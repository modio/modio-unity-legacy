using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class LoginDialog : MonoBehaviour
{
    // public event Action<string> onUserOAuthTokenReceived;

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
        Debug.Log("Text Updated");

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

        if(Utility.IsEmail(trimmedInput))
        {
            APIClient.SendSecurityCode(trimmedInput,
                                       (m) => { Debug.Log(m.message); },
                                       (e) => { Debug.Log(e.ToUnityDebugString()); });
        }
        else if(Utility.IsSecurityCode(trimmedInput))
        {
            APIClient.GetOAuthToken(trimmedInput.ToUpper(),
                                    (t) => { Debug.Log(t); },
                                    (e) => { Debug.Log(e.ToUnityDebugString()); });
        }
    }
}
