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
        /// <summary>Mod id of the mod being reported.</summary>
        private int m_modId = ModProfile.NULL_ID;

        // --- Accessors ---
        /// <summary>Gets the canvas group attached to this gameObject.</summary>
        CanvasGroup IBrowserView.canvasGroup { get { return this.gameObject.GetComponent<CanvasGroup>(); } }

        /// <summary>Reset selection on hide.</summary>
        bool IBrowserView.resetSelectionOnHide { get { return true; } }

        /// <summary>Is the view a root view or window view?</summary>
        bool IBrowserView.isRootView { get { return false; } }

        // ---------[ UI Control ]---------
        /// <summary>Sets the mod id for the mod being reported.</summary>
        public void SetModId(int modId)
        {
            if(this.m_modId != modId)
            {
                ModView view = this.GetComponent<ModView>();
                if(view != null)
                {
                    view.profile = null;

                    ModProfileRequestManager.instance.RequestModProfile(modId, this.SetModProfile, null);
                }

                this.m_modId = modId;
            }
        }

        /// <summary>Sets the mod profile for the mod being reported.</summary>
        public void SetModProfile(ModProfile profile)
        {
            ModView view = this.GetComponent<ModView>();
            if(view != null)
            {
                view.profile = profile;
            }

            int newModId = ModProfile.NULL_ID;
            if(profile != null)
            {
                newModId = profile.id;
            }

            this.m_modId = newModId;
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
