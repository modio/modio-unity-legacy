using System;
using UnityEngine;
using UnityEngine.UI;
using ModIO;


public class LoginDialog : MonoBehaviour
{
    // public event Action<UserProfile> onUserLoginSucceeded;

    public InputField inputField;
    public Button submitButton;

    public void OnTextInputUpdated()
    {
        Debug.Log("Text Updated");

        string trimmedInput = inputField.text.Trim();

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
}
