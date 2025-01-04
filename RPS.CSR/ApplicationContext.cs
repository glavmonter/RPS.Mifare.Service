using Microsoft.EntityFrameworkCore;
using RPS.CSR.Models;
using System;

namespace RPS.CSR {
    public class ApplicationContext : DbContext {
        public DbSet<Settings> Settings => Set<Settings>();
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }
        //public ApplicationContext() => Database.EnsureCreated();

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        //    //base.OnConfiguring(optionsBuilder);
        //    optionsBuilder.UseSqlite("Data Source=RPS.CSR.db");
        //}
    }
}
