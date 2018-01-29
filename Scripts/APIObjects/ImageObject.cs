using System;

namespace ModIO.API
{
    [Serializable]
    public struct ImageObject : IEquatable<ImageObject>
    {
        // - Fields -
        public string filename; // Image filename including extension.
        public string original; // URL to the full-sized image.
        public string thumb_320x180; // URL to the image thumbnail.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is ImageObject
                    && this.Equals((ImageObject)obj));
        }

        public bool Equals(ImageObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_320x180.Equals(other.thumb_320x180));
        }
    }
}