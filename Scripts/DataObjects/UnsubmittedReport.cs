using System;
using ModIO.API;

namespace ModIO
{
    public class UnsubmittedReport
    {
        public enum ReportedResourceType
        {
            Game,
            Mod,
            User
        }

        public static string GetReportedResourceTypeString(ReportedResourceType resourceType)
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
                    return null;
                }
            }
        }

        public static bool TryParseStringAsReportedResourceType(string value, out ReportedResourceType resourceType)
        {
            switch(value)
            {
                case "games":
                {
                    resourceType = ReportedResourceType.Game;
                    return true;
                }
                case "mods":
                {
                    resourceType = ReportedResourceType.Mod;
                    return true;
                }
                case "users":
                {
                    resourceType = ReportedResourceType.User;
                    return true;
                }
            }

            resourceType = ReportedResourceType.Game;
            return false;
        }

        // --- FIELDS ---
        // [Required] Type of resource you are reporting. Must be one of the following
        public ReportedResourceType resourceType;
        // [Required] Unique id of the resource you are reporting.
        public int resourceId;
        // [Required] Is this a DMCA takedown request?
        public bool isDMCA;
        // [Required] Informative title for your report.
        public string name;
        // [Required] Detailed description of your report. Make sure you include all relevant information and links to help moderators investigate and respond appropiately.
        public string summary;

        // --- ACCESSORS ---
        public StringValueParameter[] GetValueFields()
        {
            StringValueParameter[] retVal = new StringValueParameter[5];

            retVal[0] = StringValueParameter.Create("resource", GetReportedResourceTypeString(resourceType));
            retVal[1] = StringValueParameter.Create("id", resourceId);
            retVal[2] = StringValueParameter.Create("dmca", (isDMCA ? "1" : "0"));
            retVal[3] = StringValueParameter.Create("name", name);
            retVal[4] = StringValueParameter.Create("summary", summary);

            return retVal;
        }
    }
}