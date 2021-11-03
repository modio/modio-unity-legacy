using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A display component that pairs a ModfileView with a ModView to display the current
    /// build.</summary>
    [RequireComponent(typeof(ModfileView))]
    public class CurrentBuildDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        // ---------[ INITIALIZATION ]---------
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
                this.m_view.onProfileChanged.RemoveListener(DisplayCurrentBuild);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayCurrentBuild);
                this.DisplayCurrentBuild(this.m_view.profile);
            }
            else
            {
                this.DisplayCurrentBuild(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the current build for a ModProfile.</summary>
        public void DisplayCurrentBuild(ModProfile modProfile)
        {
            Modfile modfile = null;
            if(modProfile != null)
            {
                modfile = modProfile.currentBuild;
            }

            this.gameObject.GetComponent<ModfileView>().modfile = modfile;
        }
    }
}
