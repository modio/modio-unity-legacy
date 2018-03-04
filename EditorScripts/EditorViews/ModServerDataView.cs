#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class ModServerDataView : IEditorView
    {
        public string GetDisplayName() { return "Files"; }

        public static void ModServerDataPanel(SerializedProperty modInfoProp)
        {
            SerializedProperty modObjectProp = modInfoProp.FindPropertyRelative("_data");

            // --- Read-only Data ---
            using (new EditorGUI.DisabledScope(true))
            {
                int modId = modObjectProp.FindPropertyRelative("id").intValue;
                if(modId <= 0)
                {
                    EditorGUILayout.LabelField("ModIO ID",
                                               "Not yet uploaded");
                }
                else
                {
                    EditorGUILayout.LabelField("ModIO ID",
                                               modId.ToString());
                    
                    EditorGUILayout.LabelField("Submitted By",
                                               modObjectProp.FindPropertyRelative("submitted_by.username").stringValue);

                    ModInfo.Status modStatus = (ModInfo.Status)modObjectProp.FindPropertyRelative("status").intValue;
                    EditorGUILayout.LabelField("Status",
                                               modStatus.ToString());

                    string ratingSummaryDisplay
                        = modObjectProp.FindPropertyRelative("rating_summary.weighted_aggregate").floatValue.ToString("0.00")
                        + " aggregate score. (From "
                        + modObjectProp.FindPropertyRelative("rating_summary.total_ratings").intValue.ToString()
                        + " ratings)";

                    EditorGUILayout.LabelField("Rating Summary",
                                                ratingSummaryDisplay);

                    EditorGUILayout.LabelField("Date Uploaded",
                                               modObjectProp.FindPropertyRelative("date_added").intValue.ToString());
                    EditorGUILayout.LabelField("Date Updated",
                                               modObjectProp.FindPropertyRelative("date_updated").intValue.ToString());
                    EditorGUILayout.LabelField("Date Live",
                                               modObjectProp.FindPropertyRelative("date_live").intValue.ToString());
                }
            }
        }
    }
}

#endif
