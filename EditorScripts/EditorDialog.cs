#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public static class EditorDialog
    {
        public static bool ConfirmLogOut(string username)
        {
            return EditorUtility.DisplayDialog("Confirm mod.io account logout",
                                               "Do you wish to log out, " + username + "?",
                                               "Log Out",
                                               "Cancel");
        }
    }
}
#endif
