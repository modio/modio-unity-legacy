using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>A component that wraps various Text display components.</summary>
    [System.Serializable]
    public struct GenericTextComponent
    {
        // ---------[ STATICS ]---------
        /// <summary>Returns the first compatible component found on the GameObject.</summary>
        public static Object FindCompatibleTextComponent(GameObject gameObject)
        {
            Object textComponent = null;

            if(gameObject != null)
            {
                textComponent = gameObject.GetComponent<TMPro.TMP_Text>();
                if(textComponent == null)
                {
                    textComponent = gameObject.GetComponent<Text>();
                }
                if(textComponent == null)
                {
                    textComponent = gameObject.GetComponent<TextMesh>();
                }
            }

            return textComponent;
        }

        // ---------[ FIELDS ]---------
        /// <summary>The text to display on the UI component.</summary>
        public string text
        {
            get
            {
                if(this.m_getTextDelegate == null)
                {
                    if(this.m_textDisplayComponent is TMPro.TMP_Text)
                    {
                        var castComponent = (TMPro.TMP_Text)this.m_textDisplayComponent;
                        this.m_getTextDelegate = () =>
                        {
                            return castComponent.text;
                        };
                    }
                    else if(this.m_textDisplayComponent is Text)
                    {
                        var castComponent = (Text)this.m_textDisplayComponent;
                        this.m_getTextDelegate = () =>
                        {
                            return castComponent.text;
                        };
                    }
                    else if(this.m_textDisplayComponent is TextMesh)
                    {
                        var castComponent = (TextMesh)this.m_textDisplayComponent;
                        this.m_getTextDelegate = () =>
                        {
                            return castComponent.text;
                        };
                    }
                    else
                    {
                        this.m_getTextDelegate = () => null;
                    }
                }

                return this.m_getTextDelegate();
            }

            set
            {
                if(this.m_setTextDelegate == null)
                {
                    if(this.m_textDisplayComponent is TMPro.TMP_Text)
                    {
                        var castComponent = (TMPro.TMP_Text)this.m_textDisplayComponent;
                        this.m_setTextDelegate = (s) =>
                        {
                            castComponent.text = s;
                        };
                    }
                    else if(this.m_textDisplayComponent is Text)
                    {
                        var castComponent = (Text)this.m_textDisplayComponent;
                        this.m_setTextDelegate = (s) =>
                        {
                            castComponent.text = s;
                        };
                    }
                    else if(this.m_textDisplayComponent is TextMesh)
                    {
                        var castComponent = (TextMesh)this.m_textDisplayComponent;
                        this.m_setTextDelegate = (s) =>
                        {
                            castComponent.text = s;
                        };
                    }
                    else
                    {
                        this.m_setTextDelegate = (s) => {};
                    }
                }

                this.m_setTextDelegate(value);
            }
        }

        /// <summary>The component the this behaviour uses to display text.</summary>
        [SerializeField]
        private Object m_textDisplayComponent;

        /// <summary>The delegate for displaying text.</summary>
        private System.Action<string> m_setTextDelegate;

        /// <summary>The delegate for getting the displayed text.</summary>
        private System.Func<string> m_getTextDelegate;
    }
}
