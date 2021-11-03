using System;

namespace ModIO
{
    // ---------[ SERIALIZABLE EDIT FIELDS ]------
    [Serializable]
    public class EditableResourceTypeField : EditableField<ReportedResourceType>
    {
    }

    [Serializable]
    public class EditableReportTypeField : EditableField<ReportType>
    {
    }

    [Serializable]
    public class EditableReport
    {
        // ---------[ FIELDS ]---------
        public EditableResourceTypeField resourceType = new EditableResourceTypeField();
        public EditableIntField resourceId = new EditableIntField();
        public EditableReportTypeField reportType = new EditableReportTypeField();
        public EditableStringField summary = new EditableStringField();
        public EditableStringField name = new EditableStringField();
        public EditableIntField contact = new EditableIntField();

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


        // ---------[ Obsolete ]---------
        [System.Obsolete("No longer supported. Use EditableReport.reportType instead.", true)]
        public EditableBoolField isDMCA = null;
    }
}
