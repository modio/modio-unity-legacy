using Debug = UnityEngine.Debug;

namespace ModIO
{
    public static class ModImageLocator
    {
        private const string PREFIX = "MODIMAGE:";
        private const int PART_COUNT = 2;

        public static string Generate(int modId, string fileName)
        {
            Debug.Assert(modId > 0 && !System.String.IsNullOrEmpty(fileName));

            return (PREFIX + modId.ToString()
                    + @"/" + fileName);
        }

        public static bool TryParse(string identifier,
                                    out int modId,
                                    out string fileName)
        {
            modId = 0;
            fileName = string.Empty;
            // version = ImageVersion.Original;

            if(identifier.Length <= PREFIX.Length) { return false; }
            if(!identifier.Substring(0, PREFIX.Length).Equals(PREFIX)) { return false; }

            string[] identifierParts = identifier.Substring(PREFIX.Length).Split('/');
            if(identifierParts.Length != PART_COUNT) { return false; }

            if(!int.TryParse(identifierParts[0], out modId)) { return false; }
            if(identifierParts[1].Length > 0) { return false; }
            fileName = identifierParts[1];

            // int versionInt = -1;
            // if(!int.TryParse(identifierParts[2], out versionInt)) { return false; }
            // version = (ImageVersion)versionInt;

            return true;
        }
    }
}