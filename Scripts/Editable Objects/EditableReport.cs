namespace ModIO
{
    [System.Serializable]
    public class EditableReport
    {
        // ---------[ SERIALIZABLE EDIT FIELDS ]------
        [System.Serializable]
        public class EditableResourceTypeField : EditableField<ReportedResourceType> {}

        // ---------[ FIELDS ]---------
        public EditableResourceTypeField resourceType=  new EditableResourceTypeField();
        public EditableIntField resourceId =            new EditableIntField();
        public EditableBoolField isDMCA =               new EditableBoolField();
        public EditableStringField name =               new EditableStringField();
        public EditableStringField summary =            new EditableStringField();

        public static string ResourceTypeToAPIString(ReportedResourceType resourceType)
        {
            switch(resourceType)
            {
                case ReportedResourceType.Game:
                {
                    return "games";
                }
                case ReportedResourceType.Mod:
                {
                    return "mods";
                }
                case ReportedResourceType.User:
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
