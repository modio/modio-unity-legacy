using System;

namespace ModIO.API
{
    [Serializable]
    public struct ModEventObject : IEquatable<ModEventObject>
    {
        // - Fields -
        public int id;  // Unique id of the event object.
        public int mod_id;  // Unique id of the parent mod.
        public int user_id; // Unique id of the user who performed the action.
        public int date_added;  // Unix timestamp of date the event occurred.
        public string event_type; // Type of event was 'MODFILE_CHANGED', 'MOD_AVAILABLE', 'MOD_UNAVAILABLE', 'MOD_EDITED'.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is ModEventObject
                    && this.Equals((ModEventObject)obj));
        }

        public bool Equals(ModEventObject other)
        {
            return(this.id.Equals(other.id)
                   && this.mod_id.Equals(other.mod_id)
                   && this.user_id.Equals(other.user_id)
                   && this.date_added.Equals(other.date_added)
                   && this.event_type.Equals(other.event_type));
        }
    }
}