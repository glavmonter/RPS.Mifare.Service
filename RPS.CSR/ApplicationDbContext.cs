using Microsoft.EntityFrameworkCore;
using RPS.CSR.Models;

namespace RPS.CSR {
    public class ApplicationDbContext : DbContext {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Settings> Settings => Set<Settings>();

        public DbSet<KeySettings> KeySettings => Set<KeySettings>();
    }
}
