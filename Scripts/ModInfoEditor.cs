#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ModIO
{
    [CustomEditor(typeof(EditableModInfo))]
    public class ModInfoEditor : Editor
    {
        public static void DisplayAsObject(SerializedObject serializedModInfo)
        {
            serializedModInfo.Update();
            
            SerializedProperty modObjectProp = serializedModInfo.FindProperty("_data");
            DisplayInner(modObjectProp);

            serializedModInfo.ApplyModifiedProperties();
        }

        public static void DisplayAsProperty(SerializedProperty serializedModInfo)
        {
            DisplayInner(serializedModInfo.FindPropertyRelative("_data"));
        }

        private static void DisplayInner(SerializedProperty modObjectProp)
        {
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name"),
                                          new GUIContent("Name"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("name_id"),
                                          new GUIContent("Name-ID"));

            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("status"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("visible"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("logo"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("homepage"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("summary"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("modfile"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("stock"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("metadata_blob"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("tags"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("description"));
            EditorGUILayout.PropertyField(modObjectProp.FindPropertyRelative("media"));

            // TODO(@jackson): Do section header or foldout
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
                EditorGUILayout.LabelField("ModIO URL",
                                           modObjectProp.FindPropertyRelative("profile_url").stringValue);
                
                EditorGUILayout.LabelField("Submitted By",
                                           modObjectProp.FindPropertyRelative("submitted_by").FindPropertyRelative("username").stringValue);

                string ratingSummaryDisplay
                    = modObjectProp.FindPropertyRelative("rating_summary").FindPropertyRelative("weighted_aggregate").floatValue.ToString("0.00")
                    + " aggregate score. (From "
                    + modObjectProp.FindPropertyRelative("rating_summary").FindPropertyRelative("total_ratings").intValue.ToString()
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

        public override void OnInspectorGUI()
        {
            DisplayAsObject(serializedObject);
        }
    }
}

#endif