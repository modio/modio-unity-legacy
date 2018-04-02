namespace ModIO.API
{
    [System.Serializable]
    public struct GameTagOptionObject
    {
        // Name of the tag group.
        public readonly string name;
        // Can multiple tags be selected via 'checkboxes' or should only a single tag be selected via a 'dropdown'.
        public readonly string type;
        // Groups of tags flagged as 'admin only' should only be used for filtering, and should not be displayed to users.
        public readonly bool hidden;
        // Array of tags in this group.
        public readonly string[] tags;
    }
}