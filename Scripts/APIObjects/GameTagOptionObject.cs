using System;

namespace ModIO.API
{
    [Serializable]
    public struct GameTagOptionObject : IEquatable<GameTagOptionObject>
    {
        // - Fields -
        public string name; // Name of the tag group.
        public string type; // Can multiple tags be selected via 'checkboxes' or should only a single tag be selected via a 'dropdown'.
        public bool hidden;  // Groups of tags flagged as 'admin only' should only be used for filtering, and should not be displayed to users.
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

    [Serializable]
    public struct CreatedGameTagOptionObject
    {
        // --- FIELDS ---
        public string name; // [Required] Name of the tag group, for example you may want to have 'Difficulty' as the name with 'Easy', 'Medium' and 'Hard' as the tag values.
        public string type; // [Required] Determines whether you allow users to only select one tag (dropdown) or multiple tags (checkbox):
        public bool hidden; // This group of tags should be hidden from users and mod developers. Useful for games to tag special functionality, to filter on and use behind the scenes. You can also use Metadata Key Value Pairs for more arbitary data.
        public string[] tags; // [Required] Array of tags mod creators can choose to apply to their profiles.
    }
}