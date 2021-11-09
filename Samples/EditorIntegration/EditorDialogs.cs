#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ModIO.EditorCode
{
    public static class EditorDialogs
    {
        public static bool ConfirmLogOut(string username)
        {
            return EditorUtility.DisplayDialog(
                "Confirm mod.io account logout",
                "Do you wish to log out of the account \'" + username + "\'?", "Log Out", "Cancel");
        }
    }
}
#endif
