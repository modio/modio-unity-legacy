using System;
using System.Collections.Generic;
using ModIO.API;

namespace ModIO
{
    [Serializable]
    public class ModfileProfile
    {
        // --- FIELDS ---
        public int modId = 0;
        public int modfileId = 0;

        // Version of the file release.
        public string version;
        // Changelog of this release.
        public string changelog;
        // Metadata stored by the game developer which may include properties such as what version of the game this file is compatible with. Metadata can also be stored as searchable key value pairs, and to the mod object.
        public string metadataBlob;

        public AddModfileParameters AsAddModfileParameters()
        {
            AddModfileParameters retVal = new AddModfileParameters();
            retVal.version = this.version;
            retVal.changelog = this.changelog;
            retVal.active = true;
            retVal.metadata_blob = this.metadataBlob;
            return retVal;
        }
    }
}
