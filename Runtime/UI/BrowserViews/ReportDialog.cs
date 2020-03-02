using System;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>A view for allowing a player to report a mod.</summary>
    public class ReportDialog : MonoBehaviour, IBrowserView, ICancelHandler
    {
        // ---------[ Fields ]---------
        // --- Accessors ---
        /// <summary>Gets the canvas group attached to this gameObject.</summary>
        CanvasGroup IBrowserView.canvasGroup { get { return this.gameObject.GetComponent<CanvasGroup>(); } }

        /// <summary>Reset selection on hide.</summary>
        bool IBrowserView.resetSelectionOnHide { get { return true; } }

        /// <summary>Is the view a root view or window view?</summary>
        bool IBrowserView.isRootView { get { return false; } }

        // ---------[ UI Control ]---------
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
