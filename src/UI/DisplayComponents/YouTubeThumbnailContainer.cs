using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the YouTube thumbnails for a given mod.</summary>
    public class YouTubeThumbnailContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>YouTube thumbnail display object prefab.</summary>
        public YouTubeThumbnailDisplay itemPrefab = null;

        /// <summary>Container for the display objects.</summary>
        public RectTransform container = null;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>YouTube ids to display.</summary>
        private string[] m_youTubeIds = null;

        /// <summary>Display objects.</summary>
        private YouTubeThumbnailDisplay[] m_displays = new YouTubeThumbnailDisplay[0];

        // ---------[ INITIALIZATION ]---------
        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public virtual void SetModView(ModView view)
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
        /// <summary>Displays the YouTube thumbnails for a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            string[] newIds = null;

            if(profile != null
               && profile.media != null
               && profile.media.youTubeURLs != null)
            {
                string[] URLs = profile.media.youTubeURLs;
                newIds = new string[URLs.Length];

                for(int i = 0; i < URLs.Length; ++i)
                {
                    newIds[i] = Utility.ExtractYouTubeIdFromURL(URLs[i]);
                }
            }

            if(newIds != this.m_youTubeIds)
            {
                this.m_youTubeIds = newIds;

                int thumbnailCount = 0;
                if(newIds != null)
                {
                    thumbnailCount = newIds.Length;
                }

                this.SetDisplayCount(thumbnailCount);

                for(int i = 0; i < thumbnailCount; ++i)
                {
                    this.m_displays[i].DisplayThumbnail(profile.id, newIds[i]);
                }
            }
        }

        /// <summary>Creates/Destroys display objects to match the given value.</summary>
        protected virtual void SetDisplayCount(int newCount)
        {
            int difference = newCount - this.m_displays.Length;

            if(difference > 0)
            {
                YouTubeThumbnailDisplay[] newDisplayArray = new YouTubeThumbnailDisplay[newCount];

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
                    displayGO.name = "Mod YouTube Thumbnail [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(container, false);

                    newDisplayArray[i] = displayGO.GetComponent<YouTubeThumbnailDisplay>();
                }

                this.m_displays = newDisplayArray;
            }
            else if(difference < 0)
            {
                YouTubeThumbnailDisplay[] newDisplayArray = new YouTubeThumbnailDisplay[newCount];

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
