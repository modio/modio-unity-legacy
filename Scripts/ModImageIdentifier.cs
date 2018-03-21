using Debug = UnityEngine.Debug;

namespace ModIO
{
    public static class ModImageIdentifier
    {
        private const string PREFIX = "IMAGE:";
        private const int PREFIX_LENGTH = 6;

        private const string TYPESTRING_MODLOGO = "ML";
        private const string TYPESTRING_MODMEDIA = "MM";

        public static string GenerateForModLogo(int modId)
        {
            Debug.Assert(modId > 0);
            
            return (PREFIX + modId.ToString()
                    + @"/" + TYPESTRING_MODLOGO);
        }
        
        public static string GenerateForModMedia(int modId, string fileName)
        {
            Debug.Assert(modId > 0 && !System.String.IsNullOrEmpty(fileName));

            return (PREFIX + modId.ToString()
                    + @"/" + TYPESTRING_MODMEDIA
                    + @"/" + fileName);
        }

        public static bool TryParse(string identifier,
                                    out int modId,
                                    out bool isLogo,
                                    out string fileName)
        {
            Debug.Assert(!System.String.IsNullOrEmpty(identifier));

            modId = 0;
            isLogo = false;
            fileName = string.Empty;
            // version = ImageVersion.Original;

            // - Check Prefix -
            if(identifier.Length <= PREFIX_LENGTH) { return false; }
            string prefix = identifier.Substring(0, PREFIX_LENGTH);
            if(!prefix.Equals(PREFIX)) { return false; }


            string[] identifierParts = identifier.Substring(PREFIX_LENGTH).Split('/');
            if(identifierParts.Length < 2) { return false; }

            // - Mod Id -
            if(!int.TryParse(identifierParts[0], out modId)) { return false; }

            // - Get Type -
            string typeString = identifierParts[1];
            switch(typeString)
            {
                case TYPESTRING_MODLOGO:
                {
                    isLogo = true;
                    return true;
                }
                case TYPESTRING_MODMEDIA:
                {
                    if(identifierParts.Length < 3) { return false; }

                    fileName = identifierParts[2];
                    return (fileName.Length > 0);
                }
                default:
                {
                    return false;
                }
            }

            // int versionInt = -1;
            // if(!int.TryParse(identifierParts[2], out versionInt)) { return false; }
            // version = (ImageVersion)versionInt;
        }
    }
}