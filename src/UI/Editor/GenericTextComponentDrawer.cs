#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomPropertyDrawer(typeof(GenericTextComponent))]
    public class GenericTextComponentDrawer : UnityEditor.PropertyDrawer
    {
        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nestedComponentProperty = property.FindPropertyRelative("m_textDisplayComponent");

            Object displayObject = nestedComponentProperty.objectReferenceValue;
            Object assignedObject = EditorGUI.ObjectField(position, label, displayObject, typeof(GameObject), true);

            if(assignedObject != displayObject)
            {
                Object selectedComponent = null;
                GameObject assignedGameObject = assignedObject as GameObject;

                if(assignedGameObject == null)
                {
                    nestedComponentProperty.objectReferenceValue = null;
                }
                else
                {
                    selectedComponent = assignedGameObject.GetComponent<TMPro.TMP_Text>();
                    if(selectedComponent == null)
                    {
                        selectedComponent = assignedGameObject.GetComponent<UnityEngine.UI.Text>();
                    }
                    if(selectedComponent == null)
                    {
                        selectedComponent = assignedGameObject.GetComponent<UnityEngine.TextMesh>();
                    }

                    if(selectedComponent != null)
                    {
                        nestedComponentProperty.objectReferenceValue = selectedComponent;
                    }
                }
            }
        }
    }
}

#endif
