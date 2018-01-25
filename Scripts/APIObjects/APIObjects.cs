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