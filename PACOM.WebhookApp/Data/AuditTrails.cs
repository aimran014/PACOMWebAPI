using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PACOM.WebhookApp.Data
{
    public class AuditTrails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Action { get; set; }
        public string? UserName { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}
