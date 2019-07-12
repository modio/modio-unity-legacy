using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the mod gallery images for a given mod.</summary>
    public class GalleryImageContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Gallery image display object prefab.</summary>
        public GalleryImageDisplay itemPrefab = null;

        /// <summary>Container for the display objects.</summary>
        public RectTransform container = null;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Gallery Image Locators to display.</summary>
        private GalleryImageLocator[] m_locators = null;

        /// <summary>Display objects.</summary>
        private GalleryImageDisplay[] m_displays = new GalleryImageDisplay[0];

        // ---------[ INITIALIZATION ]---------
        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public virtual void SetModView(ModView view)
        {
            Debug.Log("Setting View: " + view.gameObject.name,
                      this);

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
        /// <summary>Displays gallery images of a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            Debug.Log("Displaying profile: "
                      + (profile == null ? "NULL" : profile.name));

            GalleryImageLocator[] newLocators = null;

            if(profile != null
               && profile.media != null)
            {
                newLocators = profile.media.galleryImageLocators;
            }

            if(newLocators != this.m_locators)
            {
                this.m_locators = newLocators;

                int imageCount = 0;
                if(newLocators != null)
                {
                    imageCount = newLocators.Length;
                }

                this.SetDisplayCount(imageCount);

                for(int i = 0;
                    i < imageCount;
                    ++i)
                {
                    this.m_displays[i].DisplayGalleryImage(profile.id, newLocators[i]);
                }
            }
        }

        /// <summary>Creates/Destroys display objects to match the given value.</summary>
        protected virtual void SetDisplayCount(int newCount)
        {
            int difference = newCount - this.m_displays.Length;

            if(difference > 0)
            {
                GalleryImageDisplay[] newDisplayArray = new GalleryImageDisplay[newCount];

                for(int i = 0;
                    i < this.m_displays.Length;
                    ++i)
                {
                    newDisplayArray[i] = this.m_displays[i];
                }

                for(int i = this.m_displays.Length;
                    i < newDisplayArray.Length;
                    ++i)
                {
                    GameObject displayGO = GameObject.Instantiate(itemPrefab.gameObject);
                    displayGO.name = "Mod Gallery Image [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(container, false);
                    // TODO(@jackson): Fix layouting?

                    newDisplayArray[i] = displayGO.GetComponent<GalleryImageDisplay>();
                }

                this.m_displays = newDisplayArray;
            }
            else if(difference < 0)
            {
                GalleryImageDisplay[] newDisplayArray = new GalleryImageDisplay[newCount];

                for(int i = 0;
                    i < newDisplayArray.Length;
                    ++i)
                {
                    newDisplayArray[i] = this.m_displays[i];
                }

                for(int i = newDisplayArray.Length;
                    i < this.m_displays.Length;
                    ++i)
                {
                    GameObject.Destroy(this.m_displays[i].gameObject);
                }

                this.m_displays = newDisplayArray;
            }
        }
    }
}
