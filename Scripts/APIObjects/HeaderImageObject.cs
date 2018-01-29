using System;

namespace ModIO.API
{
    [Serializable]
    public struct HeaderImageObject : IEquatable<HeaderImageObject>
    {
        // - Fields -
        public string filename; // Header image filename including extension.
        public string original; // URL to the full-sized header image.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is HeaderImageObject
                    && this.Equals((HeaderImageObject)obj));
        }

        public bool Equals(HeaderImageObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original));
        }
    }
}