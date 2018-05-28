namespace ModIO.API
{
    public class SubmitReportParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Type of resource you are reporting. Must be one of the following values:
        public string resource
        {
            set
            {
                this.SetStringValue("resource", value);
            }
        }
        // [REQUIRED] Unique id of the resource you are reporting.
        public int id
        {
            set
            {
                this.SetStringValue("id", value);
            }
        }
        // [REQUIRED] The type of report you are submitting. Must be one of the following values:
        public int type
        {
            set
            {
                this.SetStringValue("type", value);
            }
        }
        // [REQUIRED] Informative title for your report.
        public string name
        {
            set
            {
                this.SetStringValue("name", value);
            }
        }
        // [REQUIRED] Detailed description of your report. Make sure you include all relevant information and links to help moderators investigate and respond appropiately.
        public string summary
        {
            set
            {
                this.SetStringValue("summary", value);
            }
        }
    }
}