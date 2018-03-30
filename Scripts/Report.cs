namespace ModIO
{
    [System.Serializable]
    public class Report
    {
        public enum ResourceType
        {
            Game,
            Mod,
            User,
        }

        // ---------[ FIELDS ]---------
        public EditableField<ResourceType> resourceType = new EditableField<ResourceType>();
        public EditableField<int> resourceId            = new EditableField<int>();
        public EditableField<bool> isDMCA               = new EditableField<bool>();
        public EditableField<string> name               = new EditableField<string>();
        public EditableField<string> summary            = new EditableField<string>();

        public static string ResourceTypeToAPIString(ResourceType resourceType)
        {
            switch(resourceType)
            {
                case ResourceType.Game:
                {
                    return "games";
                }
                case ResourceType.Mod:
                {
                    return "mods";
                }
                case ResourceType.User:
                {
                    return "users";
                }
                default:
                {
                    return string.Empty;
                }
            }
        }
    }
}