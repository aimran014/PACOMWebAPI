using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PACOM.WebApp.Data
{
    public class ActivityEvent
    {
        [Key]
        public string Version { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;

        [Display(Name = "Organization")]
        public string? ScopeName { get; set; }
        public string? Organization { get; set; }
        public string? EventId { get; set; }

        [Display(Name = "Event")]
        public string? EventName { get; set; }
        public string? UserId { get; set; }

        [Display(Name = "Username")]
        public string? UserName { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }
        public string? CredentialId { get; set; }

        [Display(Name = "Card Number")]
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
