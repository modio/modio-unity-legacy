using System.Reflection;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A component that wraps any component with a ".text" property.</summary>
    [System.Serializable]
    public struct GenericTextComponent
    {
        // ---------[ STATICS ]---------
        /// <summary>Returns the first compatible component found on the GameObject.</summary>
        public static Component FindCompatibleTextComponent(GameObject gameObject)
        {
            Component textComponent = null;

            if(gameObject != null)
            {
                Component[] objectComponents = gameObject.GetComponents<Component>();

                foreach(Component component in objectComponents)
                {
                    var componentType = component.GetType();
                    var propertyInfo = componentType.GetProperty(
                        "text",
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if(propertyInfo != null && propertyInfo.PropertyType == typeof(string)
                       && propertyInfo.GetGetMethod() != null
                       && propertyInfo.GetSetMethod() != null)
                    {
                        textComponent = component;
                        break;
                    }
                }
            }

            return textComponent;
        }

        // ---------[ FIELDS ]---------
        /// <summary>The component the this structure uses to display text.</summary>
        [SerializeField]
        private Component m_textDisplayComponent;

        /// <summary>The delegate for displaying text.</summary>
        private System.Action<Component, string> m_setTextDelegate;

        /// <summary>The delegate for getting the displayed text.</summary>
        private System.Func<Component, string> m_getTextDelegate;

        // --- Accessors ---
        /// <summary>The component the this structure uses to display text.</summary>
        public Component displayComponent
        {
            get {
                return this.m_textDisplayComponent;
            }
        }

        /// <summary>The text to display on the UI component.</summary>
        public string text
        {
            get {
                if(this.m_getTextDelegate == null)
                {
                    GenerateDelegates();
                }

                return this.m_getTextDelegate(this.m_textDisplayComponent);
            }

            set {
                if(this.m_setTextDelegate == null)
                {
                    GenerateDelegates();
                }

                this.m_setTextDelegate(this.m_textDisplayComponent, value);
            }
        }

        // ---------[ UTILITIES ]---------
        /// <summary>Sets the component to use in displaying text.</summary>
        public void SetTextDisplayComponent(Component displayComponent)
        {
            if(displayComponent != this.m_textDisplayComponent)
            {
                this.m_textDisplayComponent = displayComponent;
                this.m_setTextDelegate = null;
                this.m_getTextDelegate = null;
            }
        }

        /// <summary>Creates the get/set delegates.</summary>
        private void GenerateDelegates()
        {
            PropertyInfo propertyInfo = null;
            if(this.m_textDisplayComponent != null)
            {
                var componentType = this.m_textDisplayComponent.GetType();
                propertyInfo = componentType.GetProperty(
                    "text", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }

            if(propertyInfo != null && propertyInfo.PropertyType == typeof(string)
               && propertyInfo.GetGetMethod() != null && propertyInfo.GetSetMethod() != null)
            {
                this.m_getTextDelegate = (component) =>
                { return propertyInfo.GetValue(component, null) as string; };
                this.m_setTextDelegate = (component, s) =>
                { propertyInfo.SetValue(component, s, null); };
            }
            else
            {
                this.m_getTextDelegate = (component) => null;
                this.m_setTextDelegate = (component, s) =>
                {};
            }
        }
    }
}
