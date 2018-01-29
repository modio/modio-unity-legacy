using System;

namespace ModIO.API
{
    [Serializable]
    public struct ModDependencyObject : IEquatable<ModDependencyObject>
    {
        // - Fields -
        public int mod_id;  // Unique id of the mod that is the dependency.
        public int date_added;  // Unix timestamp of date the dependency was added.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.mod_id ^ this.date_added);
        }

        public override bool Equals(object obj)
        {
            return (obj is ModDependencyObject
                    && this.Equals((ModDependencyObject)obj));
        }

        public bool Equals(ModDependencyObject other)
        {
            return(this.mod_id.Equals(other.mod_id)
                   && this.date_added.Equals(other.date_added));
        }
    }
}