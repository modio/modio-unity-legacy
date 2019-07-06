#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomPropertyDrawer(typeof(GenericTextComponentAttribute))]
    public class GenericTextComponentDrawer : UnityEditor.PropertyDrawer
    {
        // ---------[ CONSTANTS ]---------
        public const int HELP_BOX_HEIGHT = 40;
        public const string HELP_BOX_MESSAGE = ("This GameObject requires a UnityEngine.UI.Text,"
                                                + " UnityEngine.TextMesh, or TMPro.TMP_Text-based"
                                                + " component in order to function correctly.");

        // ---------[ FIELDS ]---------
        /// <summary>Indicates whether the OnGUI function should display the warning.</summary>
        private bool m_showWarning = false;

        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect controlPosition = position;
            if(this.m_showWarning)
            {
                controlPosition.height = base.GetPropertyHeight(property, label);

                Rect helpPosition = position;
                helpPosition.y = controlPosition.height + position.y;
                helpPosition.height = position.height - controlPosition.height;

                EditorGUI.HelpBox(helpPosition, HELP_BOX_MESSAGE, MessageType.Warning);
            }

            GameObject gameObject = property.objectReferenceValue as GameObject;
            Object displayObject = gameObject;

            if(gameObject != null)
            {
                var textComponent = gameObject.GetComponent<UnityEngine.UI.Text>();
                if(textComponent != null)
                {
                    displayObject = textComponent;
                }

                var textMesh = gameObject.GetComponent<UnityEngine.TextMesh>();
                if(textMesh != null)
                {
                    displayObject = textMesh;
                }

                var textMeshPro = gameObject.GetComponent<TMPro.TMP_Text>();
                if(textMeshPro != null)
                {
                    displayObject = textMeshPro;
                }
            }

            Object assignedObject = EditorGUI.ObjectField(controlPosition, label, displayObject, typeof(GameObject), true);
            if(assignedObject != displayObject)
            {
                property.objectReferenceValue = assignedObject;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float newHeight = EditorGUIUtility.singleLineHeight;

            GameObject gameObject = property.objectReferenceValue as GameObject;

            this.m_showWarning = false;

            if(gameObject != null
               && gameObject.GetComponent<UnityEngine.UI.Text>() == null
               && gameObject.GetComponent<UnityEngine.TextMesh>() == null
               && gameObject.GetComponent<TMPro.TMP_Text>() == null)
            {
                this.m_showWarning = true;
                newHeight += HELP_BOX_HEIGHT;
            }

            return newHeight;
        }
    }
}

#endif
