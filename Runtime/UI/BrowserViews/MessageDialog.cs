using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>A view for displaying a message to the player.</summary>
    public class MessageDialog : MonoBehaviour, IBrowserView, ICancelHandler
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Data structure used for defining the details of the message dialog.</summary>
        public struct Data
        {
            /// <summary>Text to display as the dialog header.</summary>
            public string header;

            /// <summary>Text to display as the dialog message.</summary>
            public string message;

            /// <summary>Callback to execute when the highlight button is pressed.</summary>
            public Action highlightButtonCallback;

            /// <summary>Text to display on the highlight button.</summary>
            public string highlightButtonText;

            /// <summary>Callback to execute when the warning button is pressed.</summary>
            public Action warningButtonCallback;

            /// <summary>Text to display on the warning button.</summary>
            public string warningButtonText;

            /// <summary>Callback to execute when the standard button is pressed.</summary>
            public Action standardButtonCallback;

            /// <summary>Text to display on the standard button.</summary>
            public string standardButtonText;

            /// <summary>Callback if dialog is closed.</summary>
            public Action onClose;
        }

        // ---------[ Fields ]---------
        /// <summary>Text field for displaying the header text.</summary>
        public GenericTextComponent headerText = new GenericTextComponent();

        /// <summary>Text field for displaying the message text.</summary>
        public GenericTextComponent messageText = new GenericTextComponent();

        /// <summary>Highlighted button.</summary>
        public Button highlightedButton = null;

        /// <summary>Highlighted button text component.</summary>
        public GenericTextComponent highlightedButtonText = new GenericTextComponent();

        /// <summary>Highlighted button callback.</summary>
        public Action highlightedButtonCallback = null;

        /// <summary>Warning-themed button.</summary>
        public Button warningButton = null;

        /// <summary>Warning-themed button text component.</summary>
        public GenericTextComponent warningButtonText = new GenericTextComponent();

        /// <summary>Warning-themed button callback.</summary>
        public Action warningButtonCallback = null;

        /// <summary>Standard-themed button.</summary>
        public Button standardButton = null;

        /// <summary>Standard-themed button text component.</summary>
        public GenericTextComponent standardButtonText = new GenericTextComponent();

        /// <summary>Standard-themed button callback.</summary>
        public Action standardButtonCallback = null;

        /// <summary>Closing dialog callback.</summary>
        public Action onClose = null;

        // --- Accessors ---
        /// <summary>Gets the canvas group attached to this gameObject.</summary>
        CanvasGroup IBrowserView.canvasGroup
        {
            get {
                return this.gameObject.GetComponent<CanvasGroup>();
            }
        }

        /// <summary>Reset selection on hide.</summary>
        bool IBrowserView.resetSelectionOnHide
        {
            get {
                return true;
            }
        }

        /// <summary>Is the view a root view or window view?</summary>
        bool IBrowserView.isRootView
        {
            get {
                return false;
            }
        }

        /// <summary>The priority to focus the selectables.</summary>
        private List<Selectable> m_buttonPriority = null;

        /// <summary>The priority to focus the selectables.</summary>
        List<Selectable> IBrowserView.onFocusPriority
        {
            get {
                return this.m_buttonPriority;
            }
        }

        // ---------[ Initialization ]---------
        /// <summary>Builds priority list.</summary>
        private void Awake()
        {
            this.m_buttonPriority = new List<Selectable>() {
                this.highlightedButton,
                this.standardButton,
                this.warningButton,
            };
        }

        /// <summary>Hooks up button callbacks.</summary>
        private void Start()
        {
            if(this.highlightedButton != null)
            {
                this.highlightedButton.onClick.AddListener(() => {
                    if(this.highlightedButtonCallback != null)
                    {
                        this.highlightedButtonCallback();
                    }
                });
            }
            if(this.warningButton != null)
            {
                this.warningButton.onClick.AddListener(() => {
                    if(this.warningButtonCallback != null)
                    {
                        this.warningButtonCallback();
                    }
                });
            }
            if(this.standardButton != null)
            {
                this.standardButton.onClick.AddListener(() => {
                    if(this.standardButtonCallback != null)
                    {
                        this.standardButtonCallback();
                    }
                });
            }
        }

        /// <summary>Calls the onClose callback</summary>
        private void OnDisable()
        {
            if(this.onClose != null)
            {
                this.onClose.Invoke();
            }
        }

        // ---------[ UI Control ]---------
        /// <summary>Applies the data to the message dialog.</summary>
        public void ApplyData(Data data)
        {
            if(this.headerText.displayComponent != null)
            {
                this.headerText.text = data.header;
            }
            if(this.messageText.displayComponent != null)
            {
                this.messageText.text = data.message;
            }
            if(this.highlightedButtonText.displayComponent != null)
            {
                this.highlightedButtonText.text = data.highlightButtonText;
            }
            if(this.highlightedButton != null)
            {
                this.highlightedButtonCallback = data.highlightButtonCallback;
                this.highlightedButton.gameObject.SetActive(data.highlightButtonCallback != null);
            }
            if(this.warningButtonText.displayComponent != null)
            {
                this.warningButtonText.text = data.warningButtonText;
            }
            if(this.warningButton != null)
            {
                this.warningButtonCallback = data.warningButtonCallback;
                this.warningButton.gameObject.SetActive(data.warningButtonCallback != null);
            }
            if(this.standardButtonText.displayComponent != null)
            {
                this.standardButtonText.text = data.standardButtonText;
            }
            if(this.standardButton != null)
            {
                this.standardButtonCallback = data.standardButtonCallback;
                this.standardButton.gameObject.SetActive(data.standardButtonCallback != null);
            }

            this.onClose = data.onClose;
        }

        /// <summary>ICancelHandler interface to pass through to the cancel button.</summary>
        public void OnCancel(BaseEventData eventData)
        {
            Close();
        }

        /// <summary>Closes the dialog window.</summary>
        public void Close()
        {
            ViewManager.instance.CloseWindowedView(this);
        }
    }
}
