using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PACOM.WebhookApp.Data
{
    public class ApplicationDbContext : IdentityDbContext <ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationRoles> OrganizationRoles { get; set; }
        public DbSet<ActivityEvent> ActivityEvents { get; set; }
        public DbSet<AuditTrails> AuditTrails { get; set; }

        }
}
