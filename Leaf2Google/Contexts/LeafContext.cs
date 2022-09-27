using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google;
using Microsoft.EntityFrameworkCore;

namespace Leaf2Google.Contexts
{
    public class LeafContext : DbContext
    {
        public LeafContext(DbContextOptions<LeafContext> options)
            : base(options)
        {
        }

        public DbSet<CarModel> NissanLeafs { get; set; }

        public DbSet<AuditModel<CarModel>> NissanAudits { get; set; }

        public DbSet<AuthModel> GoogleAuths { get; set; }

        public DbSet<TokenModel> GoogleTokens { get; set; }

        public DbSet<ConfigModel> AppConfig { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder
                .Entity<Leaf>()
                .Property(e => e.NissanPassword)
                .HasConversion(
                    v => v.ToString(),
                    v => (EquineBeast)Enum.Parse(typeof(EquineBeast), v));
            */

            modelBuilder
                .Entity<CarModel>()
                .ToTable("t_leafs_leaf");

            modelBuilder
                .Entity<AuditModel<CarModel>>()
                .ToTable("t_leafs_audit")
                .HasOne(i => i.Owner);

            modelBuilder
                .Entity<AuthModel>()
                .ToTable("t_auths_auth");

            modelBuilder
                .Entity<TokenModel>()
                .ToTable("t_auths_token");

            modelBuilder
                .Entity<ConfigModel>()
                .ToTable("t_app_config");
        }
    }
}