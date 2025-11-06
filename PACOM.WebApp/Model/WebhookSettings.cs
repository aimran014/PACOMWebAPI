namespace PACOM.WebApp.Model
{
    public class WebhookSettings
    {
        public bool Enabled { get; set; }
        public string WebhookLink { get; set; } = string.Empty;
        public int CheckIntervalSeconds { get; set; } = 30;
        public string OrganizationCode { get; set; } = string.Empty;
    }
}
