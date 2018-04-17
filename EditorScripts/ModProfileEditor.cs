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
        private SerializedProperty editableModProfileProperty;

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
            editableModProfileProperty = serializedObject.FindProperty("editableModProfile");

            profile = ModManager.GetModProfile(modIdProperty.intValue);

            profileViewParts = CreateProfileViewParts();

            // Initialize View
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnEnable(editableModProfileProperty, profile);
            }
            scrollPos = Vector2.zero;
            isRepaintRequired = false;

            EditorApplication.update += OnUpdate;
        }

        protected virtual void OnDisable()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnDisable();
            }

            EditorApplication.update -= OnUpdate;
        }

        protected virtual IModProfileViewPart[] CreateProfileViewParts()
        {
            return new IModProfileViewPart[]
            {
                new ModProfileInfoViewPart(),
                new ModMediaViewPart(),
            };
        }

        // ------[ GUI ]------
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnGUI();
            }
            serializedObject.ApplyModifiedProperties();

            isRepaintRequired = false;
        }

        // ------[ UPDATE ]------
        public virtual void OnUpdate()
        {
            foreach(IModProfileViewPart viewPart in profileViewParts)
            {
                viewPart.OnUpdate();
                isRepaintRequired |= viewPart.IsRepaintRequired();
            }

            if(isRepaintRequired)
            {
                Repaint();
            }
        }
    }
}

#endif
