using System;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>A view for allowing a player to report a mod.</summary>
    public class ReportDialog : MonoBehaviour, IBrowserView, ICancelHandler
    {
        // ---------[ Constants ]---------
        /// <summary>URL for the mod.io report widget.</summary>
        public static readonly string REPORT_WIDGET_URL = @"https://mod.io/report/widget";

        // ---------[ Fields ]---------
        /// <summary>Mod id of the mod being reported.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>URL link for the report web widget.</summary>
        private string m_reportURL = string.Empty;

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
                this.m_modId = modId;

                if(modId == ModProfile.NULL_ID)
                {
                    this.SetModProfile(null);
                }
                else
                {
                    ModProfileRequestManager.instance.RequestModProfile(modId, this.SetModProfile, (e) => this.m_modId = ModProfile.NULL_ID);
                }
            }
        }

        /// <summary>Sets the mod profile for the mod being reported.</summary>
        public void SetModProfile(ModProfile profile)
        {
            if(profile == null)
            {
                this.m_modId = ModProfile.NULL_ID;
                this.m_reportURL = string.Empty;
            }
            else
            {
                this.m_modId = profile.id;

                if(string.IsNullOrEmpty(profile.profileURL))
                {
                    this.m_reportURL = string.Empty;
                }
                else
                {
                    this.m_reportURL = "?urls=" + profile.profileURL;
                }
            }

            ModView view = this.GetComponent<ModView>();
            if(view != null)
            {
                view.profile = profile;
            }
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

        /// <summary>Opens a web browser to the report form pre-filled with the mod URL.</summary>
        public void OpenReportWidgetInWebBrowser()
        {
            Application.OpenURL(ReportDialog.REPORT_WIDGET_URL + this.m_reportURL);
        }
    }
}
