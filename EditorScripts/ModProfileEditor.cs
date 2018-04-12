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
        // ------[ SERIALIZED PROPERTIES ]------
        private SerializedProperty modIdProperty;
        private SerializedProperty modEditsProperty;

        // ------[ EDITOR CACHING ]------
        private ModProfile profile;

        // ------[ VIEW INFORMATION ]------
        private IModProfileViewPart[] profileViewParts;
        protected Vector2 scrollPos;
        protected bool isRepaintRequired;


        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            ModManager.Initialize();

            // Grab Serialized Properties
            modIdProperty = serializedObject.FindProperty("modId");
            modEditsProperty = serializedObject.FindProperty("modEdits");

            profile = ModManager.GetModProfile(modIdProperty.intValue);

            profileViewParts = CreateProfileViewParts();

            // Initialize View
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnEnable(modEditsProperty, profile);
            }
            scrollPos = Vector2.zero;
            isRepaintRequired = false;

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
                new ModProfileInfoViewPart(),
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
            serializedObject.ApplyModifiedProperties();
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
