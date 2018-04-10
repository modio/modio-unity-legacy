namespace ModIO
{
    [System.Serializable]
    public class EditableModfile
    {
        // ---------[ FIELDS ]---------
        public EditableField<string> version        = new EditableField<string>();
        public EditableField<string> changelog      = new EditableField<string>();
        public EditableField<string> metadataBlob   = new EditableField<string>();
    }
}