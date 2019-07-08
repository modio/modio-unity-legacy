using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component for easily displaying a mod logo.</summary>
    [RequireComponent(typeof(Image))]
    public class ModLogoDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Preferred Logo Size.</summary>
        public LogoSize logoSize = LogoSize.Original;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Current modId for the displayed logo.</summary>
        private int m_modId = ModProfile.NULL_ID;

        // --- ACCESSORS ---
        /// <summary>Image component to display with.</summary>
        protected virtual Image image
        {
            get { return this.GetComponent<Image>(); }
        }

        // ---------[ INITIALIZATION ]---------
        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayProfile;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayProfile;
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays tags of a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            int newId = ModProfile.NULL_ID;
            if(profile != null)
            {
                newId = profile.id;
            }

            if(this.m_modId != newId)
            {
                this.image.enabled = false;
                this.m_modId = newId;

                if(newId != ModProfile.NULL_ID
                   && profile.logoLocator != null)
                {
                    ImageRequestManager.instance.RequestModLogo(newId, profile.logoLocator, this.logoSize,
                                                                (t) => ApplyTexture(newId, t),
                                                                (t) => ApplyTexture(newId, t), // fallback
                                                                WebRequestError.LogAsWarning);
                }
            }
        }

        /// <summary>Internal function for applying the texture.</summary>
        protected virtual void ApplyTexture(int modId, Texture2D texture)
        {
            if(this != null
               && modId == this.m_modId
               && texture != null)
            {
                this.image.sprite = UIUtilities.CreateSpriteFromTexture(texture);
                this.image.enabled = true;
            }
        }
    }
}
