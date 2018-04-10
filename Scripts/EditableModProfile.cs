using System.Collections.Generic;

namespace ModIO
{
    [System.Serializable]
    public class EditableModProfile
    {
        // ---------[ FIELDS ]---------
        public EditableField<ModStatus> status =                    new EditableField<ModStatus>();
        public EditableField<ModVisibility> visibility =            new EditableField<ModVisibility>();
        public EditableField<string> name =                         new EditableField<string>();
        public EditableField<string> nameId =                       new EditableField<string>();
        public EditableField<string> summary =                      new EditableField<string>();
        public EditableField<string> description =                  new EditableField<string>();
        public EditableField<string> homepageURL =                  new EditableField<string>();
        public EditableField<string> metadataBlob =                 new EditableField<string>();
        public EditableField<List<string>> tags =                   new EditableField<List<string>>();
        // - Mod Media -
        public EditableField<EditableImageLocator> logoLocator =    new EditableField<EditableImageLocator>();
        public EditableField<List<string>> youtubeURLs =            new EditableField<List<string>>();
        public EditableField<List<string>> sketchfabURLs =          new EditableField<List<string>>();
        public EditableField<List<EditableImageLocator>> galleryImageLocators = new EditableField<List<EditableImageLocator>>();

        // ---------[ INITIALIZATION ]---------
        public static EditableModProfile CreateFromProfile(ModProfile profile)
        {
            EditableModProfile retVal = new EditableModProfile();
            retVal.status.value = profile.status;
            retVal.visibility.value = profile.visibility;
            retVal.name.value = profile.name;
            retVal.nameId.value = profile.nameId;
            retVal.summary.value = profile.summary;
            retVal.description.value = profile.description;
            retVal.homepageURL.value = profile.homepageURL;
            retVal.metadataBlob.value = profile.metadataBlob;
            retVal.tags.value = new List<string>(profile.tags);

            // - Media -
            retVal.logoLocator.value.fileName = profile.logoLocator.fileName;
            retVal.logoLocator.value.source = profile.logoLocator.source;

            retVal.youtubeURLs.value = new List<string>(profile.youtubeURLs);
            retVal.sketchfabURLs.value = new List<string>(profile.sketchfabURLs);

            retVal.galleryImageLocators.value = new List<EditableImageLocator>();
            foreach(var locator in profile.galleryImageLocators)
            {
                var newLocator = new EditableImageLocator();
                newLocator.fileName = locator.fileName;
                newLocator.source = locator.source;

                retVal.galleryImageLocators.value.Add(newLocator);
            }

            return retVal;
        }
    }
}