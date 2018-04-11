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

        // ---------[ SERIALIZABLE EDIT FIELDS ]------
        [System.Serializable]
        public class EditableResourceTypeField : EditableField<ResourceType> {}

        // ---------[ FIELDS ]---------
        public EditableResourceTypeField resourceType=  new EditableResourceTypeField();
        public EditableIntField resourceId =            new EditableIntField();
        public EditableBoolField isDMCA =               new EditableBoolField();
        public EditableStringField name =               new EditableStringField();
        public EditableStringField summary =            new EditableStringField();

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