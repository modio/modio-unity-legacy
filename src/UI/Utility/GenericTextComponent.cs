namespace ModIO.UI
{
    /// <summary>A component that wraps various Text display components.</summary>
    [System.Serializable]
    public struct GenericTextComponent
    {
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
                    else if(this.m_textDisplayComponent is UnityEngine.UI.Text)
                    {
                        var castComponent = (UnityEngine.UI.Text)this.m_textDisplayComponent;
                        this.m_getTextDelegate = () =>
                        {
                            return castComponent.text;
                        };
                    }
                    else if(this.m_textDisplayComponent is UnityEngine.TextMesh)
                    {
                        var castComponent = (UnityEngine.TextMesh)this.m_textDisplayComponent;
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
                    else if(this.m_textDisplayComponent is UnityEngine.UI.Text)
                    {
                        var castComponent = (UnityEngine.UI.Text)this.m_textDisplayComponent;
                        this.m_setTextDelegate = (s) =>
                        {
                            castComponent.text = s;
                        };
                    }
                    else if(this.m_textDisplayComponent is UnityEngine.TextMesh)
                    {
                        var castComponent = (UnityEngine.TextMesh)this.m_textDisplayComponent;
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
        [UnityEngine.SerializeField]
        private UnityEngine.Object m_textDisplayComponent;

        /// <summary>The delegate for displaying text.</summary>
        private System.Action<string> m_setTextDelegate;

        /// <summary>The delegate for getting the displayed text.</summary>
        private System.Func<string> m_getTextDelegate;
    }
}
