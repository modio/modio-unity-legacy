using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

// TODO(@jackson): Custom Editor
namespace ModIO.UI
{
    /// <summary>Component used to display a field of a mod statistics in text.</summary>
    public class ModStatisticsFieldDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>ModStatistics field to display.</summary>
        [SerializeField]
        private string m_fieldName = "modId";

        /// <summary>Delegate for acquiring the display string from the ModStatistics.</summary>
        private Func<ModStatistics, string> m_getStatisticsFieldValue = null;

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Currently displayed ModStatistics object.</summary>
        private ModStatistics m_statistics = null;

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            Component textDisplayComponent = GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

            this.m_getStatisticsFieldValue = this.GenerateGetDisplayStringDelegate();


            #if DEBUG
            if(this.m_getStatisticsFieldValue == null)
            {
                Debug.LogError("[mod.io] ModStatisticsFieldDisplay is unable to display the field \'"
                               + this.m_fieldName + "\' as it does not appear in the ModStatistics"
                               + " object definition.",
                               this);
            }
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                 + "GameObject to set text for."
                                 + "\nCompatible with any component that exposes a"
                                 + " publicly settable \'.text\' property.",
                                 this);
            }
            #endif
        }

        protected virtual void OnEnable()
        {
            this.DisplayStatistics(this.m_statistics);
        }

        // --- DELEGATE GENERATION ---
        protected virtual Func<ModStatistics, string> GenerateGetDisplayStringDelegate()
        {
            foreach(var fieldInfo in typeof(ModStatistics).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if(fieldInfo.Name.Equals(this.m_fieldName))
                {
                    if(fieldInfo.FieldType.IsValueType)
                    {
                        return (p) => ModStatisticsFieldDisplay.GetStatisticsFieldValueString_ValueType(p, fieldInfo);
                    }
                    else
                    {
                        return (p) => ModStatisticsFieldDisplay.GetStatisticsFieldValueString_Nullable(p, fieldInfo);
                    }
                }
            }

            return null;
        }

        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onStatisticsChanged.RemoveListener(DisplayStatistics);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onStatisticsChanged.AddListener(DisplayStatistics);
                this.DisplayStatistics(this.m_view.statistics);
            }
            else
            {
                this.DisplayStatistics(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given statistics.</summary>
        public void DisplayStatistics(ModStatistics statistics)
        {
            this.m_statistics = statistics;

            // early out
            if(this.m_getStatisticsFieldValue == null) { return; }

            // display
            string displayString = this.m_getStatisticsFieldValue(statistics);
            this.m_textComponent.text = displayString;
        }

        // ---------[ UTILITY ]---------
        protected static string GetStatisticsFieldValueString_ValueType(ModStatistics statistics, FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);

            if(statistics == null)
            {
                return string.Empty;
            }
            else
            {
                return fieldInfo.GetValue(statistics).ToString();
            }
        }

        protected static string GetStatisticsFieldValueString_Nullable(ModStatistics statistics, FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);

            if(statistics == null)
            {
                return string.Empty;
            }
            else
            {
                var fieldValue = fieldInfo.GetValue(statistics);
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
