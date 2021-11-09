namespace ModIO
{
    [System.Serializable]
    public class EditableModfile
    {
        // ---------[ FIELDS ]---------
        public EditableStringField version = new EditableStringField();
        public EditableStringField changelog = new EditableStringField();
        public EditableStringField metadataBlob = new EditableStringField();

        // ---------[ VALUE DUPLICATION ]---------
        public static EditableModfile CreateFromModfile(Modfile modfile)
        {
            EditableModfile newModfile = new EditableModfile();
            newModfile.ApplyBaseModfileChanges(modfile);
            return newModfile;
        }

        public void ApplyBaseModfileChanges(Modfile modfile)
        {
            if(!this.version.isDirty)
            {
                this.version.value = modfile.version;
            }
            if(!this.changelog.isDirty)
            {
                this.changelog.value = modfile.changelog;
            }
            if(!this.metadataBlob.isDirty)
            {
                this.metadataBlob.value = modfile.metadataBlob;
            }
        }
    }
}
