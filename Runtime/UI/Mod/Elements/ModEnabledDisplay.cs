using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component used to display the state of a mod being enabled/disabled.</summary>
    [RequireComponent(typeof(StateToggleDisplay))]
    public class ModEnabledDisplay : MonoBehaviour,
                                     IModViewElement,
                                     IModEnabledReceiver,
                                     IModDisabledReceiver
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
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayModEnabled);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayModEnabled);
                this.DisplayModEnabled(this.m_view.profile);
            }
            else
            {
                this.DisplayModEnabled(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the enabled state of a mod.</summary>
        public void DisplayModEnabled(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            if(profile != null)
            {
                modId = profile.id;
            }

            this.DisplayModEnabled(modId);
        }

        /// <summary>Displays the enabled state of a mod.</summary>
        public void DisplayModEnabled(int modId)
        {
            bool isEnabled = LocalUser.EnabledModIds.Contains(modId);
            this.DisplayModEnabled(modId, isEnabled);
        }

        /// <summary>Displays the given subscribed state of a mod.</summary>
        public void DisplayModEnabled(int modId, bool isEnabled)
        {
            this.m_modId = modId;

            foreach(StateToggleDisplay display in this.gameObject
                        .GetComponents<StateToggleDisplay>())
            {
                display.isOn = isEnabled;
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>IModEnabledReceiver interface</summary>
        public void OnModEnabled(int modId)
        {
            if(modId == this.m_modId)
            {
                this.DisplayModEnabled(modId, true);
            }
        }

        /// <summary>IModDisabledReceiver interface</summary>
        public void OnModDisabled(int modId)
        {
            if(modId == this.m_modId)
            {
                this.DisplayModEnabled(modId, false);
            }
        }
    }
}
