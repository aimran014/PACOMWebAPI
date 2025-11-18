namespace PACOM.Client.Data
{
    public class ActivityEvent
    {
        public string Version { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;

        public string? ScopeName { get; set; }
        public string? Organization { get; set; }
        public string? EventId { get; set; }

        public string? EventName { get; set; }
        public string? UserId { get; set; }

        public string? UserName { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
        public string? CredentialId { get; set; }

        public string? CredentialNumber { get; set; }

        public string? MykadNumber { get; set; }
        public string? Value { get; set; }
        public string? AreaFromId { get; set; }
        public string? AreaToId { get; set; }
        public string CustomDataUDF { get; set; } = string.Empty;
        public string CustomDataString { get; set; } = string.Empty;
        public DateTime UtcTime { get; set; }
        public string ReaderName { get; set; } = string.Empty;
        public string CustomDataEventType { get; set; } = string.Empty;
        public string CustomDataUDFType { get; set; } = string.Empty;
        public bool IsProcessed { get; set; } = false;
    }
}
