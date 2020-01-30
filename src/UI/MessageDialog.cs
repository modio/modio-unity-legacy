using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>A view for displaying a message to the player.</summary>
    public class MessageDialog : MonoBehaviour, IBrowserView, ICancelHandler
    {
        // ---------[ Fields ]---------
        /// <summary>Event fired if the view is attempted to be closed through a cancel button-press.</summary>
        public event System.Action onCancelOut = null;

        /// <summary>Initial selection item.</summary>
        public GameObject primarySelection = null;

        /// <summary>Text field for displaying the message text.</summary>
        public GenericTextComponent messageText = new GenericTextComponent();

        /// <summary>Highlighted button.</summary>
        public Button highlightedButton = null;

        /// <summary>Highlighted button text component.</summary>
        public GenericTextComponent highlightedButtonText = new GenericTextComponent();

        /// <summary>Warning-themed button.</summary>
        public Button warningButton = null;

        /// <summary>Warning-themed button text component.</summary>
        public GenericTextComponent warningButtonText = new GenericTextComponent();

        /// <summary>Standard-themed button.</summary>
        public Button standardButton = null;

        /// <summary>Standard-themed button text component.</summary>
        public GenericTextComponent standardButtonText = new GenericTextComponent();

        // --- Accessors ---
        /// <summary>Gets the canvas group attached to this gameObject.</summary>
        CanvasGroup IBrowserView.canvasGroup { get { return this.gameObject.GetComponent<CanvasGroup>(); } }

        /// <summary>Initial selection item.</summary>
        GameObject IBrowserView.primarySelection { get { return this.primarySelection; } }

        /// <summary>Reset selection on hide.</summary>
        bool IBrowserView.resetSelectionOnHide { get { return true; } }

        /// ---------[ UI Control ]---------
        /// <summary>ICancelHandler interface to pass through to the cancel button.</summary>
        public void OnCancel(BaseEventData eventData)
        {
            if(this.onCancelOut != null)
            {
                this.onCancelOut.Invoke();
            }
        }

        /// <summary>Closes the dialog window.</summary>
        public void Close()
        {
            ViewManager.instance.CloseStackedView(this);
        }
    }
}
