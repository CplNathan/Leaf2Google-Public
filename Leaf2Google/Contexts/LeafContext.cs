using Leaf2Google.Entities.Car;
using Leaf2Google.Entities.Generic;
using Leaf2Google.Entities.Google;
using Leaf2Google.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Leaf2Google.Contexts;

public class LeafContext : DbContext
{
    public LeafContext(DbContextOptions<LeafContext> options)
        : base(options)
    {
    }

    public DbSet<CarModel> NissanLeafs { get; set; }

    public DbSet<StoredCredentialModel> SecurityKeys { get; set; }

    public DbSet<AuditModel> NissanAudits { get; set; }

    public DbSet<AuthModel> GoogleAuths { get; set; }

    public DbSet<TokenModel> GoogleTokens { get; set; }

    public DbSet<ConfigModel> AppConfig { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<CarModel>()
            .ToTable("t_leafs_leaf")
            .HasQueryFilter(q => q.Deleted == null)
            .Property(e => e.NissanPassword)
            .HasConversion(
                v => AesEncryption.EncryptStringToBytes(v).Select(e => $"{Convert.ToBase64String(e.Item1)}|{Convert.ToBase64String(e.Item2)}|{Convert.ToBase64String(e.Item3)}").First(),
                v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).Length >= 3 ? AesEncryption.DecryptStringFromBytes(Convert.FromBase64String(v.Split('|', StringSplitOptions.RemoveEmptyEntries)[0]), Convert.FromBase64String(v.Split('|', StringSplitOptions.RemoveEmptyEntries)[1]), Convert.FromBase64String(v.Split('|', StringSplitOptions.RemoveEmptyEntries)[2])) : ""
            );

        modelBuilder
            .Entity<StoredCredentialModel>()
            .ToTable("t_leafs_securitykey")
            .Ignore(s => s.Descriptor);

        modelBuilder
            .Entity<StoredCredential>()
            .ToTable("t_leafs_securitykey")
            .Ignore(s => s.Descriptor)
            .HasKey(s => s.PublicKey);

        modelBuilder
            .Entity<AuditModel>()
            .ToTable("t_leafs_audit");

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