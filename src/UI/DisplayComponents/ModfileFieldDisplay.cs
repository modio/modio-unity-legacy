using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a modfile in text.</summary>
    public class ModfileFieldDisplay : MonoBehaviour, IModfileViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Modfile field to display.</summary>
        [SerializeField]
        private string m_fieldName = "id";

        /// <summary>Delegate for acquiring the display string from the Modfile.</summary>
        private Func<Modfile, string> m_getModfileFieldValue = null;

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModfileView.</summary>
        private ModfileView m_view = null;

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            this.m_getModfileFieldValue = this.GenerateGetDisplayStringDelegate();
            UnityEngine.Object textDisplayComponent = GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

            #if DEBUG
            if(this.m_getModfileFieldValue == null)
            {
                Debug.LogError("[mod.io] ModfileFieldDisplay is unable to display the field \'"
                               + this.m_fieldName + "\' as it does not appear in the Modfile"
                               + " object definition.",
                               this);
            }
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                 + "GameObject to set text for."
                                 + "\nCompatible components are UnityEngine.UI.Text, "
                                 + "UnityEngine.TextMesh, and components derived from TMPro.TMP_Text.",
                                 this);
            }
            #endif
        }

        protected virtual void Start()
        {
            if(this.m_view != null)
            {
                this.DisplayModfile(this.m_view.modfile);
            }
            else
            {
                this.DisplayModfile(null);
            }
        }

        // --- DELEGATE GENERATION ---
        protected virtual Func<Modfile, string> GenerateGetDisplayStringDelegate()
        {
            foreach(var fieldInfo in typeof(Modfile).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if(fieldInfo.Name.Equals(this.m_fieldName))
                {
                    if(fieldInfo.FieldType.IsValueType)
                    {
                        return (m) => ModfileFieldDisplay.GetModfileFieldValueString_ValueType(m, fieldInfo);
                    }
                    else
                    {
                        return (m) => ModfileFieldDisplay.GetModfileFieldValueString_Nullable(m, fieldInfo);
                    }
                }
            }

            return null;
        }

        // --- IModfileViewElement Interface ---
        /// <summary>IModfileViewElement interface.</summary>
        public void SetModfileView(ModfileView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onModfileChanged -= DisplayModfile;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onModfileChanged += DisplayModfile;
                this.DisplayModfile(this.m_view.modfile);
            }
            else
            {
                this.DisplayModfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given modfile.</summary>
        public void DisplayModfile(Modfile modfile)
        {
            // early out
            if(this.m_getModfileFieldValue == null) { return; }

            // display
            string displayString = this.m_getModfileFieldValue(modfile);
            this.m_textComponent.text = displayString;
        }

        // ---------[ UTILITY ]---------
        protected static string GetModfileFieldValueString_ValueType(Modfile modfile, FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);

            if(modfile == null)
            {
                return string.Empty;
            }
            else
            {
                return fieldInfo.GetValue(modfile).ToString();
            }
        }

        protected static string GetModfileFieldValueString_Nullable(Modfile modfile, FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);

            if(modfile == null)
            {
                return string.Empty;
            }
            else
            {
                var fieldValue = fieldInfo.GetValue(modfile);
                if(fieldValue == null)
                {
                    return string.Empty;
                }
                else
                {
                    return fieldValue.ToString();
                }
            }
        }
    }
}
