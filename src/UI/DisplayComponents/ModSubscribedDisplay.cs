using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component used to display the subscribed state of a mod.</summary>
    [RequireComponent(typeof(StateToggleDisplay))]
    public class ModSubscribedDisplay : MonoBehaviour, IModViewElement, IModSubscriptionsUpdateReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Mod Id for the mod being referenced.</summary>
        private int m_modId = ModProfile.NULL_ID;

        // ---------[ INITIALIZATION ]---------
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayModSubscribed);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayModSubscribed);
                this.DisplayModSubscribed(this.m_view.profile);
            }
            else
            {
                this.DisplayModSubscribed(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the subscribed state of a mod.</summary>
        public void DisplayModSubscribed(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            if(profile != null)
            {
                modId = profile.id;
            }

            this.DisplayModSubscribed(modId);
        }

        /// <summary>Displays the subscribed state of a mod.</summary>
        public void DisplayModSubscribed(int modId)
        {
            this.m_modId = modId;

            // display
            bool isSubscribed = ModManager.GetSubscribedModIds().Contains(modId);

            this.gameObject.GetComponent<StateToggleDisplay>().isOn = isSubscribed;
        }

        // ---------[ EVENTS ]---------
        /// <summary>IModSubscriptionsUpdateReceiver interface</summary>
        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            if(addedSubscriptions.Contains(this.m_modId))
            {
                this.gameObject.GetComponent<StateToggleDisplay>().isOn = true;
            }
            else if(removedSubscriptions.Contains(this.m_modId))
            {
                this.gameObject.GetComponent<StateToggleDisplay>().isOn = false;
            }
        }
    }
}
