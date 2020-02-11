using System;

namespace ModIO
{
    // ---------[ SERIALIZABLE EDIT FIELDS ]------
    [Serializable]
    public class EditableResourceTypeField : EditableField<ReportedResourceType> {}

    [Serializable]
    public class EditableReport
    {
        // ---------[ FIELDS ]---------
        public ModIO.EditableResourceTypeField resourceType =   new ModIO.EditableResourceTypeField();
        public EditableIntField resourceId =                    new EditableIntField();
        public EditableBoolField isDMCA =                       new EditableBoolField();
        public EditableStringField name =                       new EditableStringField();
        public EditableStringField summary =                    new EditableStringField();

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
