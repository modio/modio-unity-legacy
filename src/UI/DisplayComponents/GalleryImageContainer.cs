using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the mod gallery images for a given mod.</summary>
    public class GalleryImageContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the gallery images.</summary>
        public RectTransform template = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Gallery image display object template.</summary>
        private GalleryImageDisplay m_itemTemplate = null;

        /// <summary>ModId for the currently displayed images.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>Gallery Image Locators to display.</summary>
        private GalleryImageLocator[] m_locators = new GalleryImageLocator[0];

        /// <summary>Display objects.</summary>
        private GalleryImageDisplay[] m_displays = new GalleryImageDisplay[0];

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            // duplication protection
            if(this.m_itemTemplate != null) { return; }

            // initialize
            this.template.gameObject.SetActive(false);
            this.m_itemTemplate = this.template.GetComponentInChildren<GalleryImageDisplay>(true);

            if(this.m_itemTemplate != null
               && this.template.gameObject != this.m_itemTemplate.gameObject)
            {
                this.m_templateClone = GameObject.Instantiate(this.template.gameObject, this.template.parent);
                this.m_templateClone.SetActive(true);
                this.m_templateClone.transform.SetSiblingIndex(this.template.GetSiblingIndex() + 1);

                this.m_displays = new GalleryImageDisplay[1];
                this.m_displays[0] = this.m_templateClone.GetComponentInChildren<GalleryImageDisplay>(true);
                this.m_displays[0].gameObject.name = "Mod Gallery Image [00]";

                this.m_container = (RectTransform)this.m_displays[0].transform.parent;
            }
            else
            {
                Debug.LogError("[mod.io] This GalleryImageContainer has an invalid template"
                               + " hierarchy. The Template must contain a child with a"
                               + " GalleryImageDisplay component to use as the item template.",
                               this);
            }
        }

        /// <summary>Ensure the displays are accurate.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayImages(this.m_modId, this.m_locators);
        }

        /// <summary>IModViewElement interface.</summary>
        public virtual void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfile);
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
            int modId = ModProfile.NULL_ID;
            GalleryImageLocator[] locators = null;

            if(profile != null
               && profile.media != null)
            {
                modId = profile.id;
                locators = profile.media.galleryImageLocators;
            }

            this.DisplayImages(modId, locators);
        }

        /// <summary>Displays a set of gallery images.</summary>
        public virtual void DisplayImages(int modId, IList<GalleryImageLocator> locators)
        {
            this.m_modId = modId;

            // copy locators
            if(this.m_locators != locators)
            {
                int imageCount = 0;
                if(locators != null)
                {
                    imageCount = locators.Count;
                }

                this.m_locators = new GalleryImageLocator[imageCount];
                for(int i = 0; i < imageCount; ++i)
                {
                    this.m_locators[i] = locators[i];
                }
            }

            // display
            if(this.isActiveAndEnabled)
            {
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate,
                                             "Gallery Image", this.m_locators.Length,
                                             ref this.m_displays);

                for(int i = 0;
                    i < this.m_locators.Length;
                    ++i)
                {
                    this.m_displays[i].DisplayGalleryImage(modId, this.m_locators[i]);
                }

                this.m_templateClone.SetActive(this.m_locators.Length > 0 || !this.hideIfEmpty);
            }
        }
    }
}
