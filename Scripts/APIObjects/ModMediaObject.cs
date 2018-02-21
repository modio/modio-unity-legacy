using System;

namespace ModIO.API
{
    [Serializable]
    public struct ModMediaObject : IEquatable<ModMediaObject>
    {
        // - Fields -
        public string[] youtube; // Array of YouTube links.
        public string[] sketchfab; // Array of SketchFab links.
        public ImageObject[] images; // Array of image objects (a gallery).

        // - Clone -
        public ModMediaObject Clone()
        {
            ModMediaObject clone = new ModMediaObject();

            if(this.youtube != null)
            {
                clone.youtube = new string[this.youtube.Length];
                for(int i = 0; i < this.youtube.Length; ++i)
                {
                    clone.youtube[i] = this.youtube[i];
                }
            }
    
            if(this.sketchfab != null)
            {
                clone.sketchfab = new string[this.sketchfab.Length];
                for(int i = 0; i < this.sketchfab.Length; ++i)
                {
                    clone.sketchfab[i] = this.sketchfab[i];
                }
            }
            
            if(this.images != null)
            {
                clone.images = new ImageObject[this.images.Length];
                for(int i = 0; i < this.images.Length; ++i)
                {
                    clone.images[i] = this.images[i];
                }
            }

            return clone;
        }

        // - Equality Operators -
        public override int GetHashCode()
        {
            return(this.youtube.GetHashCode()
                   ^ this.sketchfab.GetHashCode()
                   ^ this.images.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is ModMediaObject
                    && this.Equals((ModMediaObject)obj));
        }

        public bool Equals(ModMediaObject other)
        {
            return(this.youtube.GetHashCode().Equals(other.youtube.GetHashCode())
                   && this.sketchfab.GetHashCode().Equals(other.sketchfab.GetHashCode())
                   && this.images.GetHashCode().Equals(other.images.GetHashCode()));
        }
    }
}