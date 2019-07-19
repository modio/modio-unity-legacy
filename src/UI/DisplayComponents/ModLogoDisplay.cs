using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Component for easily displaying a mod logo.</summary>
    public class ModLogoDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Image component used to display the logo.</summary>
        public Image image = null;

        /// <summary>Preferred Logo Size.</summary>
        public LogoSize logoSize = LogoSize.Original;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Current modId for the displayed logo.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>Locator for the displayed logo.</summary>
        private LogoImageLocator m_locator = null;

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
        /// <summary>Displays the logo of a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            LogoImageLocator locator = null;
            if(profile != null)
            {
                modId = profile.id;
                locator = profile.logoLocator;
            }

            this.DisplayLogo(modId, locator);
        }

        /// <summary>Displays a Mod Logo using the locator.</summary>
        public virtual void DisplayLogo(int modId, LogoImageLocator locator)
        {
            if(this.m_locator != locator)
            {
                this.image.enabled = false;

                this.m_modId = modId;
                this.m_locator = locator;

                if(locator != null)
                {
                    System.Action<Texture2D> displayDelegate = (t) => ApplyTexture(locator, t);

                    ImageRequestManager.instance.RequestModLogo(modId, locator, this.logoSize,
                                                                displayDelegate,
                                                                displayDelegate, // fallback
                                                                WebRequestError.LogAsWarning);
                }
            }
        }

        /// <summary>Internal function for applying the texture.</summary>
        protected virtual void ApplyTexture(LogoImageLocator locator, Texture2D texture)
        {
            if(this != null
               && this.m_locator == locator
               && texture != null)
            {
                this.image.sprite = UIUtilities.CreateSpriteFromTexture(texture);
                this.image.enabled = true;
            }
        }
    }
}
