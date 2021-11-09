namespace ModIO.API
{
    public class SubmitReportParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Type of resource you are reporting. Must be one of the following values:
        public string resource
        {
            set {
                this.SetStringValue("resource", value);
            }
        }
        // [REQUIRED] Unique id of the resource you are reporting.
        public int id
        {
            set {
                this.SetStringValue("id", value);
            }
        }
        // [REQUIRED] The type of report you are submitting. Must be one of the following values:
        public ReportType type
        {
            set {
                this.SetStringValue("type", (int)value);
            }
        }
        // [REQUIRED] Detailed description of your report. Make sure you include all relevant
        // information and links to help moderators investigate and respond appropiately.
        public string summary
        {
            set {
                this.SetStringValue("summary", value);
            }
        }
        // Contact details: Name of the user submitting the report. Recommended for DMCA reports.
        public string name
        {
            set {
                this.SetStringValue("name", value);
            }
        }
        // Contact details: Method of contacting the user submitting the report. Recommended for
        // DMCA reports.
        public string contact
        {
            set {
                this.SetStringValue("contact", value);
            }
        }
    }
}
