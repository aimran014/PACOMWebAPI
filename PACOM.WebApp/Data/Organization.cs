using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PACOM.WebApp.Data
{
    public class Organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Webhook ID")]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Organization Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Organization Code")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; }

        [Display(Name = "Token")]
        public string? Token { get; set; }

        [Display(Name = "Webhook Link")]
        public string? url { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime UpdatedAt { get; set; }
    }
}
