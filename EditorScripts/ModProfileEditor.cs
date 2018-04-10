#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ModIO
{
    // TODO(@jackson): Implement Login Dialog
    // TODO(@jackson): Needs beauty-pass
    // TODO(@jackson): Force repaint on Callbacks
    // TODO(@jackson): Implement client-side error-checking in submission
    // TODO(@jackson): Check if undos are necessary

    [CustomEditor(typeof(ScriptableModProfile))]
    public class ModProfileEditor : Editor
    {
        // ------[ EDITOR CACHING ]------
        private ModProfile profile;

        // ------[ VIEW INFORMATION ]------
        private IModProfileViewPart[] profileViewParts;
        protected Vector2 scrollPos;
        protected bool isRepaintRequired;

        // ------[ SERIALIZED PROPERTIES ]------
        private SerializedProperty modIdProperty;
        private SerializedProperty modEditsProperty;

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            // TODO(@jackson): Attempt profile collection

            profileViewParts = CreateProfileViewParts();

            // Initialize View
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnEnable(serializedObject, profile);
            }
            scrollPos = Vector2.zero;
            isRepaintRequired = false;

            // Grab Serialized Properties
            modIdProperty = serializedObject.FindProperty("modId");
            modEditsProperty = serializedObject.FindProperty("modEdits");
        }

        protected virtual void OnDisable()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnDisable();
            }
        }

        protected virtual IModProfileViewPart[] CreateProfileViewParts()
        {
            return new IModProfileViewPart[]
            {
                null,
            };
        }

        // ------[ ONGUI ]------
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnGUI();
            }
        }

        // ------[ ONUPDATE ]------
        public virtual void OnInspectorUpdate()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnUpdate();
            }

            if(isRepaintRequired)
            {
                Repaint();
                isRepaintRequired = false;
            }
        }
    }
}

#endif
