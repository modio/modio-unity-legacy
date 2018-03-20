namespace ModIO
{
    [System.Serializable]
    public class EditableModFields
    {
        // ---------[ FIELDS ]---------
        public EditableField<ModStatus> status =            new EditableField<ModStatus>();
        public EditableField<ModVisibility> visibility =    new EditableField<ModVisibility>();
        public EditableField<string> name =                 new EditableField<string>();
        public EditableField<string> nameId =               new EditableField<string>();
        public EditableField<string> summary =              new EditableField<string>();
        public EditableField<string> description =          new EditableField<string>();
        public EditableField<string> homepage =             new EditableField<string>();
        public EditableField<string> metadataBlob =         new EditableField<string>();

        public bool GetIsDirty()
        {
            return (this.status.isDirty
                    || this.visibility.isDirty
                    || this.name.isDirty
                    || this.nameId.isDirty
                    || this.summary.isDirty
                    || this.description.isDirty
                    || this.homepage.isDirty
                    || this.metadataBlob.isDirty);
        }
    }
}