namespace PACOM.WebhookApp.Data
{
    public class OrganizationRoles
    {
        public int Id { get; set; }           // Primary key
        public string UserId { get; set; } = string.Empty;
        public int OrganizationId { get; set; }

    }
}
