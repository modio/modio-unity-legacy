using System;

namespace ModIO.API
{
    [Serializable]
    public struct ModTagObject : IEquatable<ModTagObject>
    {
        // - Fields -
        public string name; // Tag name.
        public int date_added;  // Unix timestamp of date tag was applied.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.name.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is ModTagObject
                    && this.Equals((ModTagObject)obj));
        }

        public bool Equals(ModTagObject other)
        {
            return(this.name.Equals(other.name)
                   && this.date_added.Equals(other.date_added));
        }
    }
}