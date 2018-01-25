using System;

namespace ModIO.API
{
    [Serializable]
    public struct AvatarObject : IEquatable<AvatarObject>
    {
        public string filename; // Avatar filename including extension.
        public string original; // URL to the full-sized avatar.
        public string thumb_50x50; // URL to the small thumbnail image.
        public string thumb_100x100; // URL to the medium thumbnail image.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is AvatarObject
                    && this.Equals((AvatarObject)obj));
        }

        public bool Equals(AvatarObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_50x50.Equals(other.thumb_50x50)
                   && this.thumb_100x100.Equals(other.thumb_100x100));
        }
    }
}