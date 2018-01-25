using System;

namespace ModIO.API
{
    [Serializable]
    public struct GameTagOptionObject : IEquatable<GameTagOptionObject>
    {
        // - Fields -
        public string name; // Name of the tag group.
        public string type; // Can multiple tags be selected via 'checkboxes' or should only a single tag be selected via a 'dropdown'.
        public int hidden;  // Groups of tags flagged as 'admin only' should only be used for filtering, and should not be displayed to users.
        public string[] tags; // Array of tags in this group.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.name.GetHashCode() 
                    ^ this.type.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is GameTagOptionObject
                    && this.Equals((GameTagOptionObject)obj));
        }

        public bool Equals(GameTagOptionObject other)
        {
            return(this.name.Equals(other.name)
                   && this.type.Equals(other.type)
                   && this.hidden.Equals(other.hidden)
                   && this.tags.GetHashCode().Equals(other.tags.GetHashCode()));
        }
    }
}