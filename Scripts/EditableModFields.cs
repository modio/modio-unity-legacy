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
        public EditableField<string[]> tags =               new EditableField<string[]>();
        // - Mod Media -
        public EditableField<string> logoFilePath =         new EditableField<string>();
        public EditableField<string[]> youtubeURLs =        new EditableField<string[]>();
        public EditableField<string[]> sketchfabURLS =      new EditableField<string[]>();
        public EditableField<string[]> imageLocators =      new EditableField<string[]>();
    }
}