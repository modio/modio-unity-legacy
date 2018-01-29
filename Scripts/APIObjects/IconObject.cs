using System;

namespace ModIO.API
{
    [Serializable]
    public struct IconObject : IEquatable<IconObject>
    {
        // - Fields -
        public string filename; // Icon filename including extension.
        public string original; // URL to the full-sized icon.
        public string thumb_64x64; // URL to the small thumbnail image.
        public string thumb_128x128; // URL to the medium thumbnail image.
        public string thumb_256x256; // URL to the large thumbnail image.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is IconObject
                    && this.Equals((IconObject)obj));
        }

        public bool Equals(IconObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_64x64.Equals(other.thumb_64x64)
                   && this.thumb_128x128.Equals(other.thumb_128x128)
                   && this.thumb_256x256.Equals(other.thumb_256x256));
        }
    }
}