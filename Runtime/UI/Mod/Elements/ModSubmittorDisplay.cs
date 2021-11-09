using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A display component that pairs a UserView with a ModView to display the
    /// submittor.</summary>
    [RequireComponent(typeof(UserView))]
    public class ModSubmittorDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        // ---------[ INITIALIZATION ]---------
        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayModSubmittor);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayModSubmittor);
                this.DisplayModSubmittor(this.m_view.profile);
            }
            else
            {
                this.DisplayModSubmittor(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the submittor for a ModProfile.</summary>
        public void DisplayModSubmittor(ModProfile modProfile)
        {
            UserProfile userProfile = null;
            if(modProfile != null)
            {
                userProfile = modProfile.submittedBy;
            }

            this.gameObject.GetComponent<UserView>().profile = userProfile;
        }
    }
}
