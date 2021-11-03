using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component used to display the user's rating for a mod.</summary>
    public class ModUserRatingDisplay : MonoBehaviour, IModViewElement, IModRatingAddedReceiver
    {
        // ---------[ FIELDS ]---------
        /// <summary>Display for a positive user rating.</summary>
        public StateToggleDisplay positiveRatingDisplay = null;

        /// <summary>Display for a negative user rating.</summary>
        public StateToggleDisplay negativeRatingDisplay = null;

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
                this.m_view.onProfileChanged.RemoveListener(DisplayModRating);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayModRating);
                this.DisplayModRating(this.m_view.profile);
            }
            else
            {
                this.DisplayModRating(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the subscribed state of a mod.</summary>
        public void DisplayModRating(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            if(profile != null)
            {
                modId = profile.id;
            }

            this.DisplayModRating(modId);
        }

        /// <summary>Displays the subscribed state of a mod.</summary>
        public void DisplayModRating(int modId)
        {
            this.m_modId = modId;

            // display
            ModRatingValue rating = ModBrowser.instance.GetModRating(modId);
            if(this.positiveRatingDisplay != null)
            {
                this.positiveRatingDisplay.isOn = (rating == ModRatingValue.Positive);
            }
            if(this.negativeRatingDisplay != null)
            {
                this.negativeRatingDisplay.isOn = (rating == ModRatingValue.Negative);
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>IModRatingAddedReceiver interface</summary>
        public void OnModRatingAdded(int modId, ModRatingValue rating)
        {
            if(modId == this.m_modId)
            {
                // display
                if(this.positiveRatingDisplay != null)
                {
                    this.positiveRatingDisplay.isOn = (rating == ModRatingValue.Positive);
                }
                if(this.negativeRatingDisplay != null)
                {
                    this.negativeRatingDisplay.isOn = (rating == ModRatingValue.Negative);
                }
            }
        }
    }
}
