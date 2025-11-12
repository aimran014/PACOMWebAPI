using Microsoft.EntityFrameworkCore;

namespace PACOM.WebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<ActivityEvent> ActivityEvents { get; set; }
    }
}
