// Copyright (c) Nathan Ford. All rights reserved. LeafContext.cs

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

    public DbSet<CarEntity> NissanLeafs { get; set; }

    public DbSet<StoredCredentialEntity> SecurityKeys { get; set; }

    public DbSet<AuditEntity> NissanAudits { get; set; }

    public DbSet<AuthEntity> GoogleAuths { get; set; }

    public DbSet<TokenEntity> GoogleTokens { get; set; }

    public DbSet<ConfigEntity> AppConfig { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder
            .Entity<CarEntity>()
            .ToTable("t_leafs_leaf")
            .HasQueryFilter(q => q.Deleted == null)
            .Property(e => e.NissanPassword)
            .HasConversion(
                v => AesEncryption.EncryptStringToBytes(v).Select(e => $"{Convert.ToBase64String(e.Item1)}|{Convert.ToBase64String(e.Item2)}|{Convert.ToBase64String(e.Item3)}").First(),
                v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).Length >= 3 ? AesEncryption.DecryptStringFromBytes(Convert.FromBase64String(v.Split('|', StringSplitOptions.RemoveEmptyEntries)[0]), Convert.FromBase64String(v.Split('|', StringSplitOptions.RemoveEmptyEntries)[1]), Convert.FromBase64String(v.Split('|', StringSplitOptions.RemoveEmptyEntries)[2])) : ""
            );

        _ = modelBuilder
            .Entity<StoredCredentialEntity>()
            .ToTable("t_leafs_securitykey")
            .Ignore(s => s.Descriptor);

        _ = modelBuilder
            .Entity<StoredCredential>()
            .ToTable("t_leafs_securitykey")
            .Ignore(s => s.Descriptor)
            .HasKey(s => s.PublicKey);

        _ = modelBuilder
            .Entity<AuditEntity>()
            .ToTable("t_leafs_audit");

        _ = modelBuilder
            .Entity<AuthEntity>()
            /*
            .OwnsOne(a => a.Data, ownedNavigationBuilder =>
            {
                ownedNavigationBuilder.ToJson();
            })
            */
            .ToTable("t_auths_auth");

        _ = modelBuilder
            .Entity<TokenEntity>()
            .ToTable("t_auths_token");

        _ = modelBuilder
            .Entity<ConfigEntity>()
            .ToTable("t_app_config");
    }
}