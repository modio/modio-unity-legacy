using System;
using System.Collections.Generic;
using System.Linq; // TODO(@jackson): Remove

namespace ModIO
{
    [Serializable]
    public class EditableModProfile
    {
        // ---------[ SERIALIZABLE EDITABLE CLASSES ]---------
        [Serializable]
        public class EditableModStatusField : EditableField<ModStatus> {}
        [Serializable]
        public class EditableModVisibilityField : EditableField<ModVisibility> {}

        // ---------[ FIELDS ]---------
        public EditableModStatusField status =                      new EditableModStatusField();
        public EditableModVisibilityField visibility =              new EditableModVisibilityField();
        public EditableStringField name =                           new EditableStringField();
        public EditableStringField nameId =                         new EditableStringField();
        public EditableStringField summary =                        new EditableStringField();
        public EditableStringField description =                    new EditableStringField();
        public EditableStringField homepageURL =                    new EditableStringField();
        public EditableStringField metadataBlob =                   new EditableStringField();
        public EditableStringArrayField tags =                      new EditableStringArrayField();
        // TODO(@jackson): KVPs
        // - Mod Media -
        public EditableImageLocatorField logoLocator =              new EditableImageLocatorField();
        public EditableStringArrayField youtubeURLs =               new EditableStringArrayField();
        public EditableStringArrayField sketchfabURLs =             new EditableStringArrayField();
        public EditableImageLocatorArrayField galleryImageLocators =new EditableImageLocatorArrayField();

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
            retVal.tags.value = profile.tagNames.ToArray();

            // - Media -
            retVal.logoLocator.value.fileName = profile.logoLocator.fileName;
            retVal.logoLocator.value.url = profile.logoLocator.GetURL();

            retVal.youtubeURLs.value = profile.media.youtubeURLs;
            retVal.sketchfabURLs.value = profile.media.sketchfabURLs;

            Utility.SafeMapArraysOrZero(profile.media.galleryImageLocators,
                                        (l) => { return ImageLocatorData.CreateFromImageLocator(l); },
                                        out retVal.galleryImageLocators.value);

            return retVal;
        }
    }
}