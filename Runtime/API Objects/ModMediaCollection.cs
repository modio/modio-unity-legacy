#define SEPARATE_GIF_IMAGES

#if SEPARATE_GIF_IMAGES
using System.Collections.Generic;
using System.Runtime.Serialization;
#endif

using Path = System.IO.Path;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModMediaCollection
    {
        // ---------[ FIELDS ]---------
        /// <summary>Array of YouTube links.</summary>
        [JsonProperty("youtube")]
        public string[] youTubeURLs;

        /// <summary>Array of SketchFab links.</summary>
        [JsonProperty("sketchfab")]
        public string[] sketchfabURLs;

        /// <summary>Array of image objects (a gallery).</summary>
        [JsonProperty("images")]
        public GalleryImageLocator[] galleryImageLocators;

#if SEPARATE_GIF_IMAGES
        /// <summary>Array of gallery images that are in the GIF-format.</summary>
        [JsonProperty("gif_images")]
        public GalleryImageLocator[] galleryGIFLocators;
#endif

        // ---------[ ACCESSORS ]---------
        public GalleryImageLocator GetGalleryImageWithFileName(string fileName)
        {
            foreach(var locator in this.galleryImageLocators)
            {
                if(locator.fileName == fileName)
                {
                    return locator;
                }
            }
            return null;
        }

#if SEPARATE_GIF_IMAGES
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(this.galleryGIFLocators == null && this.galleryImageLocators != null)
            {
                List<GalleryImageLocator> gifLocators = new List<GalleryImageLocator>();
                List<GalleryImageLocator> galleryLocators = new List<GalleryImageLocator>();

                foreach(var locator in this.galleryImageLocators)
                {
                    string imageExtension = Path.GetExtension(locator.fileName);
                    if(imageExtension.ToUpper() == ".GIF")
                    {
                        gifLocators.Add(locator);
                    }
                    else
                    {
                        galleryLocators.Add(locator);
                    }
                }

                this.galleryImageLocators = galleryLocators.ToArray();
                this.galleryGIFLocators = gifLocators.ToArray();
            }
        }
#endif
    }
}
